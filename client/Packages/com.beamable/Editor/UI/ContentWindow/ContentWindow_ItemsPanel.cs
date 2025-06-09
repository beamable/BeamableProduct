using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Content.UI;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		
		private const float CONTENT_ITEMS_PANEL_WIDTH = 450;
		
		private class ItemsPanelItem
		{
			public bool IsSubtype;
			public string Name;
			public BeamLocalContentManifestEntry Entry;
			public List<ItemsPanelItem> SubItems;
		}

		private Vector2 _itemsPanelScrollPos;
		private string _selectedItemId;

		
		private readonly Dictionary<string, bool> _itemsExpandedStates = new();
		private GUIStyle _itemEvenStyle;
		private GUIStyle _itemOddStyle;
		private GUIStyle _itemSelectedStyle;


		private void BuildItemsPanelStyles()
		{
			_itemEvenStyle = new GUIStyle(EditorStyles.helpBox)
			{
				normal = { background = CreateColorTexture(new Color(0.06f, 0.06f, 0.06f)) },
			};
			
			_itemOddStyle = new GUIStyle(EditorStyles.helpBox)
			{
				normal = { background = CreateColorTexture(new Color(0.1f, 0.1f, 0.1f)) },
			};
			
			_itemSelectedStyle = new GUIStyle(EditorStyles.helpBox)
			{
				normal = { background = CreateColorTexture(new Color(0.31f, 0.31f, 0.31f)) },
			};
		}
		
		
		
		private void DrawContentItemPanel()
		{
			
			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.Width(CONTENT_ITEMS_PANEL_WIDTH));
			{
				_itemsPanelScrollPos = EditorGUILayout.BeginScrollView(_itemsPanelScrollPos);
				{
					DrawGroupNode();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
		}
		
		private void DrawGroupNode(string parentPath = "", int indentLevel = 0)
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
					DrawGroupNode(contentType, indentLevel);
					return;
				}

				string displayName = string.IsNullOrEmpty(parentPath)
					? contentType
					: contentType.Substring(parentPath.Length + 1);

				var items = GetFilteredItems(contentType);

				bool hasChildrenItems = items.Count > 0;
				
				bool hasChildrenSubtypes = filteredContents.ContainsKey(contentType) &&
				                        filteredContents[contentType].Count > 0;
				
				GUIStyle rowStyle = EditorStyles.helpBox;

				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(35));
				rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
				
				GUI.Box(rowRect, GUIContent.none, rowStyle);
				
				Rect contentRect = new Rect(rowRect);
				
				contentRect.xMin += 5;
				
				bool isGroupExpanded = _itemsExpandedStates[contentType];
				if (hasChildrenSubtypes || hasChildrenItems)
				{
					_itemsExpandedStates[contentType] = EditorGUI.Foldout(
						new Rect(contentRect.x, contentRect.y, 20f, contentRect.height),
						isGroupExpanded,
						GUIContent.none
					);
				}
				
				contentRect.xMin += 15;

				var texture = _contentConfiguration.ContentTextureConfiguration.GetTextureForType(contentType);
				var iconSize = 25f;
				GUI.DrawTexture(new Rect(contentRect.x, contentRect.center.y - iconSize/2f, iconSize, iconSize), texture, ScaleMode.ScaleToFit);
				
				contentRect.xMin += 35;
				
				GUI.Label(contentRect, displayName);

				var buttonSize = contentRect.height - 5;
				GUI.Button(new Rect(contentRect.xMax - contentRect.height - 2, contentRect.y + 2f, buttonSize, buttonSize), BeamGUI.iconPlus);
				
				
				if ((hasChildrenSubtypes || hasChildrenItems) && isGroupExpanded)
				{
					if (items.Count > 0)
					{
						DrawItemNodes(items, indentLevel + 1);
					}
					DrawGroupNode(contentType, indentLevel + 1);
				}
			}
		}

		private void DrawItemNodes(List<BeamLocalContentManifestEntry> items, int indentLevel)
		{
			for (int index = 0; index < items.Count; index++)
			{
				BeamLocalContentManifestEntry localContentManifestEntry = items[index];
				bool isSelected = _selectedItemId == localContentManifestEntry.FullId;
				GUIStyle rowStyle = isSelected ? _itemSelectedStyle : index % 2 == 0 ? _itemEvenStyle :  _itemOddStyle;
				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(35));
				rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
				GUI.Box(rowRect, GUIContent.none, rowStyle);

				Rect contentRect = new Rect(rowRect);

				contentRect.xMin += 5;
				var texture = GetIconForStatus(localContentManifestEntry.StatusEnum);
				var iconSize = 15f;
				GUI.DrawTexture(new Rect(contentRect.x, contentRect.center.y - iconSize / 2f, iconSize, iconSize),
				                texture, ScaleMode.ScaleToFit);
				contentRect.xMin += 20;
				GUI.Label(contentRect, localContentManifestEntry.Name);

				var buttonSize = contentRect.height - 5;
				GUI.Button(
					new Rect(contentRect.xMax - contentRect.height - 2, contentRect.y + 2f, buttonSize, buttonSize),
					BeamGUI.iconUpload);

				if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
				{
					_selectedItemId = localContentManifestEntry.FullId;
					Event.current.Use();
					GUI.changed = true;
				}
			}
		}

		private Texture GetIconForStatus(ContentStatus statusEnum)
		{
			throw new NotImplementedException();
		}

		private List<BeamLocalContentManifestEntry> GetFilteredItems(string specificType = "")
		{
			var allItems = new List<BeamLocalContentManifestEntry>(_contentService?.CachedManifest?.Entries ?? Array.Empty<BeamLocalContentManifestEntry>());

			string nameSearchPartValue = GetNameSearchPartValue();
			var types = GetFilterTypeActiveItems(ContentFilterType.Type);
			var tags = GetFilterTypeActiveItems(ContentFilterType.Tag);
			var statuses = GetFilterTypeActiveItems(ContentFilterType.Status);
			
			return allItems.Where(entry =>
			{
				bool matchesType = types.Count == 0 || types.Any(type => entry.TypeName.Contains(type));
				bool matchesTags = tags.Count == 0 || tags.Any(tag => entry.Tags.Contains(tag));
				bool matchesStatus = statuses.Count == 0 || statuses.Any(status => entry.StatusEnum.ToString() == status);
				bool matchesSpecificType = string.IsNullOrEmpty(specificType) || entry.TypeName == specificType;
				
				
				bool matchesName = string.IsNullOrEmpty(nameSearchPartValue) || entry.Name.Contains(nameSearchPartValue);
				bool matchesContentGroup = string.IsNullOrEmpty(_selectedContentType) || 
				                           entry.TypeName == _selectedContentType ||
				                           entry.TypeName.StartsWith(_selectedContentType + ".");
				return matchesName && matchesContentGroup && matchesType && matchesTags && matchesStatus && matchesSpecificType;
			}).ToList();
		}

		private List<BeamLocalContentManifestEntry> SortItems(List<BeamLocalContentManifestEntry> items)
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
