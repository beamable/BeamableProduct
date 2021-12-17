using Beamable.Editor.Assistant;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	[CreateAssetMenu(fileName = "AssistantMenuItem", menuName = "Beamable/Assistant/Assistant Window", order = 0)]
	public class BeamableAssistantWindowMenuItem : BeamableAssistantMenuItem
	{
		public override void OnItemClicked(EditorAPI beamableApi)
		{
			BeamableAssistantWindow.ShowWindow();
		}
	}
}
