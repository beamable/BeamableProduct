using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.BeamCli.UI
{
	struct CliWindowToolAction
	{
		public Action onClick;
		public string name;
	}

	[Serializable]
	public class CliJsonBlockData
	{
		public Vector2 scrollPosition;
		public string highlightedJson;
		public string originalJson;
	}

	public partial class BeamCliWindow
	{
		private static string[] _fonts;
		private static string[] fonts
		{
			get{
				if(_fonts == null)
				{
					_fonts = Font.GetOSInstalledFontNames();
				}
				return _fonts;
			}
		}

		private static Font _codeFont;

		private static Font codeFont
		{
			get
			{
				if (_codeFont == null)
				{
					_codeFont = Font.CreateDynamicFontFromOSFont("Courier", 12);
				}
				return _codeFont;
			}
		}

		
		
		void DrawTools(params CliWindowToolAction[] toolActions)
		{
			GUILayout.BeginHorizontal();
			{
				for (var i = 0; i < toolActions.Length; i++)
				{
					var tool = toolActions[i];
					if (GUILayout.Button(tool.name, EditorStyles.miniButton))
					{
						// delay the action so that if an exception occurs in callback, GUI events will still be closed.
						delayedActions.Add(tool.onClick);
					}
				}
			}
			GUILayout.EndHorizontal();
		}

		public static void DrawJsonBlock(object obj)
		{
			var json = JsonUtility.ToJson(obj);
			var highlighted = JsonHighlighterUtil.HighlightJson(json);
			DrawJsonBlock(highlighted);
		}
		
		public static void DrawJsonBlock(ArrayDict dict)
		{
			var highlighted = JsonHighlighterUtil.HighlightJson(dict);
			DrawJsonBlock(highlighted);
		}

		public static void DrawJsonBlock(Rect position, CliJsonBlockData data)
		{
			var style = GetCodeStyle();
			var content = new GUIContent(data.highlightedJson);
			var size = style.CalcSize(content);
			if (size.y < position.height)
			{
				size.y = position.height;
			}

			if (size.x < position.width)
			{
				size.x = position.width;
			}
			var view = new Rect(0,0, size.x, size.y);

			data.scrollPosition = GUI.BeginScrollView(position, data.scrollPosition, view);
			var copyButton = new Rect(position.xMax - 40, 10, 20, 20);
			var isButtonHover = copyButton.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			EditorGUI.SelectableLabel(view, data.highlightedJson, style);
			
			GUI.EndScrollView();
			
			// copy button...
			var tempColor = GUI.color;
			GUI.color = isButtonHover ? new Color(1,1,1,.8f) : new Color(1, 1, 1, .5f);
			GUI.Button(copyButton, EditorGUIUtility.FindTexture("Clipboard"));
			EditorGUIUtility.AddCursorRect(copyButton, MouseCursor.Link);
			GUI.color = tempColor;
			if (buttonClicked)
			{
				EditorGUIUtility.systemCopyBuffer = data.originalJson;
			}
			
		}
		
		public static void DrawJsonBlock(string formattedJson, params GUILayoutOption[] options)
		{
			Rect res = GetRectForFormattedJson(formattedJson, out var style, options);

			var indentedRect = EditorGUI.IndentedRect(res);
			var outlineWidth = 1;
			var outline = new Rect(indentedRect.x - outlineWidth, indentedRect.y - outlineWidth,
			                       indentedRect.width + outlineWidth * 2, indentedRect.height + outlineWidth * 2);

			EditorGUI.SelectableLabel(res, formattedJson, style);
		}

		public static Rect GetRectForFormattedJson(string formattedJson, out GUIStyle style, params GUILayoutOption[] options)
		{
			style = GetCodeStyle();
			return GUILayoutUtility.GetRect(new GUIContent(formattedJson), style, options);
		}
		public static Rect GetRectForFormattedJson(string formattedJson, out GUIStyle style)
		{
			return GetRectForFormattedJson(formattedJson, out style);
		}

		public static GUIStyle GetCodeStyle()
		{
			var style = new GUIStyle(EditorStyles.textArea);
			style.wordWrap = true;

			style.padding.bottom += 3;
			style.font = codeFont;
			style.richText = true;

			style.normal.textColor = Color.gray;
			style.hover = style.active = style.focused = style.normal;
			return style;
		}

		public static void DrawVirtualScroller(Rect scrollRect,
		                         int elementHeight,
		                         int totalElements,
		                         ref Vector2 scrollPos,
		                         Action<int, Rect> drawCallback)
		{
			DrawVirtualScroller(scrollRect, elementHeight, totalElements, ref scrollPos, (index, rect) =>
			{
				drawCallback(index, rect);
				return true;
			});
		}
		
		public static void DrawVirtualScroller(Rect scrollRect, 
		                         int elementHeight,
		                         int totalElements,
		                         ref Vector2 scrollPos,
		                         Func<int, Rect, bool> drawCallback)
		{
			var totalHeight = elementHeight * totalElements;
			var visHeight = (int)scrollRect.height;
			int startIndex = (int)(scrollPos.y / elementHeight);
			if (startIndex < 0) startIndex = 0;
			
			int rowsVisibleCount =( visHeight / elementHeight );
			int endIndex = startIndex + rowsVisibleCount+1;
			if (endIndex >= totalElements) endIndex = totalElements;
			
			scrollPos.x = 0;
			// var scrollRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(visHeight));
			var viewRect = new Rect(0, 0, scrollRect.width - 15, totalHeight);

			scrollPos = GUI.BeginScrollView(scrollRect, scrollPos, viewRect);
			
			if (scrollPos.y > totalHeight - visHeight)
			{
				scrollPos.y = totalHeight - visHeight;
			}
			// Debug.Log("RENDERING FROM :" + startIndex + " TO " + endIndex);

			var offset = 0;
			var shadedIndex = startIndex;
			var drawAttempts = 0;
			for (var i = startIndex; i < endIndex; i++)
			{
				var index = i + offset;
				if (index >= totalElements) break;
				
				var rect = new Rect(0, i * elementHeight, viewRect.width, elementHeight);
				if (i % 2 == 0 && i >= shadedIndex)
				{
					shadedIndex++;
					EditorGUI.DrawRect(rect, new Color(0,0,0,.1f));
				}

				var drawn = drawCallback(index, rect);
				if (!drawn)
				{
					drawAttempts++;

					// if (drawAttempts > 50)
					// {
					// 	drawAttempts = 0;
					// 	// skip cell
					// 	continue;
					// }
					//
					offset++;
					i--;
					
					// nothing was selected, so draw the next thing...
				}
				else
				{
					drawAttempts = 0;
				}
			}
			GUI.EndScrollView();
		}
		
		public static void DrawVirtualScroller(int elementHeight, int totalElements, ref Vector2 scrollPos, Action<int, Rect> drawCallback, int visHeight = 300)
		{
			var scrollRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(visHeight));
			DrawVirtualScroller(scrollRect, elementHeight, totalElements, ref scrollPos, drawCallback);
		}

		public static void DrawScrollableSelectableTextBox(string text, ref Vector2 scrollPos, int minHeight, GUIStyle baseStyle=null)
		{

			var content = new GUIContent(text);
			baseStyle ??= EditorStyles.label;
			var style = new GUIStyle(baseStyle)
			{
				wordWrap = true, padding = new RectOffset(2, 2, 2, 2), alignment = TextAnchor.UpperLeft
			};
			var size = style.CalcSize(content);
			
			var fullRect = GUILayoutUtility.GetRect(content, style,
			                                        options: new GUILayoutOption[]
			                                        {
				                                        GUILayout.ExpandWidth(true), GUILayout.MinHeight(minHeight),
				                                        GUILayout.MaxHeight(minHeight),
			                                        });
			if (size.y < fullRect.height)
			{
				size.y = fullRect.height;
			}

			size.x = fullRect.width - 18;


			var view = new Rect(0, 0, size.x, size.y);

			var copyButton = new Rect(fullRect.xMax - 40, fullRect.y + 10, 20, 20);
			var isButtonHover = copyButton.Contains(Event.current.mousePosition);
			var buttonClicked = isButtonHover && Event.current.rawType == EventType.MouseDown;

			scrollPos = GUI.BeginScrollView(fullRect, scrollPos, view);


			EditorGUI.SelectableLabel(view, text, style);
			GUI.EndScrollView();

			// copy button...
			var tempColor = GUI.color;
			GUI.color = isButtonHover ? new Color(1, 1, 1, .8f) : new Color(1, 1, 1, .5f);
			GUI.Button(copyButton, EditorGUIUtility.FindTexture("Clipboard"));
			EditorGUIUtility.AddCursorRect(copyButton, MouseCursor.Link);
			GUI.color = tempColor;
			if (buttonClicked)
			{
				Debug.Log("Copied! " + text);
				EditorGUIUtility.systemCopyBuffer = text;
			}
		}


	}
	
	
}
