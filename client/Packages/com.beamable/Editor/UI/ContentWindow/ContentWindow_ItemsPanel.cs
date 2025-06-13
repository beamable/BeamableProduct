using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private static readonly float[] MinWidths = new[] {120f, 100f, 100f};
		private const float BASE_PADDING = 5f;
		private const float SEPARATOR_WIDTH_AREA = 3;
		private const float ITEMS_TABLE_ROW_HEIGHT = 20;

		private Vector2 _itemsPanelScrollPos;
		private string _selectedItemId;

		
		private readonly Dictionary<string, bool> _itemsExpandedStates = new();
		private GUIStyle _itemPanelHeaderRowStyle;
		private GUIStyle _itemPanelHeaderFieldStyle;
		private GUIStyle _itemLabelStyle;
		private GUIStyle _rowEvenItemStyle;
		private GUIStyle _rowOddItemStyle;
		private GUIStyle _rowSelectedItemStyle;

		private void BuildItemsPanelStyles()
		{
			_itemPanelHeaderRowStyle = new GUIStyle(EditorStyles.toolbar)
			{
				normal = { background = BeamGUI.CreateColorTexture(new Color(0.28f, 0.28f, 0.28f)) },
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft,
				fixedHeight = EditorGUIUtility.singleLineHeight * 1.15f
			};

			_itemPanelHeaderFieldStyle = new GUIStyle(EditorStyles.miniBoldLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				fontSize = 12,
				fixedHeight = EditorGUIUtility.singleLineHeight * 1.15f
			};
			
			_itemLabelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft, padding = new RectOffset(5, 5, 0, 0)
			};
			
			
			float itemRowHeight = EditorGUIUtility.singleLineHeight * 1.1f;
			
			_rowEvenItemStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { background = BeamGUI.CreateColorTexture(new Color(0.2f, 0.2f, 0.2f)) },
				margin = new RectOffset(0, 0, 0, 0),
				fixedHeight = itemRowHeight,
				alignment = TextAnchor.MiddleLeft
			};
			
			_rowOddItemStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { background = BeamGUI.CreateColorTexture(new Color(0.22f, 0.22f, 0.22f)) },
				margin = new RectOffset(0, 0, 0, 0),
				fixedHeight = itemRowHeight,
				alignment = TextAnchor.MiddleLeft
			};
			
			_rowSelectedItemStyle = new GUIStyle(EditorStyles.label)
			{
				normal = { background = BeamGUI.CreateColorTexture(new Color(0.21f, 0.32f, 0.49f)) },
				margin = new RectOffset(0, 0, 0, 0),
				fixedHeight = itemRowHeight,
				alignment = TextAnchor.MiddleLeft
			};
		}
		
		
		
		private void DrawContentItemPanel()
		{
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
			{
				_itemsPanelScrollPos = GUILayout.BeginScrollView(_itemsPanelScrollPos);
				{
					DrawGroupNode();
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();
		}
		
		private void DrawGroupNode(string parentPath = "", int indentLevel = 0)
		{
			var filteredContents = _contentTypeHierarchy;
			if (!filteredContents.TryGetValue(parentPath, out List<string> value)) return;

			foreach (string contentType in value)
			{
				bool isSelectedSubtypeOfContent = SelectedContentType.Any(item => item.StartsWith(contentType + "."));
				bool isSubtypeFromSelected = SelectedContentType.Any(item => contentType.StartsWith(item + "."));
				bool isTypeSelected = SelectedContentType.Contains(contentType);
				if (SelectedContentType.Count > 0 && !isTypeSelected && !isSubtypeFromSelected && !isSelectedSubtypeOfContent)
				{
					continue;
				}

				if (isSelectedSubtypeOfContent && !isTypeSelected)
				{
					// Only Subtype detected, skip parent draw
					DrawGroupNode(contentType, indentLevel);
					continue;
				}

				string displayName = string.IsNullOrEmpty(parentPath)
					? contentType
					: contentType.Substring(parentPath.Length + 1);

				var items = GetFilteredItems(contentType);

				bool hasChildrenItems = items.Count > 0;
				
				bool hasChildrenSubtypes = filteredContents.ContainsKey(contentType) &&
				                        filteredContents[contentType].Count > 0;
				GUILayout.Space(BASE_PADDING);
				GUIStyle rowStyle = EditorStyles.helpBox;

				const float ITEM_GROUP_HEIGHT = 35;
				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(ITEM_GROUP_HEIGHT));
				rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
				
				GUI.Box(rowRect, GUIContent.none, rowStyle);
				
				Rect contentRect = new Rect(rowRect);
				
				contentRect.xMin += BASE_PADDING;
				
				float foldoutWidth = 20f;
				
				bool isGroupExpanded = _itemsExpandedStates[contentType];
				if (hasChildrenSubtypes || hasChildrenItems)
				{
					_itemsExpandedStates[contentType] = EditorGUI.Foldout(
						new Rect(contentRect.x, contentRect.y, foldoutWidth, contentRect.height),
						isGroupExpanded,
						GUIContent.none
					);
				}
				
				contentRect.xMin += foldoutWidth + BASE_PADDING;
				
				Texture texture = _contentConfiguration.ContentTextureConfiguration.GetTextureForType(contentType);
				float iconSize = 25f;
				var iconRect = new Rect(contentRect.x, contentRect.center.y - iconSize/2f, iconSize, iconSize);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
				
				contentRect.xMin += iconSize + BASE_PADDING;
				GUI.Label(contentRect, displayName);

				float buttonSize = 20;
				var buttonCreateRect = new Rect(contentRect.xMax - contentRect.height, contentRect.center.y - buttonSize/2f, buttonSize, buttonSize);
				if (GUI.Button(buttonCreateRect, BeamGUI.iconPlus, EditorStyles.iconButton))
				{
					Debug.Log("Create new Item");
				}
				
				if ((hasChildrenSubtypes || hasChildrenItems) && isGroupExpanded)
				{
					int nextIndent = indentLevel + 1;
					if (items.Count > 0)
					{
						float availableSpace = contentRect.width - (nextIndent * CONTENT_GROUP_INDENT_WIDTH);
						DrawTypeItems(items, nextIndent, availableSpace);
					}
					DrawGroupNode(contentType, nextIndent);
				}
			}
		}

		private void DrawTypeItems(List<LocalContentManifestEntry> items, int indentLevel, float availableWidth)
		{
			float[] columnWidths = CalculateColumnWidths(availableWidth);
			DrawItemsPanelHeader(columnWidths, indentLevel);
			DrawTypeItemsNodes(items, indentLevel, columnWidths);
		}
		
		private void DrawItemsPanelHeader(float[] columnWidths, int indentLevel)
		{
			Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(ITEMS_TABLE_ROW_HEIGHT));
			headerRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
			
			string[] labels = { "Name", "Tags", "Latest Update" };
    
			GUIStyle itemPanelHeaderRowStyle = _itemPanelHeaderRowStyle ?? EditorStyles.toolbar;
			GUIStyle itemFieldStyle = _itemPanelHeaderFieldStyle ?? EditorStyles.boldLabel;
			BeamGUI.DrawHorizontalSeparatorLine(headerRect.xMin, headerRect.yMin - 1f, headerRect.width + 1, Color.gray);
			DrawTableRow(labels, columnWidths, itemPanelHeaderRowStyle, itemFieldStyle, headerRect);
		}

		private void DrawTypeItemsNodes(List<LocalContentManifestEntry> items, int indentLevel, float[] columnWidths)
		{
			for (int index = 0; index < items.Count; index++)
			{
				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(ITEMS_TABLE_ROW_HEIGHT));
				rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
				DrawItemRow(items[index], index, rowRect, columnWidths);
				if (index == items.Count - 1)
				{
					BeamGUI.DrawHorizontalSeparatorLine(rowRect.xMin, rowRect.yMax + 2f, rowRect.width + 1, Color.gray);
				}
			}
		}
		
		private void DrawItemRow(LocalContentManifestEntry entry, int index, Rect rowRect, float[] columnWidths)
		{
			GUIStyle style = _selectedItemId == entry.FullId ? _rowSelectedItemStyle :
				index % 2 == 0 ? _rowEvenItemStyle : _rowOddItemStyle;

			GUIStyle guiStyle = style ?? EditorStyles.toolbar;
			GUIStyle labelStyle = _itemLabelStyle ?? EditorStyles.label;

			string[] values = {entry.Name, entry.Tags != null ? string.Join(", ", entry.Tags) : "-", "-----"};
			DrawTableRow(values, columnWidths, guiStyle, labelStyle, rowRect, new []{ GetIconForStatus(entry.StatusEnum)});
			if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
			{
				if (_selectedItemId == entry.FullId)
				{
					_selectedItemId = string.Empty;
					Selection.activeObject = null;
				}
				else
				{
					_selectedItemId = entry.FullId;
					_ = LoadItemScriptable(entry);
				}
				_selectedItemId = entry.FullId;
				Event.current.Use();
				Repaint();
			}
		}

		private static void DrawTableRow(string[] labels,
		                                 float[] columnWidths,
		                                 GUIStyle rowStyle,
		                                 GUIStyle fieldStyle,
		                                 Rect fullRect,
		                                 Texture[] icons = null)
		{
			if (Event.current.type == EventType.Repaint)
			{
				rowStyle.Draw(fullRect, false, false, false, false);
			}

			float separatorHeight = fullRect.height + SEPARATOR_WIDTH_AREA;
			BeamGUI.DrawVerticalSeparatorLine(fullRect.xMin, fullRect.yMin, separatorHeight, Color.gray);
			for (int i = 0; i < labels.Length; i++)
			{
				string labelValue = labels[i];
				float itemWidth = columnWidths[i];
				fullRect.xMin += BASE_PADDING;
				if (icons != null && icons.Length > i)
				{
					float iconSize = fullRect.height - BASE_PADDING;
					if (icons[i])
					{
						Rect iconRect = new Rect(fullRect.xMin, fullRect.center.y - iconSize / 2f, iconSize, fullRect.height);
						GUI.DrawTexture(iconRect, icons[i], ScaleMode.ScaleToFit);
					}
					itemWidth -= iconSize;
					fullRect.xMin += iconSize;
				}
				
				float contentWidth = itemWidth - BASE_PADDING;
				
				Rect nameRect = new Rect(fullRect.xMin, fullRect.y, contentWidth,
				                         fullRect.height);
				EditorGUI.LabelField(nameRect, labelValue, fieldStyle);
				fullRect.xMin += itemWidth;
				if (i + 1 < labels.Length)
				{
					BeamGUI.DrawVerticalSeparatorLine(fullRect.xMin, fullRect.yMin, separatorHeight, Color.gray);
					fullRect.xMin += SEPARATOR_WIDTH_AREA;
				}
			}

			BeamGUI.DrawVerticalSeparatorLine(fullRect.xMax, fullRect.yMin, separatorHeight, Color.gray);
		}

		private async Task LoadItemScriptable(LocalContentManifestEntry entry)
		{
			
			string fileContent = await File.ReadAllTextAsync(entry.JsonFilePath);
			if(!_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(entry.TypeName, out var type))
			{
				return;
			}
			
			var contentObject = CreateInstance(type) as IContentObject;
			var selectedContentObject = ClientContentSerializer.DeserializeContentFromCli(fileContent, contentObject, entry.FullId) as ContentObject;
			Selection.activeObject = selectedContentObject;
			selectedContentObject.OnEditorChanged += () =>
			{
				Debug.Log("Changed");
				var propertiesJson = ClientContentSerializer.SerializeProperties(selectedContentObject);
				_contentService.SaveContent(entry.FullId, propertiesJson);
			};
		}

		private Texture GetIconForStatus(ContentStatus statusEnum)
		{
			switch (statusEnum)
			{
				case ContentStatus.Invalid:
					return BeamGUI.iconStatusInvalid;
				case ContentStatus.Created:
					return BeamGUI.iconStatusAdded;
				case ContentStatus.Deleted:
					return BeamGUI.iconStatusDeleted;
				case ContentStatus.Modified:
					return BeamGUI.iconStatusModified;
				default:
					return null;
			}
		}

		private List<LocalContentManifestEntry> GetFilteredItems(string specificType = "")
		{
			var allItems = GetCachedManifestEntries();

			string nameSearchPartValue = GetNameSearchPartValue();
			var types = GetFilterTypeActiveItems(ContentFilterType.Type);
			var tags = GetFilterTypeActiveItems(ContentFilterType.Tag);
			var statuses = GetFilterTypeActiveItems(ContentFilterType.Status);
			
			return allItems.Where(entry =>
			{
				bool matchesType = types.Count == 0 || types.Any(type => entry.TypeName.Contains(type));
				bool matchesTags = tags.Count == 0 || tags.Any(tag => entry.Tags.Contains(tag));
				
				bool matchesStatus = statuses.Count == 0 || statuses.Any(status => entry.StatusEnum.ToString().Equals(status, StringComparison.InvariantCultureIgnoreCase));
				bool matchesSpecificType = string.IsNullOrEmpty(specificType) || entry.TypeName.Equals(specificType, StringComparison.InvariantCultureIgnoreCase);
				
				
				bool matchesName = string.IsNullOrEmpty(nameSearchPartValue) || entry.Name.Contains(nameSearchPartValue);
				bool matchesContentGroup = SelectedContentType.Count == 0 || 
				                           SelectedContentType.Contains(entry.TypeName) ||
				                           SelectedContentType.Any(item => entry.TypeName.StartsWith(item + "."));
				return matchesName && matchesContentGroup && matchesType && matchesTags && matchesStatus && matchesSpecificType;
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
		
		private static float[] CalculateColumnWidths(float totalAvailableWidth)
		{
			// Name, Tags, Last Modified

			float[] weights = {1.5f, 1.25f, 1f, 1f};

			totalAvailableWidth -= BASE_PADDING * 2 * MinWidths.Length;
			totalAvailableWidth -= SEPARATOR_WIDTH_AREA * Mathf.Max(0, MinWidths.Length - 1);

			// MinWidth sum + Vertical Line Separator Count
			float totalMinWidth = MinWidths.Sum() + (MinWidths.Length - 1);
			
			float remainingWidth = Mathf.Max(0, totalAvailableWidth - totalMinWidth);
			
			float totalWeight = weights.Sum();
			
			float[] widths = new float[MinWidths.Length];
			for (int i = 0; i < widths.Length; i++)
			{
				widths[i] = MinWidths[i] + (remainingWidth * (weights[i] / totalWeight));
			}

			return widths;
		}
	}
}
