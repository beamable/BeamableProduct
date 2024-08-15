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

		void DrawJsonBlock(object obj)
		{
			var json = JsonUtility.ToJson(obj);
			var highlighted = JsonHighlighterUtil.HighlightJson(json);
			DrawJsonBlock(highlighted);
		}
		
		void DrawJsonBlock(string formattedJson)
		{
			var style = new GUIStyle(EditorStyles.textArea);
			style.wordWrap = true;
			
			style.padding.bottom += 3;
			style.font = codeFont;
			style.richText = true;
			
			style.normal.textColor = Color.gray;
			style.hover = style.active = style.focused = style.normal;
			
			var res = GUILayoutUtility.GetRect(new GUIContent(formattedJson), style);
			var indentedRect = EditorGUI.IndentedRect(res);
			var outlineWidth = 1;
			var outline = new Rect(indentedRect.x - outlineWidth, indentedRect.y - outlineWidth,
			                       indentedRect.width + outlineWidth * 2, indentedRect.height + outlineWidth * 2);
			
			EditorGUI.SelectableLabel(res, formattedJson, style);
		}


		void DrawVirtualScroller(int elementHeight, int totalElements, ref Vector2 scrollPos, Action<int, Rect> drawCallback)
		{
			var visHeight = 300;
			var totalHeight = elementHeight * totalElements;

			// the first index that visible
			int startIndex = (int)(scrollPos.y / elementHeight);
			int rowsVisibleCount = ( int ) ( visHeight / elementHeight );
			int endIndex = startIndex + rowsVisibleCount+ 1;

			// fill in the slots above.
			
			var spaceTop = startIndex * elementHeight;
			// var spaceLow = totalHeight - (endIndex - startIndex) * elementHeight + spaceTop;
			var spaceLow = (totalElements - endIndex ) * elementHeight;
			// var spaceLow = totalHeight - (startIndex + (visHeight/(float)elementHeight)+1) * elementHeight;
			scrollPos.x = 0;
			scrollPos.y = (int)scrollPos.y;
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, 
			                                            GUIStyle.none,
			                                            options: new GUILayoutOption[]
			                                            {
				                                            GUILayout.Height(visHeight),
			                                            });
			EditorGUILayout.BeginVertical();
			
			
			GUILayoutUtility.GetRect(1, spaceTop, GUILayout.ExpandWidth(true));
			// 
			// figure out which elements to render...

			Debug.Log("RENDERING FROM :" + startIndex + " TO " + endIndex);
			for (var i = startIndex; i < endIndex; i++)
			{
				var rect = GUILayoutUtility.GetRect(null, GUIStyle.none, GUILayout.Height(elementHeight));
				if (i % 2 == 0)
				{
					EditorGUI.DrawRect(rect, new Color(0,0,0,.1f));
				}
				drawCallback(i, rect);
			}
			
			GUILayoutUtility.GetRect(1, spaceLow, GUILayout.ExpandWidth(true));
		
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
		}
		
	}
	
	
}
