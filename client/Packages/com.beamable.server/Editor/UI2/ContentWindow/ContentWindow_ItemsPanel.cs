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
		private const int ITEM_PANEL_SCROLL_BAR_WIDTH = 50;
		
		private Vector2 _itemsPanelScrollPos;
		private string _selectedItemId;
		
		private GUIStyle _itemPanelHeaderRowStyle;
		private GUIStyle _rowEvenItemStyle;
		private GUIStyle _rowOddItemStyle;
		private GUIStyle _rowSelectedItemStyle;
		private GUIStyle _itemPanelHeaderFieldStyle;
		private GUIStyle _itemLabelStyle;

		private static float CurrentViewWidth =>
			EditorGUIUtility.currentViewWidth - MARGIN_SEPARATOR_WIDTH * 2 -
			CONTENT_GROUP_PANEL_WIDTH - ITEM_PANEL_SCROLL_BAR_WIDTH;

		private void BuildItemsPanelStyles()
		{
			_itemPanelHeaderRowStyle = new GUIStyle(EditorStyles.toolbar)
			{
				normal = { background = CreateColorTexture(new Color(0.28f, 0.28f, 0.28f)) },
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				fixedHeight = EditorGUIUtility.singleLineHeight * 2f
			};

			_itemPanelHeaderFieldStyle = new GUIStyle(EditorStyles.miniBoldLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 16,
				fixedHeight = EditorGUIUtility.singleLineHeight * 2f
			};
			
			_itemLabelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft, padding = new RectOffset(5, 5, 0, 0)
			};
			
			
			float itemRowHeight = EditorGUIUtility.singleLineHeight * 1.15f;
			
			_rowEvenItemStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f)) },
				margin = new RectOffset(0, 0, 0, 0),
				fixedHeight = itemRowHeight,
				alignment = TextAnchor.MiddleLeft
			};
			
			_rowOddItemStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { background = CreateColorTexture(new Color(0.22f, 0.22f, 0.22f)) },
				margin = new RectOffset(0, 0, 0, 0),
				fixedHeight = itemRowHeight,
				alignment = TextAnchor.MiddleLeft
			};
			
			_rowSelectedItemStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { background = CreateColorTexture(new Color(0.21f, 0.32f, 0.49f)) },
				margin = new RectOffset(0, 0, 0, 0),
				fixedHeight = itemRowHeight,
				alignment = TextAnchor.MiddleLeft
			};
		}
		
		private Texture2D CreateColorTexture(Color color)
		{
			var texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, color);
			texture.Apply();
			return texture;
		}
		
		private void DrawContentItemPanel()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
			{
				_itemsPanelScrollPos = EditorGUILayout.BeginScrollView(_itemsPanelScrollPos);
				{
					DrawItemsPanelHeader();
					DrawItems();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
		}
		
		private void DrawItemsPanelHeader()
		{
			float[] columnWidths = CalculateColumnWidths(CurrentViewWidth);

			EditorGUILayout.BeginHorizontal(_itemPanelHeaderRowStyle ?? EditorStyles.toolbar, GUILayout.Height(20));
			{
				EditorGUILayout.LabelField("Content ID", _itemPanelHeaderFieldStyle, GUILayout.Width(columnWidths[0]));
				DrawVerticalLineSeparator();
				EditorGUILayout.LabelField("Content Type", _itemPanelHeaderFieldStyle, GUILayout.Width(columnWidths[1]));
				DrawVerticalLineSeparator();
				EditorGUILayout.LabelField("Tags", _itemPanelHeaderFieldStyle, GUILayout.Width(columnWidths[2]));
				DrawVerticalLineSeparator();
				EditorGUILayout.LabelField("Manifest UID", _itemPanelHeaderFieldStyle, GUILayout.Width(columnWidths[3]));
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawItems()
		{
			var filteredItems = GetFilteredItems();
			filteredItems = SortItems(filteredItems);

			if (filteredItems.Count == 0)
			{
				EditorGUILayout.HelpBox("No items found for this filter.", MessageType.Info);
			}
			else
			{
				for (int index = 0; index < filteredItems.Count; index++)
				{
					DrawItemRow(filteredItems[index], index);
				}

				DrawHorizontalLineSeparator();
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

		private void DrawItemRow(LocalContentManifestEntry item, int index)
		{
			
			float[] columnWidths = CalculateColumnWidths(CurrentViewWidth);

			GUIStyle style = _selectedItemId == item.FullId ? _rowSelectedItemStyle :
				index % 2 == 0 ? _rowEvenItemStyle : _rowOddItemStyle;
			
			
			EditorGUILayout.BeginHorizontal(style ?? EditorStyles.toolbar, GUILayout.Width(20));
			{
				EditorGUILayout.LabelField(item.Name, _itemLabelStyle, GUILayout.Width(columnWidths[0]));
				DrawVerticalLineSeparator();
				EditorGUILayout.LabelField(item.TypeName, _itemLabelStyle, GUILayout.Width(columnWidths[1]));
				DrawVerticalLineSeparator();
				EditorGUILayout.LabelField(item.Tags != null ? string.Join(", ", item.Tags) : "-",
				                           _itemLabelStyle, GUILayout.Width(columnWidths[2]));
				DrawVerticalLineSeparator();
				EditorGUILayout.LabelField(item.ReferenceManifestUid ?? "None",
				                           _itemLabelStyle, GUILayout.Width(columnWidths[3]));
			}
			EditorGUILayout.EndHorizontal();

			Rect rowRect = GUILayoutUtility.GetLastRect();
			if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
			{
				_selectedItemId = item.FullId;
				Event.current.Use();
				Repaint();
			}
		}

		private static float[] CalculateColumnWidths(float totalAvailableWidth)
		{
			// ID, Type, Tags, Manifest UID
			float[] minWidths = {120f, 120f, 50f, 120f}; 
			
			float[] weights = {1.5f, 1.25f, 1f, 1f};

			// MinWidth sum + Vertical Line Separator Count
			float totalMinWidth = minWidths.Sum() + (minWidths.Length - 1);
			
			float remainingWidth = Mathf.Max(0, totalAvailableWidth - totalMinWidth);
			
			float totalWeight = weights.Sum();
			
			float[] widths = new float[minWidths.Length];
			for (int i = 0; i < widths.Length; i++)
			{
				widths[i] = minWidths[i] + (remainingWidth * (weights[i] / totalWeight));
			}

			return widths;
		}
	}
}
