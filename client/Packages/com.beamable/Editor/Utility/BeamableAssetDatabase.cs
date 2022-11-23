using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Beamable.Editor
{
	public static class BeamableAssetDatabase
	{
		/// <summary>
		/// A sugar on top of <see cref="AssetDatabase.FindAssets"/> that uses the type's full name,
		/// so that we don't collide with client namespaces or common types.
		/// </summary>
		/// <param name="searchInFolders">The folders where the search will start.</param>
		/// <returns>
		///   <para>Array of matching asset. Note that GUIDs will be returned.</para>
		/// </returns>
		[Obsolete("This method may return guids to files that are not valid assets. Use " + nameof(FindAssetPaths) + " to get asset paths instead. ")]
		public static string[] FindAssets(Type t, string[] searchInFolders = null)
		{
			Assert.IsNotNull(t, "Cannot find assets for null type");
			Assert.IsFalse(t.FullName != null && t.FullName.Contains(nameof(UnityEditorInternal)),
						   $"Type {t.FullName} is part of `UnityEditorInternal`- these assets should be found using just nameof, not full type name");
			var fullName = t.FullName;
			
			return AssetDatabase.FindAssets($"t:{fullName}", searchInFolders);
		}

		
		/// <summary>
		/// Find all asset paths for the given type.
		/// Will filter out any returned assets that are not valid asset files.
		/// </summary>
		/// <param name="searchInFolders">The folders where the search will start.</param>
		/// <returns>
		///   <para>Array of matching asset. Note that paths will be returned.</para>
		/// </returns>
		public static List<string> FindAssetPaths(Type t, string[] searchInFolders = null)
		{
#pragma warning disable CS0618
			var guids = FindAssets(t, searchInFolders);
#pragma warning restore CS0618
			var paths = new List<string>();
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.EndsWith(".asset")) continue;
				var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
				if (assetType != t && !assetType.IsSubclassOf(t))
				{
					// Debug.LogWarning(
					// 	$"Found invalid type [{assetType.Name}] at path=[{path}] while trying to load [{t.Name}] instances");
					continue;
				}
				paths.Add(path);
			}

			return paths;
		}

		/// <inheritdoc cref="FindAssets"/>
		[Obsolete("This method may return guids to files that are not valid assets. Use " + nameof(FindAssetPaths) + " to get asset paths instead. ")]
#pragma warning disable CS0618
		public static string[] FindAssets<T>(string[] searchInFolders = null) => FindAssets(typeof(T), searchInFolders);
#pragma warning restore CS0618
		
		/// <inheritdoc cref="FindAssetPaths"/>
		public static List<string> FindAssetPaths<T>(string[] searchInFolders = null) => FindAssetPaths(typeof(T), searchInFolders);
	}
}
