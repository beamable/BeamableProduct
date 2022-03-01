using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	[CreateAssetMenu(menuName = "Beamable/Assistant/Toolbar Menu Items/Separator", fileName = "BeamableAssistantMenuItemSeparator", order = 0)]
	public sealed class BeamableAssistantMenuItemSeparator : BeamableAssistantMenuItem
	{
		public override GUIContent RenderLabel(EditorAPI beamableApi)
		{
			Text = "";
			return base.RenderLabel(beamableApi);
		}

		public override void OnItemClicked(EditorAPI beamableApi) { }
	}
}
