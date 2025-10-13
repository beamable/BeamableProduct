using Beamable.Config;
using Beamable.Editor.BeamCli;
using Beamable.Editor.Content;
using Beamable.Editor.Library;
using Beamable.Editor.Microservice.UI2;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Accounts
{
	public partial class AccountWindow
	{
		private Vector2 _signedInScroll;

		void Draw_SignedIn()
		{
			
			void RenderLabel(string label, string value)
			{
				EditorGUILayout.LabelField(label, _titleStyle);
				EditorGUILayout.SelectableLabel(value, _textboxStyle);
				if (string.IsNullOrEmpty(value))
				{
					var rect = GUILayoutUtility.GetLastRect();
					var spinnerTex = BeamGUI.GetSpinner();
					var spinnerRect = new Rect(rect.xMax - (spinnerTex.width + 8), rect.y - 5, spinnerTex.width,
					                           rect.height);
					GUI.DrawTexture(spinnerRect, spinnerTex, ScaleMode.ScaleToFit);
				}
			}
			
			EditorGUILayout.BeginVertical(new GUIStyle
			{
				padding = new RectOffset(12, 12, 12, 12)
			});
			
			_signedInScroll = EditorGUILayout.BeginScrollView(_signedInScroll);

			BeamGUI.DrawLogoBanner();
			
			EditorGUILayout.Space(12);

			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(1, true);

				
				{
					EditorGUILayout.BeginVertical(GUILayout.Width(280), GUILayout.ExpandHeight(true));

					EditorGUILayout.LabelField("These are your build connection strings. ", _headerStyle,
					                           GUILayout.ExpandWidth(true));

					EditorGUILayout.Space(27);

					{
						var config = context.ServiceScope.GetService<ConfigDatabaseProvider>();
						RenderLabel("Runtime Host", config.HostUrl);
						RenderLabel("Runtime Cid", config.Cid);

						var realmDisplay = config.Pid;
						if (cli.pidToRealm.TryGetValue(config.Pid, out var realm))
						{
							realmDisplay = realm.GetDisplayName();
						}
						RenderLabel("Runtime Realm", realmDisplay);
						
						// var clickedOpen = BeamGUI.SoftRightLinkButton(new GUIContent("Select config-defaults.txt"));
						// if (clickedOpen)
						// {
						// 	EditorApplication.delayCall += () =>
						// 	{
						// 		var asset = Resources.Load<TextAsset>(ConfigDatabaseProvider.CONFIG_DEFAULTS_NAME);
						// 		Selection.SetActiveObjectWithContext(asset, null);
						// 	};
						// }
					}

					EditorGUILayout.EndVertical();
				}

				EditorGUILayout.Space(12, false);
				
				{
					EditorGUILayout.BeginVertical(GUILayout.Width(280), GUILayout.ExpandHeight(true));

					EditorGUILayout.LabelField("You are logged in. These are your editor connection strings.", _headerStyle,
					                           GUILayout.ExpandWidth(true));

					EditorGUILayout.Space(12);


					{
						RenderLabel("Editor Host", cli.latestRouteInfo.apiUri);
						RenderLabel("Editor Cid", cli.latestRealmInfo?.Cid);
						RenderLabel("Editor Realm", cli.CurrentRealm?.GetDisplayName());
						RenderLabel("Editor Organization", cli.latestRealmInfo?.CustomerAlias);
						RenderLabel("Editor Game", cli.ProductionRealm?.ProjectName);

						var userLabel = $"[{cli.latestUser?.GetPermissionsForRealm(cli.CurrentRealm?.Pid)?.Role}] {cli.latestUser?.email}";
						if (string.IsNullOrEmpty(cli.latestUser?.email))
						{
							userLabel = null;
						}
						RenderLabel("Editor User", userLabel);

					}

					{
						// render some buttons...

						var clickedChangeGame = BeamGUI.SoftRightLinkButton(new GUIContent("Change Game"));
						var clickedManageAccount = BeamGUI.SoftRightLinkButton(new GUIContent("Manage Account"));

						EditorGUILayout.Space(12);

						var clickedCommitConfig = BeamGUI.SoftRightLinkButton(new GUIContent("Copy editor connection strings to build"));

						EditorGUILayout.Space(24);

						var clickedLogout = BeamGUI.CancelButton("Log Out");

						EditorGUILayout.Space(4);

						if (_onQuitAction == null)
						{
							_onQuitAction = BeamLibraryWindow.Init;
							_onQuitName = "Open Library";
						}

						var clickedLibrary = BeamGUI.PrimaryButton(new GUIContent(_onQuitName), allowEnterKeyToClick: true);

						if (clickedChangeGame)
						{
							needsGameSelection = true;
						}

						if (clickedCommitConfig)
						{
							context.CommitConfigDefaults();
						}

						if (clickedManageAccount)
						{
							EditorApplication.delayCall += () =>
							{
								var url = $"{BeamableEnvironment.PortalUrl}/{cli.Alias}/account";
								Application.OpenURL(url);
							};
						}

						if (clickedLogout)
						{
							EditorApplication.delayCall += () => { context.Logout(false); };
						}

						if (clickedLibrary)
						{
							EditorApplication.delayCall += () =>
							{
								_onQuitAction?.Invoke();
								// GetWindow(windowType, true);
								Close();
								// BeamLibraryWindow.Init();
							};
						}
					}
				}
				EditorGUILayout.EndVertical();
			}

			
			EditorGUILayout.Space(1, true);

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}
	}
}
