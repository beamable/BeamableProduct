using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.Editor.Microservice.UI2
{
	public class ServicePickerWindow : EditorWindow
	{
		public static bool isOpen;
		public UsamWindow usamWindow;
		public Vector2 scrollPosition;

		const int elementHeight = 39;
		const int buttonWidth = 24;
		const int buttonPadding = 2;
		const int buttonYPadding = 7;

		private const int clickablePadding = 20;

		private void OnDestroy()
		{
			isOpen = false;
		}

		private void OnInspectorUpdate()
		{
			Repaint();
		}

		private void OnGUI()
		{
			isOpen = true;
			var totalElementCount = usamWindow.usam.latestManifest.services.Count +
			                        usamWindow.usam.latestManifest.storages.Count;
			var pos = position;
			minSize = new Vector2(usamWindow.position.width-100, 
			                      totalElementCount * elementHeight + 40);
			position = pos;
			var usam = usamWindow.usam;
			
			
			// _headerHeight = 0f;
			{ // render a header bar
				EditorGUILayout.BeginHorizontal();
				// EditorGUILayout.Space(elementHeight + 4, false); // space for the icon
				EditorGUILayout.LabelField("service name", new GUIStyle(EditorStyles.miniLabel)
				                           {
					                           padding = new RectOffset(elementHeight + 4, 0, 0, 0),
					                           alignment = TextAnchor.MiddleLeft
				                           }, 
				                           GUILayout.Width(position.width - buttonWidth * 4 - clickablePadding));
				EditorGUILayout.LabelField("local actions", new GUIStyle(EditorStyles.miniLabel)
				                           {
					                           padding = new RectOffset(clickablePadding - 2, 0, 0, 0),
					                           alignment = TextAnchor.MiddleLeft
				                           }, 
				                           GUILayout.Width(buttonWidth * 4));

				
				// EditorGUILayout.LabelField("local actions", new GUIStyle(EditorStyles.miniLabel),
				//                            GUILayout.Width(buttonWidth * 4 - 20));
				EditorGUILayout.EndHorizontal();
			}
			
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			EditorGUILayout.BeginVertical();

			var clicked = false;
			var totalIndex = 0;

			
			{ // render the services first, 
				for (var i = 0; i < usam?.latestManifest?.services?.Count; i++, totalIndex++)
				{
					var service = usam.latestManifest.services[i];
					if (DrawService(service, totalIndex))
					{
						clicked = true;
						usamWindow.selectedBeamoId = service.beamoId;
					}
				}

			}
			{ // render the storages second.
				for (var i = 0; i < usam?.latestManifest?.storages?.Count; i++, totalIndex++)
				{
					var storage = usam.latestManifest.storages[i];
					if (DrawStorage(storage, totalIndex))
					{
						clicked = true;
						usamWindow.selectedBeamoId = storage.beamoId;
					}
				}
			}
			
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			if (clicked)
			{
				Close();
			}
		}
		bool DrawCard(string beamoId, int index, Texture icon, Action drawButtons)
		{
			var bounds = new Rect(0, index * elementHeight, position.width, elementHeight);
		
			EditorGUILayout.BeginHorizontal(GUILayout.Height(elementHeight), GUILayout.ExpandWidth(true));


			var clickableRect = new Rect(bounds.x, bounds.y + 4, bounds.width - buttonWidth * 4 - clickablePadding,
			                             bounds.height);
			EditorGUIUtility.AddCursorRect(clickableRect, MouseCursor.Link);

			var isButtonHover = clickableRect.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			{ // draw hover color
				if (beamoId == usamWindow.selectedBeamoId)
				{
					var selectionRect = new Rect(bounds.x, bounds.y, 4, bounds.height);
					EditorGUI.DrawRect(selectionRect, new Color(.25f, .5f, 1f, .8f));
				}
				
				{
					EditorGUI.DrawRect(bounds, new Color(0, 0, 0, index%2 == 0 ? .1f : .2f));
				}
				
				if (isButtonHover)
				{
					EditorGUI.DrawRect(bounds, new Color(1,1,1, .05f));
				}
			}

			// space for icon
			var iconRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Width(elementHeight), GUILayout.Height(elementHeight));
			var paddedIconRect = new Rect(iconRect.x + 16, iconRect.y + 8, iconRect.width - 16, iconRect.height - 16);
			GUI.DrawTexture(paddedIconRect, icon);

			var labelStyle = new GUIStyle(EditorStyles.largeLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(8, 0, 0, 0),
			};
			EditorGUILayout.LabelField(beamoId, labelStyle, GUILayout.MaxWidth(position.width - buttonWidth*4 - elementHeight - 8), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));

			drawButtons();
			EditorGUILayout.EndHorizontal();

			return buttonClicked;
		}

		bool DrawStorage(BeamManifestStorageEntry storage, int index)
		{
			return DrawCard(storage.beamoId, index, BeamGUI.iconStorage, () =>
			{
				var isRunning = false;
				var isLoading = false;
				if (usamWindow.usam.TryGetStatus(storage.beamoId, out var status))
				{
					isRunning = usamWindow.usam.IsRunningLocally(status);
					isLoading = usamWindow.usam.IsLoadingLocally(status);
				}

				GUI.enabled = status != null;
				var icon = BeamGUI.iconPlay;
				int iconPadding = 0;
				if (isLoading)
				{
					icon = BeamGUI.GetSpinner(3);
					iconPadding = 2;
				}
				var clickedToggle = BeamGUI.HeaderButton(null, icon,
				                                         width: buttonWidth,
				                                         padding: buttonPadding,
				                                         yPadding: buttonYPadding,
				                                         iconPadding: iconPadding,
				                                         drawBorder: false,
				                                         tooltip: isRunning ? "Stop the storage" : "Start the storage",
				                                         backgroundColor: isRunning
					                                         ? usamWindow.primaryColor
					                                         : default);
				GUI.enabled = true;

				var clickedOpenApi = BeamGUI.HeaderButton(null, BeamGUI.iconOpenMongoExpress,
				                                          width: buttonWidth,
				                                          padding: buttonPadding,
				                                          yPadding: 3,
				                                          tooltip: "Go to Mongo Express",
				                                          drawBorder: false);
				var clickedOpenProject = BeamGUI.HeaderButton(null, BeamGUI.iconFolder,
				                                              width: buttonWidth,
				                                              padding: buttonPadding,
				                                              yPadding: 3,
				                                              tooltip: "Open project",
				                                              drawBorder: false);
				var clickedOptions = BeamGUI.HeaderButton(null, BeamGUI.iconMoreOptions,
				                                          width: buttonWidth,
				                                          padding: buttonPadding,
				                                          yPadding: 3,
				                                          tooltip: "More options",
				                                          drawBorder: false);
				if (clickedOptions)
				{
					usamWindow.ShowStorageMenu(storage);
				}

				if (clickedOpenProject)
				{
					usamWindow.usam.OpenProject(storage.beamoId, storage.csprojPath);
				}

				if (clickedOpenApi)
				{
					usamWindow.usam.OpenMongo(storage.beamoId);
				}

				if (clickedToggle)
				{
					usamWindow.usam.ToggleRun(storage, status);
				}
			});
		}

		bool DrawService(BeamManifestServiceEntry service, int index)
		{
			return DrawCard(service.beamoId, index, BeamGUI.iconService, () =>
			{
				var isRunning = false;
				var isLoading = false;
				if (usamWindow.usam.TryGetStatus(service.beamoId, out var status))
				{
					isRunning = usamWindow.usam.IsRunningLocally(status);
					isLoading = usamWindow.usam.IsLoadingLocally(status);
				}

				GUI.enabled = status != null;
				var icon = BeamGUI.iconPlay;
				int iconPadding = 0;
				if (isLoading)
				{
					icon = BeamGUI.GetSpinner(3);
					iconPadding = 2;
				}
				var clickedToggle = BeamGUI.HeaderButton(null, icon,
				                                         width: buttonWidth,
				                                         padding: buttonPadding,
				                                         yPadding: buttonYPadding,
				                                         iconPadding: iconPadding,
				                                         drawBorder: false,
				                                         tooltip: isRunning ? "Stop the service" : "Start the service",
				                                         backgroundColor: isRunning
					                                         ? usamWindow.primaryColor
					                                         : default);
				GUI.enabled = true;

				var clickedOpenApi = BeamGUI.HeaderButton(null, BeamGUI.iconOpenApi,
				                                          width: buttonWidth,
				                                          padding: buttonPadding,
				                                          yPadding: 3,
				                                          tooltip: "Go to Open API",
				                                          drawBorder: false);
				var clickedOpenProject = BeamGUI.HeaderButton(null, BeamGUI.iconFolder,
				                                              width: buttonWidth,
				                                              padding: buttonPadding,
				                                              yPadding: 3,
				                                              tooltip: "Open project",
				                                              drawBorder: false);
				var clickedOptions = BeamGUI.HeaderButton(null, BeamGUI.iconMoreOptions,
				                                          width: buttonWidth,
				                                          padding: buttonPadding,
				                                          yPadding: 3,
				                                          tooltip: "More options",
				                                          drawBorder: false);
				if (clickedOptions)
				{
					usamWindow.ShowServiceMenu(service);
				}

				if (clickedOpenProject)
				{
					usamWindow.usam.OpenProject(service.beamoId, service.csprojPath);
				}

				if (clickedOpenApi)
				{
					usamWindow.usam.OpenSwagger(service.beamoId, false);
				}

				if (clickedToggle)
				{
					usamWindow.usam.ToggleRun(service, status);
				}
			});
		}
		
	}
}
