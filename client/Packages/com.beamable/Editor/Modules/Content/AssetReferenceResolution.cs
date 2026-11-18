using Beamable.Editor.ContentService;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;

namespace Beamable.Editor.Content
{
	/// <summary>
	/// Whether an <see cref="AssetReference"/> GUID resolves to an asset its reference type accepts.
	/// </summary>
	public enum AssetReferenceState
	{
		/// <summary>No GUID is assigned.</summary>
		Empty,
		/// <summary>The GUID resolves to an asset the reference type accepts.</summary>
		Resolved,
		/// <summary>The GUID does not exist in this project.</summary>
		MissingAsset,
		/// <summary>The GUID exists, but the asset is not a type the reference accepts.</summary>
		WrongType,
	}

	/// <summary>
	/// Resolves <see cref="AssetReference"/> GUIDs the way the Addressables <c>editorAsset</c> getter does,
	/// without logging. Addressables logs "Assigned editorAsset does not match type ..." on every read of
	/// an unresolved reference, which its property drawer does on every repaint. The content inspector
	/// uses this to draw unresolved references itself instead.
	/// </summary>
	public static class AssetReferenceResolution
	{
		public const string GUID_PROPERTY = "m_AssetGUID";
		public const string SUB_OBJECT_NAME_PROPERTY = "m_SubObjectName";
		public const string SUB_OBJECT_TYPE_PROPERTY = "m_SubObjectType";

		private struct Resolution
		{
			public AssetReferenceState state;
			public string assetPath;
		}

		private static readonly Dictionary<(Type, string), Resolution> _cache = new Dictionary<(Type, string), Resolution>();
		private static bool _subscribed;

		/// <summary>True when <paramref name="type"/> is an <see cref="AssetReference"/> or derives from one.</summary>
		public static bool IsAssetReferenceType(Type type)
		{
			return type != null && typeof(AssetReference).IsAssignableFrom(type);
		}

		/// <summary>
		/// Resolves <paramref name="guid"/> for a field of type <paramref name="referenceType"/>.
		/// Results are cached until the project changes.
		/// </summary>
		/// <param name="referenceType">The <see cref="AssetReference"/> type of the field.</param>
		/// <param name="guid">The asset GUID stored in the reference.</param>
		/// <param name="assetPath">The asset path of the GUID, or null when it does not exist.</param>
		public static AssetReferenceState Resolve(Type referenceType, string guid, out string assetPath)
		{
			assetPath = null;
			if (string.IsNullOrEmpty(guid)) return AssetReferenceState.Empty;

			EnsureSubscribed();
			var key = (referenceType, guid);
			if (!_cache.TryGetValue(key, out var resolution))
			{
				resolution = ResolveUncached(referenceType, guid);
				_cache[key] = resolution;
			}

			assetPath = resolution.assetPath;
			return resolution.state;
		}

		/// <summary>Forgets cached results. Called automatically when the project changes.</summary>
		public static void ClearCache()
		{
			_cache.Clear();
		}

		/// <summary>
		/// The asset type an <see cref="AssetReferenceT{TObject}"/> loads, or null for a plain <see cref="AssetReference"/>.
		/// </summary>
		public static Type GetTargetType(Type referenceType)
		{
			for (var type = referenceType; type != null && type != typeof(object); type = type.BaseType)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AssetReferenceT<>))
				{
					return type.GetGenericArguments()[0];
				}
			}

			return null;
		}

		/// <summary>
		/// User-facing guidance for an unresolved reference.
		/// </summary>
		public static string GetGuidance(Type referenceType, AssetReferenceState state, string guid, string assetPath)
		{
			var expected = DescribeExpectedType(referenceType);
			switch (state)
			{
				case AssetReferenceState.MissingAsset:
					var message = $"References asset GUID {guid}, which is not in this project. " +
					              "The reference is kept unchanged. ";
					if (DefaultContentImporter.IsSeedSpriteGuid(guid))
					{
						return message + "It is a Beamable default content sprite: run Import Default Content to add it, " +
						       $"or pick a {expected} to replace it.";
					}

					return message + $"Import the asset into this project, or pick a {expected} to replace it.";
				case AssetReferenceState.WrongType:
					var spriteHint = typeof(Sprite).IsAssignableFrom(GetTargetType(referenceType) ?? typeof(object))
					                 && referenceType != typeof(AssetReferenceAtlasedSprite)
						? " If it is a texture, set its Texture Type to 'Sprite (2D and UI)' in its import settings."
						: string.Empty;
					return $"'{assetPath}' is not a {expected}. The reference is kept unchanged.{spriteHint} " +
					       $"Or pick a {expected} to replace it.";
				default:
					return string.Empty;
			}
		}

		/// <summary>Short description of the assets a reference type accepts, such as "Sprite or SpriteAtlas".</summary>
		public static string DescribeExpectedType(Type referenceType)
		{
			if (referenceType == typeof(AssetReferenceAtlasedSprite)) return nameof(SpriteAtlas);
			var target = GetTargetType(referenceType);
			if (target == null) return "asset";
			if (target == typeof(Sprite)) return $"{nameof(Sprite)} or {nameof(SpriteAtlas)}";
			return target.Name;
		}

		private static Resolution ResolveUncached(Type referenceType, string guid)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var mainType = string.IsNullOrEmpty(path) ? null : AssetDatabase.GetMainAssetTypeAtPath(path);
			if (mainType == null)
			{
				return new Resolution { state = AssetReferenceState.MissingAsset };
			}

			return new Resolution { state = IsAccepted(referenceType, path, mainType) ? AssetReferenceState.Resolved : AssetReferenceState.WrongType, assetPath = path };
		}

		// Mirrors the lookups in the Addressables GetEditorAssetInternal overrides.
		private static bool IsAccepted(Type referenceType, string path, Type mainType)
		{
			if (typeof(AssetReferenceAtlasedSprite).IsAssignableFrom(referenceType))
			{
				return mainType == typeof(SpriteAtlas);
			}

			var target = GetTargetType(referenceType);
			if (target == null)
			{
				// A plain AssetReference accepts any asset and never logs.
				return true;
			}

			if (typeof(AssetReferenceSprite).IsAssignableFrom(referenceType) && mainType == typeof(SpriteAtlas))
			{
				return true;
			}

			return AssetDatabase.LoadAssetAtPath(path, target) != null;
		}

		private static void EnsureSubscribed()
		{
			if (_subscribed) return;
			_subscribed = true;
			EditorApplication.projectChanged += ClearCache;
		}
	}
}
