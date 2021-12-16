using Beamable.Editor;
using UnityEngine;

namespace Editor.Beamable.ToolbarExtender
{
	[CreateAssetMenu(menuName = "Beamable/Assistant/MenuItems", fileName = "BeamableAssistantMenuItemSeparator", order = 0)]
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
