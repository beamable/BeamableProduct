using System;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		private static readonly Color backgroundColor = new Color(.5f, .5f, .5f, .65f);
		
		public static void LayoutDropDown<T>(EditorWindow rootWindow, 
		                                     GUIContent current, 
		                                     GUILayoutOption widthOption, 
		                                     Func<T> windowFactory,
		                                     int yPadding=5,
		                                     int yShift=1,
		                                     Color backdropColor=default,
		                                     bool popupOnLeft=false)
			where T : EditorWindow
		{
			if (backdropColor == default)
			{
				backdropColor = backgroundColor;
			}
			const int arrowWidth = 25;

			var bounds = GUILayoutUtility.GetRect(current, new GUIStyle
			                                      {
				                                      padding = new RectOffset(5, arrowWidth + 5, 0, 0)
			                                      }, 
			                                      GUILayout.ExpandWidth(false),
			                                      GUILayout.ExpandHeight(true)
			                         );

			// const int yPadding = 5;
			// const int yShift = 1;
			var paddedRect = new Rect(bounds.x, bounds.y + yPadding + yShift, bounds.width, bounds.height - yPadding * 2);
			
			var contentRect = new Rect(paddedRect.x, paddedRect.y, paddedRect.width - arrowWidth, paddedRect.height);
			var arrowRect = new Rect(paddedRect.xMax - arrowWidth, paddedRect.y, arrowWidth, paddedRect.height);

			var isButtonHover = GUI.enabled && paddedRect.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			if (GUI.enabled)
			{
				EditorGUIUtility.AddCursorRect(paddedRect, MouseCursor.Link);
			}
			

			{ // draw hover color
				EditorGUI.DrawRect(paddedRect, backdropColor);
				if (isButtonHover)
				{
					EditorGUI.DrawRect(paddedRect, new Color(1,1,1, .05f));
				}
			}
			
			{ // draw the preview
				GUI.Label(contentRect, current, new GUIStyle(EditorStyles.label)
				{
					alignment = TextAnchor.MiddleRight,
					normal = new GUIStyleState
					{
						textColor = Color.white
					},
					hover = new GUIStyleState
					{
						textColor = Color.white
					},
					fontSize = 10
				});
			}

			{ // draw the drop down arrow
				var paddedArrowRect = new Rect(arrowRect.x + 5, arrowRect.y, 15, arrowRect.height);
				GUI.DrawTexture(paddedArrowRect, EditorGUIUtility.IconContent("Icon Dropdown").image, ScaleMode.ScaleToFit);
			}


			if (buttonClicked)
			{
				var popup = windowFactory?.Invoke();
				// var popup = ScriptableObject.CreateInstance<T>();
				const int tabHeight = 20;
				var popupWidth = 300;
				var xCoord = rootWindow.position.x + (paddedRect.xMax - popupWidth);
				if (popupOnLeft)
				{
					xCoord = rootWindow.position.x + paddedRect.xMin;
				}
				var popupPosition = new Rect(xCoord, rootWindow.position.y + tabHeight + paddedRect.yMax, 0, 0);
				
				popup.ShowAsDropDown(popupPosition, new Vector2(popupWidth, 100));
			}
		}
	}

	public interface IBeamGuiPopupWindow
	{
		void Configure();
	}
	
	public class BeamGuiPopup : EditorWindow
	{
		
		
		private void OnGUI()
		{
			EditorGUILayout.LabelField("hahah");
		}
	}
}
