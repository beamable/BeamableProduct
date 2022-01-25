// unset

using Beamable.Api;
using Beamable.UI.Scripts;
using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor
{
	public static class ResetPlayerAccessTokenEditor
	{
		[MenuItem(BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Clear Access Token")]
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
				Prefix = EditorGUILayout.TextField("PlayerCode", Prefix);
				if (GUILayout.Button("Cancel"))
				{
					Close();
					return;
				}
				if (GUILayout.Button("Clear Token"))
				{
					EditorAPI.Instance.Then(api =>
					{
						var storage = new AccessTokenStorage(Prefix);
						storage.ClearDeviceRefreshTokens(api.Cid, api.Pid);
						storage.DeleteTokenForRealm(api.Cid, api.Pid).Then(_ =>
						{
							Debug.Log("Success!");
							Close();
						});
					});
				}
			}
		}
	}
}
