using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	[CreateAssetMenu(menuName = "Beamable/Toolbar/Menu Items/Separator", fileName = "BeamableAssistantMenuItemSeparator", order = 0)]
	public sealed class BeamableToolbarMenuItemSeparator : BeamableToolbarMenuItem
	{
		public override GUIContent RenderLabel(BeamEditorContext beamableApi)
		{
			Text = "";
			return base.RenderLabel(beamableApi);
		}

		public override void OnItemClicked(BeamEditorContext beamableApi) { }
	}
}
