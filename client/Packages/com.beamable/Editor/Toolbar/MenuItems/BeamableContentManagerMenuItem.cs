using Beamable.Editor.Content;
using Beamable.Editor.UI.ContentWindow;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	/// <summary>
	/// Menu Item that opens the <see cref="Content.ContentManagerWindow"/> when clicked.
	/// </summary>
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenContentManagerMenuItem", menuName = "Beamable/Toolbar/Menu Items/Content Manager Window", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
	public class BeamableContentManagerMenuItem : BeamableToolbarMenuItem
	{
		public override async void OnItemClicked(BeamEditorContext beamableApi)
		{
			await ContentWindow.Init();
		}
	}
}
