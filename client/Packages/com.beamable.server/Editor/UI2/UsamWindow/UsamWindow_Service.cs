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
		void DrawService(BeamManifestServiceEntry service)
		{
			var clickedOpenCode = false;
			var clickedOpenDocs = false;
			var clickedOpenMenu = false;
			var clickedRunToggle = false;
			
			
			if (!usam.TryGetStatus(service.beamoId, out var status))
			{
				EditorGUILayout.LabelField("Loading service...");
				return;
			}
			
			var isRunning = usam.IsRunningLocally(status);

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
				const int buttonWidth = 30;
				var buttonBackgroundColor = new Color(.5f, .5f, .5f, .5f);

				var lastRect = GUILayoutUtility.GetLastRect();
				var backRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true),
				                                        GUILayout.Height(1));
				EditorGUI.DrawRect(new Rect(backRect.x, lastRect.yMax, backRect.width, toolbarHeight), new Color(0, 0, 0, .25f));
				
				
				EditorGUILayout.BeginHorizontal(new GUIStyle(), GUILayout.ExpandWidth(true), GUILayout.MinHeight(toolbarHeight));

				EditorGUILayout.Space(1, true);

				// GUI.enabled = false;
				clickedRunToggle = BeamGUI.HeaderButton(null, iconPlay, 
				                                     width: buttonWidth, 
				                                     padding: 4,
				                                     xOffset: (int)((buttonWidth * 3) * -.5f), // number of buttons to the right, split by half
				                                     backgroundColor: isRunning ? primaryColor : buttonBackgroundColor,
				                                     tooltip: isRunning ? "Shutdown the service " : "Start the service");
				EditorGUILayout.Space(5, true);
				// GUI.enabled = true;

				clickedOpenDocs = BeamGUI.HeaderButton(null, iconOpenApi,
				                                       width: buttonWidth,
				                                       padding: 4,
				                                       backgroundColor: Color.clear,
				                                       tooltip: "open Open API");
				clickedOpenCode = BeamGUI.HeaderButton(null, iconOpenProject,
				                                       width: buttonWidth,
				                                       padding: 4,
				                                       backgroundColor: Color.clear,
				                                       tooltip: "open source code");

				clickedOpenMenu = BeamGUI.HeaderButton(null, iconMoreOptions,
				                                       width: buttonWidth,
				                                       padding: 4,
				                                       backgroundColor: Color.clear,
				                                       tooltip: "extra options");
				
				EditorGUILayout.EndHorizontal();
				
				AddDelayedAction(() =>
				{
					if (clickedRunToggle)
					{
						if (service.storageDependencies.Count > 0)
						{
							CheckDocker("start a service with a Storage Object dependency", () =>
							{
								usam.ToggleRun(service, status);
							}, out _);
						}
					}
					
					if (clickedOpenCode)
					{
						usam.OpenProject(service.beamoId, service.csprojPath);
					}

					if (clickedOpenDocs)
					{
						usam.OpenSwagger(service.beamoId, false);
					}

					if (clickedOpenMenu)
					{
						ShowServiceMenu(service);
					}
				});
			}

			{ // draw logs
				if (!usam.TryGetLogs(service.beamoId, out var log))
				{
					log = new UsamService.NamedLogView
					{
						beamoId = service.beamoId, logView = new LogView(), logs = new List<CliLogMessage>()
					};
					usam._namedLogs.Add(log);
				}
				
				if (!_beamoIdToLogProvider.TryGetValue(service.beamoId, out var provider))
				{
					provider = _beamoIdToLogProvider[service.beamoId] = new CliLogDataProvider(log.logs);
					log.logView.BuildView(provider);
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space(4, expand:false);
				this.DrawLogWindow(log.logView, provider, () =>
				{
					log.logs.Clear();
					log.logView.RebuildView();
				});
				EditorGUILayout.Space(4, expand:false);

				EditorGUILayout.EndHorizontal();
			}
		}

		public void ShowServiceMenu(BeamManifestServiceEntry service)
		{
			var menu = new GenericMenu();
			// the openAPI and project buttons are in the same order as the buttons on the toolbar
			
			menu.AddItem(new GUIContent("Open openAPI"), false, () =>
			{
				usam.OpenSwagger(service.beamoId);
			});
			menu.AddItem(new GUIContent("Open project"), false, () =>
			{
				usam.OpenProject(service.beamoId, service.csprojPath);
			});
			menu.AddItem(new GUIContent("Generate client"), false, () =>
			{
				// TODO:
				throw new NotImplementedException("add client generation!");
			});
			
			menu.AddSeparator("");
			
			menu.AddItem(new GUIContent("Go to deployed services"), false, () =>
			{
				throw new NotImplementedException("open up the remote portal page");
			});
			
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Delete service"), false, () =>
			{
				var confirm = EditorUtility.DisplayDialog($"Delete {service.beamoId}",
				                            @"Are you sure you want to delete all the local source code for the service? This will not remove any deployed services until a Release action is taken. ",
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
