using Beamable.Common.Inventory;
using Beamable.Editor.Content;
using Beamable.Editor.Content.UI;
using Beamable.Editor.ContentService;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Beamable.Editor.Tests
{
	public class AssetReferenceResolutionTests
	{
		// Pinned GUID of Editor/Modules/Content/DefaultAssets~/currency/coins.png.
		private const string COINS_SEED_GUID = "a7796303ede2490ea179355045c77b7e";

		private readonly List<string> _createdAssets = new List<string>();

		[SetUp]
		public void SetUp()
		{
			AssetReferenceResolution.ClearCache();
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var path in _createdAssets)
			{
				AssetDatabase.DeleteAsset(path);
			}
			_createdAssets.Clear();
			AssetReferenceResolution.ClearCache();
		}

		[Test]
		public void EmptyGuid_IsEmpty()
		{
			Assert.That(AssetReferenceResolution.Resolve(typeof(AssetReferenceSprite), string.Empty, out _), Is.EqualTo(AssetReferenceState.Empty));
			Assert.That(AssetReferenceResolution.Resolve(typeof(AssetReferenceSprite), null, out _), Is.EqualTo(AssetReferenceState.Empty));
		}

		[Test]
		public void UnknownGuid_IsMissingAsset_WithoutLogging()
		{
			var state = AssetReferenceResolution.Resolve(typeof(AssetReferenceSprite), NewGuid(), out var path);

			Assert.That(state, Is.EqualTo(AssetReferenceState.MissingAsset));
			Assert.That(path, Is.Null);
			LogAssert.NoUnexpectedReceived();
		}

		[Test]
		public void SpriteTexture_IsResolved()
		{
			var guid = CreateTexture(TextureImporterType.Sprite, SpriteImportMode.Single);

			Assert.That(AssetReferenceResolution.Resolve(typeof(AssetReferenceSprite), guid, out _), Is.EqualTo(AssetReferenceState.Resolved));
		}

		[Test]
		public void SlicedSpriteTexture_IsResolved()
		{
			var guid = CreateTexture(TextureImporterType.Sprite, SpriteImportMode.Multiple, sliced: true);

			Assert.That(AssetReferenceResolution.Resolve(typeof(AssetReferenceSprite), guid, out _), Is.EqualTo(AssetReferenceState.Resolved));
		}

		[Test]
		public void DefaultTexture_IsWrongTypeForSprite_WithoutLogging()
		{
			var guid = CreateTexture(TextureImporterType.Default, SpriteImportMode.None);

			var state = AssetReferenceResolution.Resolve(typeof(AssetReferenceSprite), guid, out var path);

			Assert.That(state, Is.EqualTo(AssetReferenceState.WrongType));
			Assert.That(path, Is.EqualTo(AssetDatabase.GUIDToAssetPath(guid)));
			LogAssert.NoUnexpectedReceived();
		}

		[Test]
		public void DefaultTexture_IsResolvedForTextureAndPlainReferences()
		{
			var guid = CreateTexture(TextureImporterType.Default, SpriteImportMode.None);

			Assert.That(AssetReferenceResolution.Resolve(typeof(AssetReferenceTexture2D), guid, out _), Is.EqualTo(AssetReferenceState.Resolved));
			Assert.That(AssetReferenceResolution.Resolve(typeof(AssetReference), guid, out _), Is.EqualTo(AssetReferenceState.Resolved));
		}

		[Test]
		public void SpriteTexture_IsWrongTypeForAtlasedSprite()
		{
			var guid = CreateTexture(TextureImporterType.Sprite, SpriteImportMode.Single);

			Assert.That(AssetReferenceResolution.Resolve(typeof(AssetReferenceAtlasedSprite), guid, out _), Is.EqualTo(AssetReferenceState.WrongType));
		}

		[Test]
		public void SeedSpriteGuid_IsRecognized()
		{
			Assert.That(DefaultContentImporter.IsSeedSpriteGuid(COINS_SEED_GUID), Is.True);
			Assert.That(DefaultContentImporter.IsSeedSpriteGuid(NewGuid()), Is.False);
			Assert.That(DefaultContentImporter.IsSeedSpriteGuid(null), Is.False);
		}

		[Test]
		public void SeedSpriteGuid_GuidanceMentionsImportDefaultContent()
		{
			var guidance = AssetReferenceResolution.GetGuidance(typeof(AssetReferenceSprite), AssetReferenceState.MissingAsset, COINS_SEED_GUID, null);

			StringAssert.Contains("Import Default Content", guidance);
		}

		[Test]
		public void CurrencyWithMissingIcon_IsUnresolved_AndInspectingDoesNotChangeContent()
		{
			var guid = NewGuid();
			var currency = ScriptableObject.CreateInstance<CurrencyContent>();
			try
			{
				currency.icon = new AssetReferenceSprite(guid);
				var checksum = currency.GetEditorDataChecksum();
				var serialized = new SerializedObject(currency);

				var found = ContentObjectEditor.TryGetUnresolvedAssetReference(typeof(CurrencyContent), serialized.FindProperty(nameof(CurrencyContent.icon)), out var unresolved);

				Assert.That(found, Is.True);
				Assert.That(unresolved.state, Is.EqualTo(AssetReferenceState.MissingAsset));
				Assert.That(unresolved.guid, Is.EqualTo(guid));
				Assert.That(unresolved.referenceType, Is.EqualTo(typeof(AssetReferenceSprite)));
				Assert.That(serialized.ApplyModifiedProperties(), Is.False);
				Assert.That(currency.GetEditorDataChecksum(), Is.EqualTo(checksum));
				Assert.That(currency.icon.AssetGUID, Is.EqualTo(guid));
				LogAssert.NoUnexpectedReceived();
			}
			finally
			{
				Object.DestroyImmediate(currency);
			}
		}

		[Test]
		public void CurrencyWithSpriteIcon_IsDrawnByAddressables()
		{
			var guid = CreateTexture(TextureImporterType.Sprite, SpriteImportMode.Single);
			var currency = ScriptableObject.CreateInstance<CurrencyContent>();
			try
			{
				currency.icon = new AssetReferenceSprite(guid);
				var serialized = new SerializedObject(currency);

				Assert.That(ContentObjectEditor.TryGetUnresolvedAssetReference(typeof(CurrencyContent), serialized.FindProperty(nameof(CurrencyContent.icon)), out _), Is.False);
			}
			finally
			{
				Object.DestroyImmediate(currency);
			}
		}

		[Test]
		public void NonAssetReferenceField_IsIgnored()
		{
			var currency = ScriptableObject.CreateInstance<CurrencyContent>();
			try
			{
				var serialized = new SerializedObject(currency);

				Assert.That(ContentObjectEditor.TryGetUnresolvedAssetReference(typeof(CurrencyContent), serialized.FindProperty(nameof(CurrencyContent.clientPermission)), out _), Is.False);
			}
			finally
			{
				Object.DestroyImmediate(currency);
			}
		}

		private static string NewGuid() => Guid.NewGuid().ToString("N");

		private string CreateTexture(TextureImporterType textureType, SpriteImportMode spriteMode, bool sliced = false)
		{
			var path = $"Assets/__AssetRefTest_{NewGuid()}.png";
			var texture = new Texture2D(8, 8);
			try
			{
				File.WriteAllBytes(path, texture.EncodeToPNG());
			}
			finally
			{
				Object.DestroyImmediate(texture);
			}
			_createdAssets.Add(path);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

			var importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.textureType = textureType;
			importer.spriteImportMode = spriteMode;
			if (sliced)
			{
#pragma warning disable CS0618 // spritesheet is the simplest slicing API available on every supported Unity version.
				importer.spritesheet = new[]
				{
					new SpriteMetaData { name = "left", rect = new Rect(0, 0, 4, 8), pivot = new Vector2(0.5f, 0.5f) },
					new SpriteMetaData { name = "right", rect = new Rect(4, 0, 4, 8), pivot = new Vector2(0.5f, 0.5f) },
				};
#pragma warning restore CS0618
			}
			importer.SaveAndReimport();

			return AssetDatabase.AssetPathToGUID(path);
		}
	}
}
