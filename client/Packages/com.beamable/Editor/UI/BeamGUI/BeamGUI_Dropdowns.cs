using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{
		private class PopupTime
		{
			public EditorWindow window;
			public bool noticedClose;
		}
		
		private static readonly Color backgroundColor = new Color(.5f, .5f, .5f, .65f);
		private static Dictionary<Type, PopupTime> _popupTypeToCloseTime = new Dictionary<Type, PopupTime>();


		private static void DropdownUpdate()
		{
			foreach (var kvp in _popupTypeToCloseTime)
			{
				if (kvp.Value.window == null && ! kvp.Value.noticedClose)
				{
					kvp.Value.noticedClose = true;
				}
			}
		}
		
		public static void LayoutDropDown<T>(EditorWindow rootWindow,
		                                     GUIContent current,
		                                     GUILayoutOption widthOption,
		                                     Func<T> windowFactory,
		                                     int yPadding = 5,
		                                     int yShift = 1,
		                                     Color backdropColor = default
		                                     )
			where T : EditorWindow
		{
			LayoutDropDown(rootWindow, current,widthOption,() => new DropdownMetadata<T>
			{
				window = windowFactory(),
				startSize = new Vector2Int(300, 100)
			},out _,yPadding,yShift, backdropColor);
		}

		public static void LayoutDropDown<T>(EditorWindow rootWindow,
		                                     GUIContent current,
		                                     GUILayoutOption widthOption,
		                                     Func<DropdownMetadata<T>> windowFactory,
		                                     int yPadding = 5,
		                                     int yShift = 1,
		                                     Color backdropColor = default
		)
			where T : EditorWindow
		{
			LayoutDropDown(rootWindow, current,widthOption,windowFactory,out _,yPadding,yShift, backdropColor);
		}
		
		public struct DropdownMetadata<T> where T : EditorWindow
		{
			public T window;
			public Vector2 startSize;
		}

		public static bool LayoutDropDownButton(GUIContent current, string tooltip=null)
		{
			return LayoutDropDownButton(current, out _, out _, tooltip: tooltip);
		}
		public static bool LayoutDropDownButton(GUIContent current, 
		                                  out Rect contentBounds,
		                                  out Rect paddedRect,
		                                  int yPadding=5,
		                                  int yShift=1,
		                                  Color backdropColor=default,
		                                  string tooltip=null)
		{
			if (backdropColor == default)
			{
				backdropColor = backgroundColor;
			}
			
			const int arrowWidth = 25;

			var bounds = GUILayoutUtility.GetRect(current, new GUIStyle
			                                      {
				                                      fontSize = 10,
				                                      padding = new RectOffset(5, arrowWidth + 5, 0, 0)
			                                      }, 
			                                      GUILayout.ExpandWidth(false),
			                                      GUILayout.ExpandHeight(true)
			                         );

			// const int yPadding = 5;
			// const int yShift = 1;
			paddedRect = new Rect(bounds.x, bounds.y + yPadding + yShift, bounds.width, bounds.height - yPadding * 2);
			contentBounds = paddedRect;
			var contentRect = new Rect(paddedRect.x, paddedRect.y, paddedRect.width - arrowWidth, paddedRect.height);
			var arrowRect = new Rect(paddedRect.xMax - arrowWidth, paddedRect.y, arrowWidth, paddedRect.height);

			var isButtonHover = GUI.enabled && paddedRect.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			if (GUI.enabled)
			{
				EditorGUIUtility.AddCursorRect(paddedRect, MouseCursor.Link);
			}
			
			// GUIUtility.too
			GUI.Label(contentRect, new GUIContent(null, null, tooltip), GUIStyle.none);

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


			return buttonClicked;
		}
		
		public static void LayoutDropDown<T>(EditorWindow rootWindow, 
		                                     GUIContent current, 
		                                     GUILayoutOption widthOption, 
		                                     Func<DropdownMetadata<T>> windowFactory,
		                                     out Rect contentBounds,
		                                     int yPadding=5,
		                                     int yShift=1,
		                                     Color backdropColor=default)
			where T : EditorWindow
		{

			var buttonClicked =
				LayoutDropDownButton(current, out contentBounds, out var paddedRect, yPadding, yShift, backdropColor);
			if (buttonClicked)
			{

				var shouldShow = true;
				if (_popupTypeToCloseTime.TryGetValue(typeof(T), out var existing))
				{
					if (!existing.noticedClose)
					{
						shouldShow = false;
					}
				}
				
				if (shouldShow)
				{
					
					var metadata = windowFactory?.Invoke() ?? new DropdownMetadata<T>();
					var popup = metadata.window;
					_popupTypeToCloseTime[typeof(T)] = new PopupTime
					{
						window = popup
					};
					const int tabHeight = 20;
					var popupWidth = metadata.startSize.x;
					var xCoord = rootWindow.position.x + (paddedRect.xMax - popupWidth);
					var popupPosition = new Rect(xCoord, rootWindow.position.y + tabHeight + paddedRect.yMax, 0, 0);
					popup.ShowAsDropDown(popupPosition, metadata.startSize);

				}
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
