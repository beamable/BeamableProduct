using Beamable.Editor.BeamCli.Commands;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public class RoutingPickerWindow : EditorWindow
	{
		public UsamWindow2 usamWindow;
		public BeamManifestServiceEntry service;

		public Vector2 scrollPosition;
		private const int elementHeight = 35;
		const int buttonWidth = 24;
		const int buttonPadding = 2;
		const int buttonYPadding = 7;
		
		private void OnGUI()
		{

			var totalElementCount = usamWindow.usam.latestManifest.services.Count +
			                        usamWindow.usam.latestManifest.storages.Count;
			minSize = new Vector2(usamWindow.position.width-100, 
			                      totalElementCount * elementHeight + 15);

			var usam = usamWindow.usam;
			
			if (!usam.TryGetRoutingSetting(service.beamoId, out var settings))
			{
				EditorGUILayout.LabelField("select a valid service.");
				return;
			}

			
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			EditorGUILayout.BeginVertical();

			
			var clicked = false;
			var totalIndex = 0;
			
			{ // render the services first, 
				for (var i = 0; i < settings.options.Count; i++, totalIndex++)
				{
					var option = settings.options[i];
					if (DrawOption(settings, option, i))
					{
						clicked = true;
						settings.selectedOption = option;
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

		bool DrawOption(ServiceRoutingSetting setting, RoutingOption option, int index)
		{
			var bounds = new Rect(0, index * elementHeight, position.width, elementHeight);
		
			EditorGUILayout.BeginHorizontal(GUILayout.Height(elementHeight), GUILayout.ExpandWidth(true));


			var clickableRect = new Rect(bounds.x, bounds.y + 4, bounds.width - buttonWidth * 4 - 20,
			                             bounds.height);
			EditorGUIUtility.AddCursorRect(clickableRect, MouseCursor.Link);

			var isButtonHover = clickableRect.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			{ // draw hover color
				if (option.routingKey == setting.selectedOption.routingKey && option.type == setting.selectedOption.type)
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
			
			var labelStyle = new GUIStyle(EditorStyles.largeLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset(8, 0, 0, 0),
			};
			var display = "local";
			switch (option.type)
			{
				case RoutingOptionType.AUTO:
					display = $"{option.display} (automatic)" ;
					break;
				case RoutingOptionType.LOCAL:
					display = "local";
					break;
				case RoutingOptionType.REMOTE:
					display = "realm";
					break;
				case RoutingOptionType.FRIEND:
					display = option.instance.startedByAccountEmail;
					break;
			}
			EditorGUILayout.LabelField(display, labelStyle, GUILayout.MaxWidth(position.width - buttonWidth*4 - elementHeight - 8), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));


			EditorGUILayout.EndHorizontal();

			return buttonClicked;
		}
	}

}
