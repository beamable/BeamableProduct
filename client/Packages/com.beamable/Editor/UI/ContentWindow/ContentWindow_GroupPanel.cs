using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.UI2.ContentWindow
{
	public partial class ContentWindow
	{
		private const float CONTENT_GROUP_PANEL_WIDTH = 250;
		private const float CONTENT_GROUP_INDENT_WIDTH = 15f;
		private Vector2 _contentGroupScrollPos;
		private string _selectedContentType = string.Empty;
		
		private readonly Dictionary<string, List<string>> _contentTypeHierarchy = new();
		private readonly Dictionary<string, bool> _groupExpandedStates = new();
		private GUIStyle _headerStyle;

		private void BuildContentTypeHierarchy()
		{
			_contentTypeHierarchy.Clear();

			var allContentTypes = _contentTypeReflectionCache.GetAll()
			                                                 .OrderBy(item => item.Name)
			                                                 .Select(item => item.Name);

			foreach (string contentType in allContentTypes)
			{
				string[] parts = contentType.Split('.');
				string currentPath = "";
            
				for (int i = 0; i < parts.Length; i++)
				{
					string parentPath = currentPath;
					currentPath += (i > 0 ? "." : "") + parts[i];
                
					if (!_contentTypeHierarchy.ContainsKey(parentPath))
					{
						_contentTypeHierarchy[parentPath] = new List<string>();
					}
                
					if (!_contentTypeHierarchy[parentPath].Contains(currentPath))
					{
						_contentTypeHierarchy[parentPath].Add(currentPath);
					}
					
					_groupExpandedStates.TryAdd(currentPath, false);
					_itemsExpandedStates.TryAdd(currentPath, false);
				}
			}
		}

		private void BuildContentStyles()
		{
			_headerStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 16, 
				alignment = TextAnchor.MiddleCenter, 
				margin = new RectOffset(0, 0, 10, 20)
			};
		}

		private void DrawContentGroupPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(CONTENT_GROUP_PANEL_WIDTH));
			{
				EditorGUILayout.LabelField("Content Type", _headerStyle);

				if (GUILayout.Button("Show All"))
				{
					_selectedContentType = "";
				}

				EditorGUILayout.Separator();

				_contentGroupScrollPos = EditorGUILayout.BeginScrollView(_contentGroupScrollPos);
				{
					DrawContentTypeNode();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
			
		}

		private void DrawContentTypeNode(string parentPath = "", int indentLevel = 0)
		{
			if (!_contentTypeHierarchy.TryGetValue(parentPath, out List<string> value)) return;

			foreach (string contentType in value)
			{
				string displayName = string.IsNullOrEmpty(parentPath)
					? contentType
					: contentType.Substring(parentPath.Length + 1);

				bool hasChildrenNodes = _contentTypeHierarchy.ContainsKey(contentType) &&
				                        _contentTypeHierarchy[contentType].Count > 0;

				bool isSelected = _selectedContentType == contentType;
				GUIStyle rowStyle = isSelected ? new GUIStyle("TV Selection") : new GUIStyle("Label");

				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));
				rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;

				if (isSelected)
				{
					GUI.Box(rowRect, GUIContent.none, rowStyle);
				}
				
				Rect contentRect = new Rect(rowRect);
				bool isGroupExpanded = _groupExpandedStates[contentType];
				
				if (hasChildrenNodes)
				{
					_groupExpandedStates[contentType] = EditorGUI.Foldout(
						new Rect(contentRect.x, contentRect.y, 20f, contentRect.height),
						isGroupExpanded,
						GUIContent.none
					);
				}

				contentRect.xMin += CONTENT_GROUP_INDENT_WIDTH;
				GUI.Label(contentRect, displayName);

				if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
				{
					_selectedContentType = contentType;
					Event.current.Use();
					GUI.changed = true;
				}

				if (hasChildrenNodes && isGroupExpanded)
				{
					DrawContentTypeNode(contentType, indentLevel + 1);
				}
			}
		}

	}
}
