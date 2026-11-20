using Beamable.Common.Inventory;
using Beamable.Editor.Content;
using Beamable.Editor.Content.UI;
using Beamable.Editor.ContentService;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
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
			var guid = CopySlicedSpriteSheet();

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
		public void SeedSpriteGuid_GuidanceMentionsImportDefaultIcons()
		{
			var guidance = AssetReferenceResolution.GetGuidance(typeof(AssetReferenceSprite), AssetReferenceState.MissingAsset, COINS_SEED_GUID, null);

			StringAssert.Contains("Import Default Icons", guidance);
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

		[Test]
		public void ReplacementWithoutSettings_PreservesExistingReference()
		{
			var spriteGuid = CreateTexture(TextureImporterType.Sprite, SpriteImportMode.Single);
			var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(spriteGuid));
			AssertRejectedReplacement(sprite, typeof(AssetReferenceSprite), "configure Addressables");
		}

		[Test]
		public void NonSpriteSubAsset_PreservesExistingReference()
		{
			var path = $"Assets/__AssetRefTest_{NewGuid()}.asset";
			AssetDatabase.CreateAsset(new Mesh { name = "Main" }, path);
			_createdAssets.Add(path);
			var mesh = new Mesh { name = "Child" };
			AssetDatabase.AddObjectToAsset(mesh, path);
			AssetDatabase.SaveAssets();

			AssertRejectedReplacement(mesh, typeof(AssetReferenceT<Mesh>), "pick a standalone asset");
		}

		private static void AssertRejectedReplacement(Object asset, Type referenceType, string expectedWarning)
		{
			var currency = ScriptableObject.CreateInstance<CurrencyContent>();
			try
			{
				currency.icon = new AssetReferenceSprite(NewGuid());
				var serialized = new SerializedObject(currency);
				var icon = serialized.FindProperty(nameof(CurrencyContent.icon));
				icon.FindPropertyRelative(AssetReferenceResolution.SUB_OBJECT_NAME_PROPERTY).stringValue = "Previous";
				serialized.ApplyModifiedProperties();
				var previousKey = currency.icon.RuntimeKey;

				LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(expectedWarning));
				ContentObjectEditor.AssignAssetReference(icon, referenceType, asset, null);

				Assert.That(serialized.ApplyModifiedProperties(), Is.False);
				Assert.That(currency.icon.RuntimeKey, Is.EqualTo(previousKey));
			}
			finally
			{
				Object.DestroyImmediate(currency);
			}
		}

		[Test]
		public void SlicedSpriteReplacement_RegistersAssetAndPreservesSelectedSprite()
		{
			var guid = CopySlicedSpriteSheet();
			var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GUIDToAssetPath(guid));
			var sprite = Array.Find(sprites, asset => asset is Sprite && asset.name == "Avatars_1");
			Assert.That(sprite, Is.Not.Null);
			var settings = AddressableAssetSettings.Create("Assets", "TestSettings", true, false);
			var currency = ScriptableObject.CreateInstance<CurrencyContent>();
			try
			{
				currency.icon = new AssetReferenceSprite(NewGuid());
				var serialized = new SerializedObject(currency);
				var icon = serialized.FindProperty(nameof(CurrencyContent.icon));
				ContentObjectEditor.AssignAssetReference(icon, typeof(AssetReferenceSprite), sprite, settings);
				serialized.ApplyModifiedProperties();

				Assert.That(settings.FindAssetEntry(guid), Is.Not.Null);
				Assert.That(currency.icon.RuntimeKey, Is.EqualTo($"{guid}[Avatars_1]"));
			}
			finally
			{
				Object.DestroyImmediate(currency);
				Object.DestroyImmediate(settings);
			}
		}

		private static string NewGuid() => Guid.NewGuid().ToString("N");

		private string CopySlicedSpriteSheet()
		{
			const string source = "Packages/com.beamable/Runtime/Modules/Avatars/Sprites/Avatars.png";
			var path = $"Assets/__AssetRefTest_{NewGuid()}.png";
			Assert.That(AssetDatabase.CopyAsset(source, path), Is.True);
			_createdAssets.Add(path);
			return AssetDatabase.AssetPathToGUID(path);
		}

		private string CreateTexture(TextureImporterType textureType, SpriteImportMode spriteMode)
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
			importer.SaveAndReimport();

			return AssetDatabase.AssetPathToGUID(path);
		}
	}
}
