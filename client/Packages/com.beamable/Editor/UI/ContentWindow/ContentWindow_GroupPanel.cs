using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const float CONTENT_GROUP_PANEL_WIDTH = 250;
		private Vector2 _contentGroupScrollPos;

		private HashSet<string> SelectedContentType
		{
			get
			{
				if (_activeFilters.TryGetValue(ContentSearchFilterType.Type, out var value))
				{
					return value;
				}

				value = new HashSet<string>();
				_activeFilters.Add(ContentSearchFilterType.Type, value);
				return value;
			}
			set
			{
				_activeFilters[ContentSearchFilterType.Type] = value;
				UpdateActiveFilterSearchText();
			}
		}

		private readonly Dictionary<string, List<string>> _contentTypeHierarchy = new();
		private readonly Dictionary<string, bool> _groupExpandedStates = new();

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
					
					_groupExpandedStates.TryAdd(currentPath, true);
					_itemsExpandedStates.TryAdd(currentPath, true);
				}
			}
		}

		private void DrawContentGroupPanel()
		{
			EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.Space(5);
				bool isSelected = SelectedContentType.Count == 0;
				
				GUIStyle rowStyle = isSelected ? EditorStyles.selectionRect : EditorStyles.label;

				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));
				
				if (isSelected)
				{
					GUI.Box(rowRect, GUIContent.none, rowStyle ?? EditorStyles.label);
				}
				
				Rect contentRect = new Rect(rowRect);
				
				contentRect.xMin += INDENT_WIDTH;
				GUI.Label(contentRect, "Show all");

				if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
				{
					SelectedContentType.Clear();
					UpdateActiveFilterSearchText();
					Event.current.Use();
					GUI.changed = true;
				}
				

				_contentGroupScrollPos = EditorGUILayout.BeginScrollView(_contentGroupScrollPos);
				{
					DrawContentTypeNode(indentLevel: 1);
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

				bool isSelected = SelectedContentType.Contains(contentType);
				GUIStyle rowStyle = isSelected ? EditorStyles.selectionRect : EditorStyles.label;

				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));
				rowRect.xMin += indentLevel * INDENT_WIDTH;


				if (isSelected)
				{
					// GUI.Box(rowRect, GUIContent.none, EditorStyles.selectionRect);

					GUI.Box(rowRect, GUIContent.none, rowStyle ?? EditorStyles.label);
				}
				
				Rect contentRect = new Rect(rowRect);
				bool isGroupExpanded = _groupExpandedStates[contentType];
				
				if (hasChildrenNodes)
				{
					_groupExpandedStates[contentType] = EditorGUI.Foldout(
						new Rect(contentRect.x, contentRect.y, FOLDOUT_WIDTH, contentRect.height),
						isGroupExpanded,
						GUIContent.none
					);
				}

				contentRect.xMin += INDENT_WIDTH;
				
				Texture texture = _contentConfiguration.ContentTextureConfiguration.GetTextureForType(contentType);
				float iconSize = 15f;
				var iconRect = new Rect(contentRect.x, contentRect.center.y - iconSize/2f, iconSize, iconSize);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
				
				contentRect.xMin += iconSize + BASE_PADDING;
				
				GUI.Label(contentRect, displayName);

				if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
				{
					if (Event.current.button == 0)
					{
						if (!Event.current.control)
						{
							SelectedContentType.Clear();
						}
					
						if (!SelectedContentType.Add(contentType))
						{
							SelectedContentType.Remove(contentType);
						}
					
						UpdateActiveFilterSearchText();
						Event.current.Use();
						GUI.changed = true;
					}
					else if (Event.current.button == 1)
					{
						ShowTypeMenu(contentType);
					}
				}

				if (hasChildrenNodes && isGroupExpanded)
				{
					DrawContentTypeNode(contentType, indentLevel + 1);
				}
			}
		}

	}
}
