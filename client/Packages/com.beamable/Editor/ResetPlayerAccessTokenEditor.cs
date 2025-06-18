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
					var token = BeamEditorContext.Default.Requester.Token;
					// BeamEditorContext.Default.EditorAccountService.Clear();
					var storage = BeamEditorContext.Default.ServiceScope.GetService<AccessTokenStorage>();
					storage.DeleteTokenForCustomer(token.Cid);
					storage.DeleteTokenForRealm(token.Cid, token.Pid);
					BeamEditorContext.Default.Requester.DeleteToken();

				}

				if (GUILayout.Button("Corrupt Tokens"))
				{
					BeamEditorContext.Default.Requester.Token.CorruptAccessToken();
					BeamEditorContext.Default.Requester.Token.SaveAsCustomerScoped();
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
					storage.ClearDeviceRefreshTokens(api.BeamCli.Cid, api.BeamCli.Pid);
					storage.DeleteTokenForRealm(api.BeamCli.Cid, api.BeamCli.Pid).Then(_ =>
					{
						Close();
					});

				}
			}
		}
	}
}
