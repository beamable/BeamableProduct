using Beamable.Editor.Toolbox.UI;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	/// <summary>
	/// Menu Item that opens the <see cref="ToolboxWindow"/> when clicked.
	/// </summary>
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenToolboxWindowMenuItem", menuName = "Beamable/Toolbar/Menu Items/Toolbox Window", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
	public class BeamableToolboxWindowMenuItem : BeamableToolbarMenuItem
	{
		public override void OnItemClicked(BeamEditorContext beamableApi)
		{
			ToolboxWindow.Init();
		}
	}
}
