using Beamable.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.QuickSearch;
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
		public static string[] FindAssets(Type t, string[] searchInFolders = null)
		{
			Assert.IsNotNull(t, "Cannot find assets for null type");
			Assert.IsFalse(t.FullName != null && t.FullName.Contains(nameof(UnityEditorInternal)),
						   $"Type {t.FullName} is part of `UnityEditorInternal`- these assets should be found using just nameof, not full type name");
			var fullName = t.FullName;
			var shortName = t.Name;
			
			var searchContext = SearchService.CreateContext(SearchService.Providers.Where(n => !string.Equals(n.name.id, "scene")), $"t:{shortName}");

			var result1 = new List<SearchItem>();
			
			{
				// Set up a callback that will be used gather additional asynchronous results.
				searchContext.asyncItemReceived += (context, incomingItems) => result1.AddRange(incomingItems);

				// Initiate the query and get the first results.
				result1.AddRange(SearchService.GetItems(searchContext, SearchFlags.WantsMore | SearchFlags.Synchronous));
			}
			
			var result2 = AssetDatabase.FindAssets($"t:{fullName}", searchInFolders);

			List<string> result2_paths = new List<string>();
			for (int i = 0; i < result2.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(result2[i]);
				result2_paths.Add(path);
				
			}
			

			if (result1.Count != result2.Length)
			{
				Debug.LogError("DIFFERENT FOR: " + shortName + " | quickAmmount : " + result1.Count + " , legacyAmount : " + result2.Length);
			}
			else
			{
				Debug.LogError("EXISTS FOR: " + shortName + " | quickAmmount : " + result1.Count + " , legacyAmount : " + result2.Length);
			}
			
			return result2;
		}

		/// <inheritdoc cref="FindAssets"/>
		public static string[] FindAssets<T>(string[] searchInFolders = null) => FindAssets(typeof(T), searchInFolders);
		
		
	}
}
