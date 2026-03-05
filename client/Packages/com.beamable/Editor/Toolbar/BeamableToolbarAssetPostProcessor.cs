using Beamable.Editor.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER
using Beamable.Editor.ToolbarExtender;
#endif

namespace Beamable.Editor
{
	/// <summary>
	/// An asset post-processor that reloads and re-builds Beamable Toolbar-related data defined in relevant scriptable objects.
	/// </summary>
	public class BeamableToolbarAssetPostProcessor : AssetPostprocessor
	{
		public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER
			var toolbarExtendedRelatedAssets = importedAssets.Union(movedAssets)
															 .Select(path => (path, type: AssetDatabase.GetMainAssetTypeAtPath(path)))
															 .Where(t => typeof(BeamableToolbarMenuItem).IsAssignableFrom(t.type) || typeof(BeamableToolbarButton).IsAssignableFrom(t.type))
															 .ToList();

			if (toolbarExtendedRelatedAssets.Count > 0 || deletedAssets.Length > 0)
				BeamableToolbarExtender.LoadToolbarExtender();
#endif
		}
	}
}
