using Beamable.Editor.Reflection;
using System.Linq;
using UnityEditor;

#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER
using Beamable.Editor.ToolbarExtender;
#endif

namespace Beamable.Editor.Assistant
{
	public class BeamableAssistantAssetPostProcessor : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER
			var toolbarExtendedRelatedAssets = importedAssets.Union(movedAssets)
			                                                 .Select(path => (path, type: AssetDatabase.GetMainAssetTypeAtPath(path)))
			                                                 .Where(t => typeof(BeamableAssistantMenuItem).IsAssignableFrom(t.type) || typeof(BeamableToolbarButton).IsAssignableFrom(t.type))
			                                                 .ToList();

			if (toolbarExtendedRelatedAssets.Count > 0 || deletedAssets.Length > 0)
				BeamableToolbarExtender.Reload();
#endif
			var beamHintDetailsRelatedAssets = importedAssets.Union(movedAssets)
			                                                 .Select(path => (path, type: AssetDatabase.GetMainAssetTypeAtPath(path)))
			                                                 .Where(t => typeof(BeamHintDetailsConfig).IsAssignableFrom(t.type) || typeof(BeamHintDetailsConfig).IsAssignableFrom(t.type))
			                                                 .ToList();

			if (beamHintDetailsRelatedAssets.Count > 0 || deletedAssets.Length > 0)
			{
				EditorAPI.Instance.Then(editorApi => {
					editorApi.EditorReflectionCache.GetFirstRegisteredUserSystemOfType<BeamHintDetailsReflectionCache.Registry>()
					         .ReloadHintDetailConfigScriptableObjects(editorApi.CoreConfiguration.BeamableAssistantHintDetailConfigPaths);
				});
			}
		}
	}
}
