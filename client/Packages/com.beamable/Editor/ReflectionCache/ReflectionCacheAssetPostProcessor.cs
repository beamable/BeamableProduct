using Beamable.Editor;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Common
{
	public class ReflectionCacheAssetPostProcessor : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			var assistantRelatedAssets = importedAssets.Union(movedAssets)
			                                           .Select(path => (path, type: AssetDatabase.GetMainAssetTypeAtPath(path)))
			                                           .Where(t => typeof(ReflectionCacheUserSystemObject).IsAssignableFrom(t.type))
			                                           .ToList();

			if (assistantRelatedAssets.Count > 0 || deletedAssets.Length > 0)
			{
				EditorAPI.Instance.Then(api => {
					BeamableLogger.Log("Re-building the Reflection Cache!");
					api.EditorReflectionCache.GenerateReflectionCache(assembliesToSweep: api.CoreConfiguration.AssembliesToSweep);
					BeamableLogger.Log("Finished Rebuilding Reflection Cache Systems");
					AssetDatabase.Refresh();
				});
			}
		}
	}
}
