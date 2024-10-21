using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	/// <summary>
	/// Menu Item that opens the <see cref="Beamable.Editor.Microservice.UI.MicroserviceWindow"/> when clicked.
	/// </summary>
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenMicroserviceManagerMenuItem", menuName = "Beamable/Toolbar/Menu Items/Microservice Manager Window", order = BeamableMenuItemScriptableObjectCreationOrder + 1)]
#endif
	public class BeamableMicroserviceManagerMenuItem : BeamableToolbarMenuItem
	{
		public override void OnItemClicked(BeamEditorContext beamableApi)
		{
			// Microservice.UI.MicroserviceWindow.Init();
		}
	}
}
