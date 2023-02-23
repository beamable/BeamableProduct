using Beamable.Api;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.MenuItems.Windows;

namespace Beamable.Editor
{
	public static class ResetPlayerAccessTokenEditor
	{
		[MenuItem(Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Clear Access Token")]
		public static void ResetPlayerAccessTokens()
		{
			var wnd = ScriptableObject.CreateInstance<ResetTokenWindow>();
			wnd.Show();
		}

		class ResetTokenWindow : EditorWindow
		{
			private string Prefix;
			private void OnGUI()
			{
				EditorGUILayout.LabelField("Editor (the token used in edit mode)");
				if (GUILayout.Button("Clear Tokens"))
				{
					BeamEditorContext.Default.EditorAccountService.Clear();
					BeamEditorContext.Default.Requester.DeleteToken();
				}

				EditorGUILayout.LabelField("Runtime (the token used when you enter playmode)");
				
				Prefix = EditorGUILayout.TextField("PlayerCode", Prefix);
				if (GUILayout.Button("Cancel"))
				{
					Close();
					return;
				}
				if (GUILayout.Button("Clear Token"))
				{
					var api = BeamEditorContext.Default;
					var storage = new AccessTokenStorage(Prefix);
					storage.ClearDeviceRefreshTokens(api.CurrentCustomer.Cid, api.CurrentRealm.Pid);
					storage.DeleteTokenForRealm(api.CurrentCustomer.Cid, api.CurrentRealm.Pid).Then(_ =>
					{
						Close();
					});

				}
			}
		}
	}
}
