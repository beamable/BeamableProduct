using Beamable.Common;
using Beamable.Reflection;
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

			var result1 = SearchService.GetItems(searchContext, SearchFlags.WantsMore);
			var result_path = new List<string>();

			if (searchInFolders != null)
			{
				for (int i = 0; i < result1.Count; i++)
				{
					foreach (var t1 in searchInFolders)
					{
						if (result1[i].id.Contains(t1))
						{
							result_path.Add(AssetDatabase.AssetPathToGUID(result1[i].id));
						}
					}
				}
				
				result_path = result_path.Distinct().ToList();
			}
			else
			{
				result_path = result1.Select(p => AssetDatabase.AssetPathToGUID(p.id)).Distinct().ToList();
			}
			
			if (result_path.Count == 0 || t == typeof(ReflectionSystemObject)) // FALLBACK CODE
			{
				Debug.LogError("FALLBACK " + shortName);
				return AssetDatabase.FindAssets($"t:{fullName}", searchInFolders);
			}

			return result_path.ToArray();
		}

		/// <inheritdoc cref="FindAssets"/>
		public static string[] FindAssets<T>(string[] searchInFolders = null) => FindAssets(typeof(T), searchInFolders);
		
		
	}
}
