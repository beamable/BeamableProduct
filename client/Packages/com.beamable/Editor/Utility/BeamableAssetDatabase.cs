using System;
using System.Collections.Generic;
using System.Linq;
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
			
			var searchContext = SearchService.CreateContext(new string[] {"asset", "object", "res"}, $"t:{fullName}");

			SearchProvider beamableProvider = new SearchProvider("beam", "beamableProvider");

			var result1 = SearchService.GetItems(searchContext, SearchFlags.WantsMore)
			                           .Select(x => AssetDatabase.AssetPathToGUID(x.id)).ToArray();
			
			//var results = new List<SearchItem>();
			//results.AddRange(SearchService.GetItems(searchContext, SearchFlags.WantsMore));
			//SearchService.Request(searchContext, SearchFlags.WantsMore | SearchFlags.Debug);
		
			
			//while (searchContext.searchInProgress);
			
			//var rr = results .Select(x => AssetDatabase.AssetPathToGUID(x.id)).ToArray();
			
				var result2 = AssetDatabase.FindAssets($"t:{fullName}", searchInFolders);

				if (result1.Length != result2.Length)
				{

					for (int i = 0; i < result2.Length; i++)
					{
						if (!result1.Contains(result2[i]))
						{
							string path = AssetDatabase.GUIDToAssetPath(result2[i]);
							Debug.LogError("DIFFERENT FOR: " + path);
						}
					}
				}
				else
				{
					for (int i = 0; i < result2.Length; i++)
					{
						string path = AssetDatabase.GUIDToAssetPath(result2[i]);
						Debug.LogError("WORKS FOR: " + path);
					}
				}
				
			return result2;

			//	Debug.LogError("DIFFERENT");

			//return resitem.ToArray();
		}

		/// <inheritdoc cref="FindAssets"/>
		public static string[] FindAssets<T>(string[] searchInFolders = null) => FindAssets(typeof(T), searchInFolders);
		
		
	}
}
