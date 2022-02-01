using Beamable.Editor.Assistant;
using Beamable.Editor.Config;
using Beamable.Editor.Content;
using Beamable.Editor.Toolbox.UI;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
    /// <summary>
    /// Menu Item that opens the <see cref="BeamableAssistantWindow"/> when clicked.
    /// </summary>
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenMicroserviceManagerMenuItem", menuName = "Beamable/Assistant/Toolbar Menu Items/Microservice Manager Window", order = BeamableMenuItemScriptableObjectCreationOrder + 1)]
#endif
    public class BeamableMicroserviceManagerMenuItem : BeamableAssistantMenuItem
    {
        public override void OnItemClicked(EditorAPI beamableApi)
        {
            Microservice.UI.MicroserviceWindow.Init();
        }
    }
}
