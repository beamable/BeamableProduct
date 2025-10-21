using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow
	{				
		const int toolbarHeight = 30;

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
			var isLoading = usam.IsLoadingLocally(status);
			var isCreating = usam.IsCreatingLocally(status.service);

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
				const int buttonWidth = 30;
				var buttonBackgroundColor = new Color(.5f, .5f, .5f, .5f);

				var lastRect = GUILayoutUtility.GetLastRect();
				var backRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true),
				                                        GUILayout.Height(1));
				EditorGUI.DrawRect(new Rect(backRect.x, lastRect.yMax, backRect.width, toolbarHeight), new Color(0, 0, 0, .25f));
				
				
				EditorGUILayout.BeginHorizontal(new GUIStyle(), GUILayout.ExpandWidth(true), GUILayout.MinHeight(toolbarHeight));

				
				var badgeCount = DrawBadges(service.Flags);
				
				EditorGUILayout.Space(1, true);

				var icon = BeamGUI.iconPlay;
				int iconPadding = 0;
				if (isLoading)
				{
					icon = BeamGUI.GetSpinner(service.csprojPath?.Length ?? 0);
					iconPadding = 2;
				}

				var isProductionRealm = ActiveContext.BeamCli.CurrentRealm.IsProduction;
				BeamGUI.ShowDisabled(!isProductionRealm && !isCreating, () =>
				{
					var tooltip = isRunning
						? "Shutdown the service "
						: "Start the service";
					if (isProductionRealm)
					{
						tooltip =
							"Cannot start the service on a production realm. Please switch to a non production realm";
					}

					if (isCreating)
					{
						tooltip = "The service is still being created...";
					}

					clickedRunToggle = BeamGUI.HeaderButton(null, icon,
					                                        width: buttonWidth,
					                                        padding: 4,
					                                        iconPadding: iconPadding,
					                                        xOffset: (int)((buttonWidth * (3 - badgeCount)) *
					                                                       -.5f), // number of buttons to the right, split by half
					                                        backgroundColor: isRunning
						                                        ? primaryColor
						                                        : buttonBackgroundColor,
					                                        tooltip: tooltip
					);
				});
				

				EditorGUILayout.Space(1, true);

				clickedOpenDocs = BeamGUI.HeaderButton(null, BeamGUI.iconOpenApi,
				                                       width: buttonWidth,
				                                       padding: 4,
				                                       backgroundColor: Color.clear,
				                                       tooltip: "open Open API");
				clickedOpenCode = BeamGUI.HeaderButton(null, BeamGUI.iconFolder,
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
						if (service.storageDependencies.Count > 0)
						{
							CheckDocker("start a service with a Storage Object dependency", () =>
							{
								usam.ToggleRun(service, status);
							}, out _);
						}
						else
						{
							usam.ToggleRun(service, status);
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
						beamoId = service.beamoId, logView = new LogView
						{
							verbose = new LogLevelView
							{
								enabled = false,
							},
							debug = new LogLevelView
							{
								enabled = false
							}
						}, logs = new List<CliLogMessage>()
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
				
				DrawLogs(log,provider);
				EditorGUILayout.Space(4, expand:false);

				EditorGUILayout.EndHorizontal();
			}
		}

		public void DrawLogs(UsamService.NamedLogView log, LogDataProvider provider)
		{
			this.DrawLogWindow(log.logView, provider, () =>
			{
				log.logs.Clear();
				log.logView.RebuildView();
			}, customClearGui: view =>
			{
				var isClear = GUILayout.Button("clear", new GUIStyle(EditorStyles.toolbarButton), GUILayout.Width(50));
				var clearRect = GUILayoutUtility.GetLastRect();
				var icon = EditorGUIUtility.IconContent("Icon Dropdown");

				var isClearMenu = GUILayout.Button(icon, new GUIStyle(EditorStyles.toolbarButton)
				{
					padding = new RectOffset(2, 2, 4, 4),
				}, GUILayout.ExpandWidth(false));
				if (isClearMenu)
				{
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent("Clear on Start"), view.clearOnPlay, () =>
					{
						view.clearOnPlay = !view.clearOnPlay;
					});
					menu.DropDown(new Rect(clearRect.x, clearRect.yMax, 0, 0));
				}
					
				return isClear;
			});
		}

		public void ShowServiceMenu(BeamManifestServiceEntry service)
		{
			var menu = new GenericMenu();
			// the openAPI and project buttons are in the same order as the buttons on the toolbar
			
			menu.AddItem(new GUIContent("Goto openAPI in Portal"), false, () =>
			{
				usam.OpenSwagger(service.beamoId);
			});
			menu.AddItem(new GUIContent("Open project solution"), false, () =>
			{
				usam.OpenProject(service.beamoId, service.csprojPath);
			});
			
			menu.AddSeparator("");
			
			if (!service.IsReadonlyPackage)
			{
				menu.AddItem(new GUIContent("Generate client"), false, () =>
				{
					var _ = usam.GenerateClient(service);
				});

				menu.AddItem(new GUIContent("Generate client on build"),
				             usam.ShouldServiceAutoGenerateClient(service.beamoId), () =>
				             {
					             usam.ToggleServiceAutoGenerateClient(service.beamoId);
				             });
				menu.AddItem(new GUIContent("Generate Assembly Definition Projects"), false, () =>
				{
					CsProjUtil.OnPreGeneratingCSProjectFiles(usam);
				});
			}


			var hintPath = usam.GetClientFileCandidatePath(service.beamoId);
			if (File.Exists(hintPath))
			{
				menu.AddItem(new GUIContent("Show client in Unity"), false, () =>
				{
					var projectBrowserType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
					EditorWindow.GetWindow(projectBrowserType)?.Focus();
					var asset = AssetDatabase.LoadAssetAtPath(hintPath, typeof(TextAsset));
					EditorGUIUtility.PingObject(asset);
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Show client in Unity"));
			}
			
			menu.AddSeparator("");
			
			menu.AddItem(new GUIContent("Goto deployed services in Portal"), false, () =>
			{
				usam.OpenPortalToReleaseSection();
			});

			if (!service.IsReadonlyPackage)
			{
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
			}

			menu.ShowAsContext();
		}


	}
}
