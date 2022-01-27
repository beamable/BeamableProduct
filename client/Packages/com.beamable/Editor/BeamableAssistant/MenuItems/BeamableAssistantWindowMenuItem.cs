using Beamable.Editor.Assistant;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	/// <summary>
	/// Menu Item that opens the <see cref="BeamableAssistantWindow"/> when clicked.
	/// </summary>
	[CreateAssetMenu(fileName = "OpenAssistantWindowMenuItem", menuName = "Beamable/Assistant/Toolbar/Menu Items/Assistant Window", order = 0)]
	public class BeamableAssistantWindowMenuItem : BeamableAssistantMenuItem
	{
		public override void OnItemClicked(EditorAPI beamableApi)
		{
			BeamableAssistantWindow.ShowWindow();
		}
	}
}
