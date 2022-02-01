using Beamable.Editor.Assistant;
using Beamable.Editor.Config;
using Beamable.Editor.Content;
using Beamable.Editor.Toolbox.UI;
using Beamable.Editor.UI.Buss;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
    /// <summary>
    /// Menu Item that opens the <see cref="BeamableAssistantWindow"/> when clicked.
    /// </summary>
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenThemeManagerMenuItem", menuName = "Beamable/Assistant/Toolbar Menu Items/Theme Manager Window", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
    public class BeamableThemeManagerMenuItem : BeamableAssistantMenuItem
    {
        public override void OnItemClicked(EditorAPI beamableApi)
        {
            BussThemeManager.Init();
        }
    }
}
