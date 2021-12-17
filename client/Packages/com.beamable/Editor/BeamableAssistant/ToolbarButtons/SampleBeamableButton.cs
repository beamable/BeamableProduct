using Beamable.Common;
using Beamable.Editor.Assistant;
using Beamable.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	[CreateAssetMenu(menuName = "Beamable/Assistant/Toolbar/Buttons/Sample Button", fileName = "SampleBeamableButton", order = 0)]
	public class SampleBeamableButton : BeamableToolbarButton
	{
		public override bool ShouldDisplayButton(EditorAPI editorAPI) => editorAPI.HasCustomer;
		public override Side GetButtonSide(EditorAPI editorAPI) => Side.Left;
		public override int GetButtonOrder(EditorAPI editorAPI) => 0;

		public override void OnButtonClicked(EditorAPI editorAPI) => BeamableLogger.Log($"I'm a working beamable button for customer: {editorAPI.User.email}");

		public override GenericMenu GetDropdownOptions(EditorAPI editorAPI)
		{
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Open Beamable Assistant 😃"), false, BeamableAssistantWindow.ShowWindow);
			menu.AddItem(new GUIContent("Open Beamable Content 💼"), false, async () => await ContentManagerWindow.Init());

			return menu;
		}
	}
}
