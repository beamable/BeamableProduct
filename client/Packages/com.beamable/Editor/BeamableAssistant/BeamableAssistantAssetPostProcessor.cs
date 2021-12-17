#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER

using Beamable.Editor.ToolbarExtender;
using System.Linq;
using UnityEditor;

namespace Beamable.Editor.Assistant
{
	public class BeamableAssistantAssetPostProcessor : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			var assistantRelatedAssets = importedAssets.Union(movedAssets)
			                                           .Select(path => (path, type: AssetDatabase.GetMainAssetTypeAtPath(path)))
			                                           .Where(t => typeof(BeamableAssistantMenuItem).IsAssignableFrom(t.type) || typeof(BeamableToolbarButton).IsAssignableFrom(t.type))
			                                           .ToList();

			if (assistantRelatedAssets.Count > 0 || deletedAssets.Length > 0)
				BeamableToolbarExtender.Reload();
		}
	}
}
#endif
