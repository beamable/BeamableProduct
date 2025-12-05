using Beamable.Config;
using Beamable.Editor.BeamCli;
using Beamable.Editor.Content;
using Beamable.Editor.Library;
using Beamable.Editor.Microservice.UI2;
using Beamable.Editor.Util;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Accounts
{
	public partial class AccountWindow
	{
		private Vector2 _signedInScroll;
		private const string RUNTIME_INFO_IS_NOT_SET = "The build runtime {0}: <b>[{1}]</b> {2} not set. Please set your editor Realm to a desired Realm and set the value by clicking on: ";

		void Draw_SignedIn()
		{
			
			void RenderLabel(string label, string value, string placeholder = "")
			{
				bool isValueNullOrEmpty = string.IsNullOrEmpty(value);
				EditorGUILayout.LabelField(label, _titleStyle);
				if (!isValueNullOrEmpty)
				{
					EditorGUILayout.SelectableLabel(value, _textboxStyle);
				}
				else
				{
					EditorGUILayout.LabelField(placeholder, _textboxPlaceholderStyle);
					if (string.IsNullOrEmpty(placeholder))
					{
						var rect = GUILayoutUtility.GetLastRect();
						var spinnerTex = BeamGUI.GetSpinner();
						var spinnerRect = new Rect(rect.xMax - (spinnerTex.width + 8), rect.y - 5, spinnerTex.width,
						                           rect.height);
						GUI.DrawTexture(spinnerRect, spinnerTex, ScaleMode.ScaleToFit);
					}
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
						RenderLabel("Runtime Host", config.HostUrl, "No value set");
						RenderLabel("Runtime Cid", config.Cid, "No value set");

						var realmDisplay = config.Pid;
						if (cli.pidToRealm.TryGetValue(config.Pid, out var realm))
						{
							realmDisplay = realm.GetDisplayName();
						}
						RenderLabel("Runtime Realm", realmDisplay, "No value set");

						List<string> missingRuntimeFields = new List<string>();
						if (string.IsNullOrEmpty(config.HostUrl))
						{
							missingRuntimeFields.Add("host");
						}

						if (string.IsNullOrEmpty(config.Cid))
						{
							missingRuntimeFields.Add("cid");
						}

						if (string.IsNullOrEmpty(realmDisplay))
						{
							missingRuntimeFields.Add("realm");
						}
						
						if (missingRuntimeFields.Count > 0)
						{
							GUIStyle guiStyle = new GUIStyle(EditorStyles.label)
							{
								wordWrap = true,
								richText = true,
								padding = new RectOffset(0, 0, 10, 0)
							};
							var valueInfo = missingRuntimeFields.Count > 1 ? "values" : "value";
							var missingFields = string.Join(", ", missingRuntimeFields);
							var missingFieldsText = missingRuntimeFields.Count > 1 ? "are" : "is";
							EditorGUILayout.LabelField(string.Format(RUNTIME_INFO_IS_NOT_SET, valueInfo, missingFields, missingFieldsText), guiStyle);
							if (BeamGUI.SoftLeftLinkButton(new GUIContent("Copy editor connection strings to build")))
							{
								context.CommitConfigDefaults();
							}
						}
						
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
