using Beamable.Common;
using Beamable.Common.Reflection;
using Beamable.Reflection;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
			if (!BeamEditor.IsInitialized)
				return;

			var reflectionCacheRelatedAssets = importedAssets.Union(movedAssets)
															 .Select(path => (path, type: AssetDatabase.GetMainAssetTypeAtPath(path)))
															 .Where(t => typeof(ReflectionSystemObject).IsAssignableFrom(t.type))
															 .ToList();

			if (reflectionCacheRelatedAssets.Count > 0)
			{
				var reimportedReflectionSystemObjects = reflectionCacheRelatedAssets
														.Select(tuple => AssetDatabase.LoadAssetAtPath<ReflectionSystemObject>(tuple.path)).ToList();
				var reimportedReflectionTypes = reimportedReflectionSystemObjects.Select(sysObj => sysObj.SystemType).ToList();

				BeamEditor.EditorReflectionCache.RebuildReflectionUserSystems(reimportedReflectionTypes);
				BeamEditor.EditorReflectionCache.SetStorage(BeamEditor.HintGlobalStorage);

				if (reimportedReflectionTypes.Contains(typeof(BeamHintReflectionCache.Registry)))
				{
					// Set up Globally Accessible Hint System Dependencies and then call init
					foreach (var hintSystem in BeamEditor.GetReflectionSystem<BeamHintReflectionCache.Registry>().GloballyAccessibleHintSystems)
					{
						hintSystem.SetStorage(BeamEditor.HintGlobalStorage);
						hintSystem.SetPreferencesManager(BeamEditor.HintPreferencesManager);

						hintSystem.OnInitialized();
					}
				}

				AssetDatabase.Refresh();
			}

			if (deletedAssets.Length > 0)
			{
				BeamEditor.EditorReflectionCache.RebuildReflectionUserSystems();
				BeamEditor.EditorReflectionCache.SetStorage(BeamEditor.HintGlobalStorage);

				// Set up Globally Accessible Hint System Dependencies and then call init
				foreach (var hintSystem in BeamEditor.GetReflectionSystem<BeamHintReflectionCache.Registry>().GloballyAccessibleHintSystems)
				{
					hintSystem.SetStorage(BeamEditor.HintGlobalStorage);
					hintSystem.SetPreferencesManager(BeamEditor.HintPreferencesManager);

					hintSystem.OnInitialized();
				}

				AssetDatabase.Refresh();
			}
		}
	}
}
