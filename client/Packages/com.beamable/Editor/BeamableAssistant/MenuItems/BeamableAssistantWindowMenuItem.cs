using System.Linq;
using Beamable.Editor.Assistant;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	/// <summary>
	/// Menu Item that opens the <see cref="BeamableAssistantWindow"/> when clicked.
	/// </summary>
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenAssistantWindowMenuItem", menuName = "Beamable/Assistant/Toolbar Menu Items/Assistant Window", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
	public class BeamableAssistantWindowMenuItem : BeamableAssistantMenuItem
	{
		public override GUIContent RenderLabel(EditorAPI beamableApi)
		{
			var _hintNotificationManager = default(BeamHintNotificationManager);
			BeamEditor.GetBeamHintSystem(ref _hintNotificationManager);
			
			var numNotifications = _hintNotificationManager.AllPendingNotifications.Count();
			
			var label = $"{Text}";
			label += numNotifications > 0 ? $" - ({numNotifications})" : "";
			return new GUIContent(label);
		}

		public override void OnItemClicked(EditorAPI beamableApi)
		{
			BeamableAssistantWindow.ShowWindow();
		}
	}
}
