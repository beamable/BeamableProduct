using Beamable.Editor.Login.UI;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "AccountMenuItem", menuName = "Beamable/Toolbar/Menu Items/Account Picker", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
	public class AccountMenuItem : BeamableToolbarMenuItem
	{
		public override GUIContent RenderLabel(BeamEditorContext editorApi)
		{
			if (editorApi.IsAuthenticated)
			{
				return new GUIContent("Log Out");
			}
			else
			{
				return new GUIContent("Log In");
			}
		}

		public override void ContextualizeMenu(BeamEditorContext editorApi, GenericMenu menu)
		{
			if (editorApi.IsAuthenticated)
			{
				menu.AddItem(new GUIContent($"Account: {editorApi.CurrentUser.email}"), false, () =>
				{
					var _ = LoginWindow.Init();
				});
				// menu.AddItem(new GUIContent("Log Out"), false, () =>
				// {
				// 	editorApi.Logout(false);
				// });
				//
			}
			else
			{
				// menu.AddItem(new GUIContent("Log In"), false, () =>
				// {
				// 	var _ = LoginWindow.CheckLogin();
				// });
			}
		}

		public override void OnItemClicked(BeamEditorContext editorApi)
		{
			if (editorApi.IsAuthenticated)
			{
				editorApi.Logout(false);
			}
			else
			{
				var _ = LoginWindow.CheckLogin();
			}
		}
	}
}