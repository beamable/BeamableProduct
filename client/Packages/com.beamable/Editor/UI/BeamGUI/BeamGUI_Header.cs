using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{

		public static void DrawHeaderSection(EditorWindow window,
		                                     BeamEditorContext context,
		                                     Action drawTopBarGui,
		                                     Action drawLowBarGui,
		                                     Action onClickedHelp,
		                                     Action onClickedRefresh)
		{
			DrawHeaderSection(window, context, drawTopBarGui, _ => drawLowBarGui(), onClickedHelp, onClickedRefresh);
		}

		public static void DrawHeaderSection(
			EditorWindow window, 
			BeamEditorContext context, 
			Action drawTopBarGui,
			Action<Rect> drawLowBarGui,
			Action onClickedHelp,
			Action onClickedRefresh
			)
		{
			{ // hopefully a no-op, but we should make sure the icons are all ready to rock
				LoadAllIcons();
			}

			var clickedRefresh = false;
			var clickedHelp = false;
			
			{ // draw button strip
				EditorGUILayout.BeginHorizontal(new GUIStyle(), GUILayout.ExpandWidth(true), GUILayout.MinHeight(35));

				drawTopBarGui();
				
				EditorGUILayout.Space(1, true);

				{ // draw the right buttons
					clickedRefresh = BeamGUI.HeaderButton(null, iconRefresh,
					                                      width: 30,
					                                      padding: 4,
					                                      iconPadding: 3,
					                                      drawBorder: false);

					clickedHelp = BeamGUI.HeaderButton(null, iconHelp,
					                                   width: 30,
					                                   padding: 4,
					                                   iconPadding: 1,
					                                   drawBorder: false);
				}

				EditorGUILayout.Space(12, false);


				EditorGUILayout.EndHorizontal();
			}

			{ 
				var rect = new Rect(0, GUILayoutUtility.GetLastRect().yMax, window.position.width, 30);
				EditorGUILayout.BeginHorizontal(new GUIStyle(), GUILayout.ExpandWidth(true),
				                                GUILayout.Height(30));
				EditorGUI.DrawRect(rect, new Color(0, 0, 0, .6f));

				EditorGUILayout.Space(1, true);
				drawLowBarGui.Invoke(rect);
				
				EditorGUILayout.EndHorizontal();
			}

			if (clickedRefresh)
			{
				onClickedRefresh?.Invoke();
			}

			if (clickedHelp)
			{
				onClickedHelp?.Invoke();
			}
		}
		
		public static bool HeaderButton(string label, Texture icon, int width=80, 
		                                string tooltip="", 
		                                int padding=0, 
		                                int yPadding=0,
		                                int xOffset=0,
		                                int iconPadding=0,
		                                Color backgroundColor=default,
		                                bool drawBorder=true,
		                                Rect? forcedRect = null)
		{
			var isDisabled = !GUI.enabled;
			Color startColor = GUI.color;
			if (isDisabled)
			{
				GUI.color = Color.Lerp(startColor, Color.clear, .3f);
			}
			
			var rect = forcedRect ?? GUILayoutUtility.GetRect(GUIContent.none, new GUIStyle(),  GUILayout.Width(width), GUILayout.ExpandHeight(true));
			rect = new Rect(rect.x + padding, rect.y + padding + yPadding, rect.width - padding * 2, rect.height - padding * 2 - yPadding * 2);

			rect = new Rect(rect.x - xOffset, rect.y, rect.width, rect.height);
			
			var isButtonHover = rect.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			{ // draw hover color
				EditorGUI.DrawRect(rect, backgroundColor);
				if (isButtonHover)
				{
					EditorGUI.DrawRect(rect, new Color(1,1,1, .05f));
				}
			}
			GUI.Label(rect, new GUIContent(null, null, tooltip), GUIStyle.none);
			
			{ // draw the icon
				var lowerPadding = label == null ? 2 : 15;
				var texRect = new Rect(rect.x+iconPadding, rect.y+2+iconPadding, rect.width-iconPadding*2, rect.height - lowerPadding -iconPadding*2);
				GUI.DrawTexture(texRect, icon, ScaleMode.ScaleToFit);
			}

			if (!string.IsNullOrEmpty(label)) // draw the label
			{
				var labelRect = new Rect(rect.x, rect.yMax - 15, rect.width, 15);
				GUI.Label(labelRect, new GUIContent(label),
				          new GUIStyle(EditorStyles.miniLabel) {alignment = TextAnchor.MiddleCenter});
			}

			if (drawBorder)
			{ // draw right-border (only right, because these feed out from the left)
				var borderRect = new Rect(rect.xMax - 1, rect.y, 1, rect.height);
				EditorGUI.DrawRect(borderRect, new Color(0, 0, 0, .3f));
			}

			if (!isDisabled)
			{
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
			}

			GUI.color = startColor;
			return !isDisabled && buttonClicked;
		}
	}
}
