using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow2
	{
		void DrawStorage(BeamManifestStorageEntry storage)
		{
			var clickedOpenCode = false;
			var clickedOpenDocs = false;
			var clickedOpenMenu = false;
			var clickedRunToggle = false;
			
			
			if (!usam.TryGetStatus(storage.beamoId, out var status))
			{
				EditorGUILayout.LabelField("Loading service...");
				return;
			}
			
			var isRunning = usam.IsRunningLocally(status);
			var isLoading = usam.IsLoadingLocally(status);

			{ // draw any loading bar we need...
				var loadingRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true),
				                         GUILayout.Height(4));
				if (usam.TryGetExistingAction(status.service, out var progress))
				{
					var shouldAnimate = !progress.isComplete;
					BeamGUI.LoadingRect(loadingRect, progress.progressRatio, 
					                    isFailed: progress.isFailed,
					                    animate: shouldAnimate);
				}
			}
			
			{ // draw toolbar bar
				
				// draw the background of the bar
				const int toolbarHeight = 30;
				const int buttonWidth = 40;
				var buttonBackgroundColor = new Color(.5f, .5f, .5f, .5f);

				var lastRect = GUILayoutUtility.GetLastRect();
				var backRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true),
				                                        GUILayout.Height(1));
				EditorGUI.DrawRect(new Rect(backRect.x, lastRect.yMax, backRect.width, toolbarHeight), new Color(0, 0, 0, .25f));
				
				
				EditorGUILayout.BeginHorizontal(new GUIStyle(), GUILayout.ExpandWidth(true), GUILayout.MinHeight(toolbarHeight));

			
				var badgeCount = DrawBadges(storage.Flags);

				EditorGUILayout.Space(1, true);

				var icon = BeamGUI.iconPlay;
				int iconPadding = 0;
				if (isLoading)
				{
					icon = BeamGUI.GetSpinner(storage.csprojPath?.Length ?? 0);
					iconPadding = 2;
				}
				clickedRunToggle = BeamGUI.HeaderButton(null, icon, 
				                                     width: buttonWidth, 
				                                     padding: 4,
				                                     iconPadding: iconPadding,
				                                     xOffset: (int)((buttonWidth * 3 - badgeCount) * -.5f), // number of buttons to the right, split by half
				                                     backgroundColor: isRunning ? primaryColor : buttonBackgroundColor,
				                                     tooltip: isRunning ? "Shutdown the storage " : "Start the storage");
				EditorGUILayout.Space(1, true);
				// GUI.enabled = true;

				clickedOpenDocs = BeamGUI.HeaderButton(null, BeamGUI.iconOpenMongoExpress,
				                                       width: buttonWidth,
				                                       padding: 4,
				                                       backgroundColor: Color.clear,
				                                       tooltip: "open Mongo Express");
				clickedOpenCode = BeamGUI.HeaderButton(null, BeamGUI.iconOpenProject,
				                                       width: buttonWidth,
				                                       padding: 4,
				                                       backgroundColor: Color.clear,
				                                       tooltip: "open source code");

				clickedOpenMenu = BeamGUI.HeaderButton(null, BeamGUI.iconMoreOptions,
				                                       width: buttonWidth,
				                                       padding: 4,
				                                       backgroundColor: Color.clear,
				                                       tooltip: "extra options");
				
				EditorGUILayout.EndHorizontal();
				
				AddDelayedAction(() =>
				{
					if (clickedRunToggle)
					{
						CheckDocker("start a Storage Object", () =>
						{
							usam.ToggleRun(storage, status);
						}, out _);
					}
					
					if (clickedOpenCode)
					{
						usam.OpenProject(storage.beamoId, storage.csprojPath);
					}

					if (clickedOpenDocs)
					{
						usam.OpenMongo(storage.beamoId);
					}

					if (clickedOpenMenu)
					{
						ShowStorageMenu(storage);
					}
				});
			}

			{ // draw logs
				if (!usam.TryGetLogs(storage.beamoId, out var log))
				{
					log = new UsamService.NamedLogView
					{
						beamoId = storage.beamoId, logView = new LogView(), logs = new List<CliLogMessage>()
					};
					usam._namedLogs.Add(log);
				}
				
				if (!_beamoIdToLogProvider.TryGetValue(storage.beamoId, out var provider))
				{
					provider = _beamoIdToLogProvider[storage.beamoId] = new CliLogDataProvider(log.logs);
					log.logView.BuildView(provider);
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(4, expand:false);
				DrawLogs(log,provider);
				
				EditorGUILayout.Space(4, expand:false);

				EditorGUILayout.EndHorizontal();
			}
		}

		public void ShowStorageMenu(BeamManifestStorageEntry service)
		{
			var menu = new GenericMenu();
			
			menu.AddItem(new GUIContent("Open MongoExpress"), false, () =>
			{
				usam.OpenMongo(service.beamoId);
			});
			menu.AddItem(new GUIContent("Open project"), false, () =>
			{
				usam.OpenProject(service.beamoId, service.csprojPath);
			});
			
			menu.AddItem(new GUIContent("Copy Local Connection String"), false, () =>
			{
				usam.GetLocalConnectionString(service).Then(connStr =>
				{
					EditorGUIUtility.systemCopyBuffer = connStr;
					Debug.Log("Copied connection string: " + connStr);
				});
			});

			
			menu.AddSeparator("");
			
			menu.AddItem(new GUIContent("Go to deployed storages"), false, () =>
			{
				usam.OpenPortalToReleaseSection();
			});
			
			menu.AddSeparator("");
		
			menu.AddItem(new GUIContent("Create Local Snapshot"), false, () =>
			{
				var dest = EditorUtility.OpenFolderPanel("Select where to save snapshot", "", "default");
				if (string.IsNullOrEmpty(dest)) return;
				Debug.Log("Starting snapshot...");
				
				usam.SnapshotMongo(service, dest);
			});
			
			menu.AddItem(new GUIContent("Restore Local Snapshot"), false, () =>
			{
				var dest = EditorUtility.OpenFolderPanel("Select where to load snapshot", "", "default");
				if (string.IsNullOrEmpty(dest)) return;
				Debug.Log("Loading snapshot...");
				usam.RestoreMongo(service, dest);
			});
			
			menu.AddItem(new GUIContent("Erase data"), false, () =>
			{
				var confirm = EditorUtility.DisplayDialog(
					title: "Erase Storage Object's Content",
					message: $"Are you sure you want to erase the data inside the {service.beamoId} storage object? " +
					         $"The data will be deleted locally. ",
					ok: "Erase");
				if (confirm)
				{
					usam.EraseMongo(service);
				}
			});

			
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Delete storage"), false, () =>
			{
				var confirm = EditorUtility.DisplayDialog($"Delete {service.beamoId}",
				                                          @"Are you sure you want to delete all the local source code for the storage? This will not remove any deployed storages until a Release action is taken. ",
				                                          "Delete", "Cancel");
				if (confirm)
				{
					AddDelayedAction(() =>
					{ 
						// run this as a delayed action because it can change the layout of the GUI! 
						//  and that can cause IMGUI to go bananas
						selectedBeamoId = null;
						usam.DeleteProject(service.beamoId, service.csprojPath);
					});
				}
			});
			
			menu.ShowAsContext();
		}
	}
}
