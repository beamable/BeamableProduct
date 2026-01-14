using System;
using System.Linq;
using Beamable.Common;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Accounts
{
	public partial class AccountWindow
	{
		public bool needsGameSelection;
		public int gameSelectionIndex = 0;
		public int realmSelectionIndex = 0;

		[NonSerialized]
		public string[] realmNames;
		[NonSerialized]
		public BeamOrgRealmData[] availableRealms; // parallel to realmNames
		
		[NonSerialized]
		private Promise _realmCommandPromise;
		[NonSerialized]
		private OrgRealmsWrapper _realmsCommand;
		[NonSerialized]
		private BeamOrgRealmData[] _visibleRealms;

		void Draw_Games()
		{
			EditorGUILayout.BeginVertical(new GUIStyle
			{
				padding = new RectOffset(12, 12, 12, 12)
			});

			BeamGUI.DrawLogoBanner();
			
			EditorGUILayout.Space(12);

			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(1, true);
				EditorGUILayout.BeginVertical(new GUIStyle
				{
					// padding = new RectOffset(24, 24, 0, 0)
				}, GUILayout.Width(280));

				EditorGUILayout.LabelField("Welcome! Please select a game and realm.", _headerStyle,
				                           GUILayout.ExpandWidth(true));

				EditorGUILayout.Space(12);

				{
					EditorGUILayout.LabelField("Select a game", _titleStyle);
					
					var choices = cli.latestGames.VisibleGames.Select(x => x.ProjectName.Replace("-prod", "")).ToArray();
					if (gameSelectionIndex < 0 || gameSelectionIndex >= choices.Length)
					{
						gameSelectionIndex = 0;
					}
					
					var old = gameSelectionIndex;
					gameSelectionIndex = EditorGUILayout.Popup(gameSelectionIndex, choices, _textboxStyle);
					if (old != gameSelectionIndex)
					{
						// made a selection!
						UpdateRealmsForGame();
					}
				}

				{
					if (realmNames == null && _realmsCommand == null)
					{
						UpdateRealmsForGame();
					}
					
					EditorGUILayout.Space(4);
					EditorGUILayout.LabelField("Select a realm", _titleStyle);
					
					if (realmNames == null)
					{
						GUI.enabled = false;
						EditorGUILayout.Popup(0, new string[]{"(Fetching Realms...)"}, _textboxStyle);
						var rect = GUILayoutUtility.GetLastRect();
						var spinnerTex = BeamGUI.GetSpinner();
						var spinnerRect = new Rect(rect.xMax - (spinnerTex.width + 8) , rect.y + 2, spinnerTex.width, rect.height);
						GUI.DrawTexture(spinnerRect, spinnerTex, ScaleMode.ScaleToFit);

						GUI.enabled = true;
					}
					else
					{
						if (realmSelectionIndex < 0 || realmSelectionIndex >= realmNames.Length)
						{
							realmSelectionIndex = 0;
						}
						realmSelectionIndex = EditorGUILayout.Popup(realmSelectionIndex, realmNames, _textboxStyle);
					}
					

				}

				
				EditorGUILayout.Space(24);

				{
					GUI.enabled = realmSelectionIndex >= 0;
					var wasClicked = BeamGUI.PrimaryButton(new GUIContent("Continue"), allowEnterKeyToClick: true);
					GUI.enabled = true;
					
					if (wasClicked)
					{
						var _ = context.SwitchRealm(availableRealms[realmSelectionIndex].Pid);
						needsGameSelection = false;
					}
				}
				
				
				EditorGUILayout.EndVertical();
				
				EditorGUILayout.Space(1, true);
				EditorGUILayout.EndHorizontal();

			}


			EditorGUILayout.EndVertical();
		}

		void UpdateRealmsForGame()
		{
			if (_realmsCommand != null)
			{
				_realmsCommand.Cancel();
				_realmsCommand = null;
			}

			realmNames = null;
			realmSelectionIndex = -1;
			
			try
			{
				if (!cli.CanBuildCommands) return;
				var command = cli.Command;
				command.ModifierNextDefault(args =>
				{
					args.pid = cli.latestGames.VisibleGames[gameSelectionIndex].Pid;
				});
				_realmsCommand = command.OrgRealms(new OrgRealmsArgs());
				_realmsCommand.OnStreamRealmsListCommandOutput(dp =>
				{
					var data = dp.data;
					_visibleRealms = data.VisibleRealms;
					availableRealms = data.VisibleRealms
					            .Where(x => x.ProjectName ==
					                        cli.latestGames.VisibleGames[gameSelectionIndex].ProjectName).ToArray();
					realmNames = new string[availableRealms.Length];
					for (var i = 0; i < availableRealms.Length; i++)
					{
						realmNames[i] = $"{availableRealms[i].RealmName} - {availableRealms[i].Pid}";
					}
				});

				_realmCommandPromise = _realmsCommand.Run();
				_realmCommandPromise.Then(_ => { _realmsCommand = null; });
				_realmCommandPromise.Error(_ => { _realmsCommand = null; });
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
				// drop it
			}
			finally
			{
				
			}
		}
	}
}
