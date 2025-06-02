using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Editor.UI2.ContentWindow
{
	public partial class ContentWindow
	{
		private class ItemsPanelItem
		{
			public bool IsSubtype;
			public string Name;
			public LocalContentManifestEntry? Entry;
			public List<ItemsPanelItem> SubItems;
		}

		private Vector2 _itemsPanelScrollPos;
		private string _selectedItemId;

		private Dictionary<string, List<LocalContentManifestEntry>> _itemViewHierarchy = new();
		private readonly Dictionary<string, bool> _itemsExpandedStates = new();


		private void BuildItemsHierarchy()
		{
			_itemViewHierarchy.Clear();
			var filteredItems = GetFilteredItems();
			foreach (var localContentManifestEntry in filteredItems)
			{
				if (!_itemViewHierarchy.TryGetValue(localContentManifestEntry.Name, out var entries))
				{
					entries = new List<LocalContentManifestEntry>();
					_itemViewHierarchy.Add(localContentManifestEntry.Name, entries);
				}
				entries.Add(localContentManifestEntry);
			}
		}
		
		private void DrawContentItemPanel()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
			{
				_itemsPanelScrollPos = EditorGUILayout.BeginScrollView(_itemsPanelScrollPos);
				{
					DrawItemNode();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
		}
		
		private void DrawItemNode(string parentPath = "", int indentLevel = 0)
		{
			var filteredContents = _contentTypeHierarchy;
			if (!filteredContents.TryGetValue(parentPath, out List<string> value)) return;

			foreach (string contentType in value)
			{
				bool startsWithType = _selectedContentType.StartsWith(contentType + ".");
				bool isSameType = contentType == _selectedContentType;
				if (!string.IsNullOrEmpty(_selectedContentType) && !isSameType && !startsWithType)
				{
					continue;
				}

				if (startsWithType && !isSameType)
				{
					// Subtype detected, skip parent draw
					DrawItemNode(contentType, indentLevel);
					return;
				}

				string displayName = string.IsNullOrEmpty(parentPath)
					? contentType
					: contentType.Substring(parentPath.Length + 1);

				bool hasChildrenSubtypes = filteredContents.ContainsKey(contentType) &&
				                        filteredContents[contentType].Count > 0;

				bool isSelected = _selectedItemId == contentType;
				GUIStyle rowStyle = isSelected ? new GUIStyle("TV Selection") : new GUIStyle("Label");

				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));
				rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;

				if (isSelected)
				{
					GUI.Box(rowRect, GUIContent.none, rowStyle);
				}
				
				Rect contentRect = new Rect(rowRect);
				bool isGroupExpanded = _itemsExpandedStates[contentType];
				
				if (hasChildrenSubtypes)
				{
					_itemsExpandedStates[contentType] = EditorGUI.Foldout(
						new Rect(contentRect.x, contentRect.y, 20f, contentRect.height),
						isGroupExpanded,
						GUIContent.none
					);
				}

				contentRect.xMin += CONTENT_GROUP_INDENT_WIDTH;
				GUI.Label(contentRect, displayName);

				if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
				{
					_selectedItemId = contentType;
					Event.current.Use();
					GUI.changed = true;
				}

				if (hasChildrenSubtypes && isGroupExpanded)
				{
					DrawItemNode(contentType, indentLevel + 1);
				}
			}
		}

		private List<LocalContentManifestEntry> GetFilteredItems()
		{
			var allItems = new List<LocalContentManifestEntry>(_contentService?.CachedManifest?.Entries ?? Array.Empty<LocalContentManifestEntry>());

			string nameSearchPartValue = GetNameSearchPartValue();
			var types = GetFilterTypeActiveItems(ContentFilterType.Type);
			var tags = GetFilterTypeActiveItems(ContentFilterType.Tag);
			var statuses = GetFilterTypeActiveItems(ContentFilterType.Status);
			
			return allItems.Where(entry =>
			{
				bool matchesType = types.Count == 0 || types.Any(type => entry.TypeName.Contains(type));
				bool matchesTags = tags.Count == 0 || tags.Any(tag => entry.Tags.Contains(tag));
				bool matchesStatus = statuses.Count == 0 || statuses.Any(status => entry.StatusEnum.ToString() == status);
				
				
				bool matchesName = string.IsNullOrEmpty(nameSearchPartValue) || entry.Name.Contains(nameSearchPartValue);
				bool matchesContentGroup = string.IsNullOrEmpty(_selectedContentType) || 
				                           entry.TypeName == _selectedContentType ||
				                           entry.TypeName.StartsWith(_selectedContentType + ".");
				return matchesName && matchesContentGroup && matchesType && matchesTags && matchesStatus;
			}).ToList();
		}

		private List<LocalContentManifestEntry> SortItems(List<LocalContentManifestEntry> items)
		{
			switch (_currentSortOption)
			{
				case ContentSortOptionType.IdAscending:
					return items.OrderBy(item => item.Name).ToList();
				case ContentSortOptionType.IdDescending:
					return items.OrderByDescending(item => item.Name).ToList();
				case ContentSortOptionType.TypeAscending:
					return items.OrderBy(item => item.TypeName).ThenBy(item => item.Name).ToList();
				case ContentSortOptionType.TypeDescending:
					return items.OrderByDescending(item => item.TypeName).ThenBy(item => item.Name).ToList();
				case ContentSortOptionType.Status:
					return items.OrderBy(item => item.CurrentStatus).ThenBy(item => item.Name).ToList();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
