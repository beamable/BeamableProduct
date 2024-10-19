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
		public UsamWindow2 usamWindow;
		public Vector2 scrollPosition;

		const int elementHeight = 35;
		const int buttonWidth = 24;
		const int buttonPadding = 2;
		const int buttonYPadding = 3;

		
		private void OnInspectorUpdate()
		{
			Repaint();
		}

		private void OnGUI()
		{
			var totalElementCount = usamWindow.usam.latestManifest.services.Count +
			                        usamWindow.usam.latestManifest.storages.Count;
			minSize = new Vector2(usamWindow.position.width-100, 
			                      totalElementCount * elementHeight + 15);

			var usam = usamWindow.usam;
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

			var clickableRect = new Rect(bounds.x, bounds.y, bounds.width - buttonWidth * 4 - 20,
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
			return DrawCard(storage.beamoId, index, UsamWindow2.iconStorage, () =>
			{
				var isRunning = false;
				if (usamWindow.usam.TryGetStatus(storage.beamoId, out var status))
				{
					isRunning = usamWindow.usam.IsRunningLocally(status);
				}

				GUI.enabled = status != null;
				var clickedToggle = BeamGUI.HeaderButton(null, UsamWindow2.iconPlay,
				                                         width: buttonWidth,
				                                         padding: buttonPadding,
				                                         yPadding: buttonYPadding,
				                                         drawBorder: false,
				                                         tooltip: isRunning ? "Stop the storage" : "Start the storage",
				                                         backgroundColor: isRunning
					                                         ? usamWindow.primaryColor
					                                         : default);
				GUI.enabled = true;

				var clickedOpenApi = BeamGUI.HeaderButton(null, UsamWindow2.iconOpenApi,
				                                          width: buttonWidth,
				                                          padding: buttonPadding,
				                                          yPadding: 3,
				                                          tooltip: "Go to Mongo Express",
				                                          drawBorder: false);
				var clickedOpenProject = BeamGUI.HeaderButton(null, UsamWindow2.iconOpenProject,
				                                              width: buttonWidth,
				                                              padding: buttonPadding,
				                                              yPadding: 3,
				                                              tooltip: "Open project",
				                                              drawBorder: false);
				var clickedOptions = BeamGUI.HeaderButton(null, UsamWindow2.iconMoreOptions,
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
			return DrawCard(service.beamoId, index, UsamWindow2.iconService, () =>
			{
				var isRunning = false;
				if (usamWindow.usam.TryGetStatus(service.beamoId, out var status))
				{
					isRunning = usamWindow.usam.IsRunningLocally(status);
				}

				GUI.enabled = status != null;
				var clickedToggle = BeamGUI.HeaderButton(null, UsamWindow2.iconPlay,
				                                         width: buttonWidth,
				                                         padding: buttonPadding,
				                                         yPadding: buttonYPadding,
				                                         drawBorder: false,
				                                         tooltip: isRunning ? "Stop the service" : "Start the service",
				                                         backgroundColor: isRunning
					                                         ? usamWindow.primaryColor
					                                         : default);
				GUI.enabled = true;

				var clickedOpenApi = BeamGUI.HeaderButton(null, UsamWindow2.iconOpenApi,
				                                          width: buttonWidth,
				                                          padding: buttonPadding,
				                                          yPadding: 3,
				                                          tooltip: "Go to Open API",
				                                          drawBorder: false);
				var clickedOpenProject = BeamGUI.HeaderButton(null, UsamWindow2.iconOpenProject,
				                                              width: buttonWidth,
				                                              padding: buttonPadding,
				                                              yPadding: 3,
				                                              tooltip: "Open project",
				                                              drawBorder: false);
				var clickedOptions = BeamGUI.HeaderButton(null, UsamWindow2.iconMoreOptions,
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
		
		// bool DrawService2(BeamManifestServiceEntry service, int index)
		// {
		// 	
		// 	var bounds = new Rect(0, index * elementHeight, position.width, elementHeight);
		//
		// 	EditorGUILayout.BeginHorizontal(GUILayout.Height(elementHeight), GUILayout.ExpandWidth(true));
		//
		// 	var clickableRect = new Rect(bounds.x, bounds.y, bounds.width - buttonWidth * 4 - 20,
		// 	                             bounds.height);
		// 	EditorGUIUtility.AddCursorRect(clickableRect, MouseCursor.Link);
		//
		// 	var isButtonHover = clickableRect.Contains(Event.current.mousePosition);
		// 	var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;
		//
		// 	{ // draw hover color
		// 		if (service.beamoId == usamWindow.selectedBeamoId)
		// 		{
		// 			var selectionRect = new Rect(bounds.x, bounds.y, 4, bounds.height);
		// 			EditorGUI.DrawRect(selectionRect, new Color(.25f, .5f, 1f, .8f));
		// 		}
		// 		
		// 		{
		// 			EditorGUI.DrawRect(bounds, new Color(0, 0, 0, index%2 == 0 ? .1f : .2f));
		// 		}
		// 		
		// 		if (isButtonHover)
		// 		{
		// 			EditorGUI.DrawRect(bounds, new Color(1,1,1, .05f));
		// 		}
		// 	}
		// 	
		// 	
		// 	// space for icon
		// 	var iconRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Width(elementHeight), GUILayout.Height(elementHeight));
		// 	var paddedIconRect = new Rect(iconRect.x + 16, iconRect.y + 8, iconRect.width - 16, iconRect.height - 16);
		// 	GUI.DrawTexture(paddedIconRect, UsamWindow2.iconService);
		//
		// 	var labelStyle = new GUIStyle(EditorStyles.largeLabel)
		// 	{
		// 		alignment = TextAnchor.MiddleLeft,
		// 		padding = new RectOffset(8, 0, 0, 0),
		// 	};
		// 	EditorGUILayout.LabelField(service.beamoId, labelStyle, GUILayout.MaxWidth(position.width - buttonWidth*4 - elementHeight - 8), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
		//
		//
		// 	var isRunning = false;
		// 	if (usamWindow.usam.TryGetStatus(service.beamoId, out var status))
		// 	{
		// 		isRunning = usamWindow.usam.IsRunningLocally(status);
		// 	}
		//
		// 	GUI.enabled = status != null;
		// 	var clickedToggle = BeamGUI.HeaderButton(null, UsamWindow2.iconPlay,
		// 	                                         width: buttonWidth,
		// 	                                         padding: buttonPadding,
		// 	                                         yPadding: buttonYPadding,
		// 	                                         drawBorder: false,
		// 	                                         tooltip: isRunning ? "Stop the service" : "Start the service",
		// 	                                         backgroundColor: isRunning ? usamWindow.primaryColor : default);
		// 	GUI.enabled = true;
		// 	
		// 	var clickedOpenApi = BeamGUI.HeaderButton(null, UsamWindow2.iconOpenApi,
		// 	                                          width: buttonWidth,
		// 	                                          padding: buttonPadding,
		// 	                                          yPadding: 3,
		// 	                                          tooltip: "Go to Open API",
		// 	                                          drawBorder: false);
		// 	var clickedOpenProject = BeamGUI.HeaderButton(null, UsamWindow2.iconOpenProject, 
		// 	                                              width: buttonWidth, 
		// 	                                              padding: buttonPadding, 
		// 	                                              yPadding: 3,
		// 	                                              tooltip: "Open project",
		// 	                                              drawBorder: false);
		// 	var clickedOptions = BeamGUI.HeaderButton(null, UsamWindow2.iconMoreOptions, 
		// 	                                          width: buttonWidth, 
		// 	                                          padding: buttonPadding, 
		// 	                                          yPadding: 3,
		// 	                                          tooltip: "More options",
		// 	                                          drawBorder: false);
		// 	if (clickedOptions)
		// 	{
		// 		usamWindow.ShowServiceMenu(service);
		// 	}
		//
		// 	if (clickedOpenProject)
		// 	{
		// 		usamWindow.usam.OpenProject(service.beamoId, service.csprojPath);
		// 	}
		//
		// 	if (clickedOpenApi)
		// 	{
		// 		usamWindow.usam.OpenSwagger(service.beamoId, false);
		// 	}
		//
		// 	if (clickedToggle)
		// 	{
		// 		usamWindow.usam.ToggleRun(service, status);
		// 	}
		// 	
		// 	EditorGUILayout.EndHorizontal();
		//
		// 	return buttonClicked;
		// }
	}
}
