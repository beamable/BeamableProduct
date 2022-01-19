using Beamable.Common;
using Beamable.Common.Reflection;
using Beamable.Reflection;
using System.Linq;
using UnityEditor;

namespace Beamable.Editor.Reflection
{
	/// <summary>
	/// An asset post-processor that reloads and rebuilds all (or the re-imported) <see cref="IReflectionSystem"/> defined via <see cref="ReflectionSystemObject"/> whenever
	/// one gets re-imported, deleted or moved.
	///
	/// This makes it so that a recompile isn't necessary to update the <see cref="ReflectionCache"/> for cases where you might not want that.
	/// </summary>
	public class ReflectionCacheAssetPostProcessor : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			var reflectionCacheRelatedAssets = importedAssets.Union(movedAssets)
			                                                 .Select(path => (path, type: AssetDatabase.GetMainAssetTypeAtPath(path)))
			                                                 .Where(t => typeof(ReflectionSystemObject).IsAssignableFrom(t.type))
			                                                 .ToList();

			if (reflectionCacheRelatedAssets.Count > 0)
			{
				var reimportedReflectionTypes = reflectionCacheRelatedAssets.Select(tuple => AssetDatabase.LoadAssetAtPath<ReflectionSystemObject>(tuple.path).SystemType).ToList();

				BeamableLogger.Log("Re-building the Reflection Systems from Cached Data!");
				BeamEditor.EditorReflectionCache.RebuildReflectionUserSystems(reimportedReflectionTypes);
				BeamableLogger.Log("Finished Rebuilding Reflection Cache Systems");
				AssetDatabase.Refresh();
			}

			if (deletedAssets.Length > 0)
			{
				BeamableLogger.Log("Re-building the Reflection Systems from Cached Data!");
				BeamEditor.EditorReflectionCache.RebuildReflectionUserSystems();
				BeamableLogger.Log("Finished Rebuilding Reflection Cache Systems");
				AssetDatabase.Refresh();
			}
		}
	}
}
