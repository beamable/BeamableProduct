using Beamable.Common;
using Beamable.Editor.Assistant;
using Beamable.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	/// <summary>
	/// A sample button that can be used as a reference to understand how to extend the Unity editor's toolbar.  
	/// </summary>
	[CreateAssetMenu(menuName = "Beamable/Assistant/Toolbar/Buttons/Sample Button", fileName = "SampleBeamableButton", order = 0)]
	public class SampleBeamableButton : BeamableToolbarButton
	{
		public override bool ShouldDisplayButton(EditorAPI editorAPI) => editorAPI.HasCustomer;
		public override void OnButtonClicked(EditorAPI editorAPI) => BeamableLogger.Log($"I'm a working beamable button for customer: {editorAPI.User.email}");

		public override GenericMenu GetDropdownOptions(EditorAPI editorAPI)
		{
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Open Beamable Assistant 😃"), false, () => BeamableAssistantWindow.ShowWindow());
			menu.AddItem(new GUIContent("Open Beamable Content 💼"), false, async () => await ContentManagerWindow.Init());

			return menu;
		}
	}
}
