using Beamable.Editor.Config;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	/// <summary>
	/// Menu Item that opens the <see cref="CoreConfiguration"/> when clicked.
	/// </summary>
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenConfigurationManagerMenuItem", menuName = "Beamable/Toolbar/Menu Items/Configuration Manager Window", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
	public class BeamableConfigurationManagerMenuItem : BeamableToolbarMenuItem
	{
		public override void OnItemClicked(BeamEditorContext beamableApi)
		{
			ConfigManager.Initialize(forceCreation: true);
			SettingsService.OpenProjectSettings("Project/Beamable");
		}
	}
}
