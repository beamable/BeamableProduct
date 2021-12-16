#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER

using Editor.Beamable.ToolbarExtender;
using System;
using System.Linq;
using UnityEditor;

namespace Editor.BeamableAssistant
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
