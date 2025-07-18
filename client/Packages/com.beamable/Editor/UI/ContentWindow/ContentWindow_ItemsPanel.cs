using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Editor.Util;
using Beamable.Common.Util;
using Beamable.Editor.ContentService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	partial class ContentWindow
	{
		private static readonly NaturalStringComparer NaturalStringComparer = new();
		private const int BASE_PADDING = 5;
		private const float SEPARATOR_WIDTH_AREA = 3;
		private const float ITEMS_TABLE_ROW_HEIGHT = 20;
		private const float ITEM_GROUP_HEIGHT = 35;
		private const string FOCUS_NAME = "EditLabel";
		private const float FOLDOUT_WIDTH = 20f;
		
		private readonly Dictionary<string, Vector2> _groupScrollPositions = new();
		
		private readonly Dictionary<string, List<LocalContentManifestEntry>> _filteredCache = new();
		private readonly Dictionary<(string filterKey, ContentSortOptionType sortOption), List<LocalContentManifestEntry>> _sortedCache = new();

		private Vector2 _itemsPanelScrollPos;
		private string SelectedItemId
		{
			get
			{
				if (Selection.activeObject && Selection.activeObject is ContentObject contentObject)
				{
					return contentObject.Id;
				}

				return string.Empty;

			}
		}

		private readonly Dictionary<string, bool> _itemsExpandedStates = new();
		private GUIStyle _itemPanelHeaderRowStyle;
		private GUIStyle _itemPanelHeaderFieldStyle;
		private GUIStyle _itemLabelStyle;
		private GUIStyle _rowEvenItemStyle;
		private GUIStyle _rowOddItemStyle;
		private GUIStyle _rowSelectedItemStyle;
		private string _editItemId;
		private string[] _editLabels;
		private bool _needToFocusLabel;
		
		private readonly Color _tableSeparatorColor = new Color(0.13f, 0.13f, 0.13f);
		
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
			
			if (!string.IsNullOrEmpty(SelectedItemId) && Event.current.type == EventType.KeyDown &&
			    (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace))
			{
				
				if (Event.current.control || Event.current.alt || Event.current.shift || Event.current.command)
				{
					return;
				}

				if (_contentService.EntriesCache.TryGetValue(SelectedItemId, out var entry) &&
				    entry.StatusEnum is not ContentStatus.Deleted)
				{
					bool shouldDelete = EditorUtility.DisplayDialog("Delete Content",
					                                                "Are you sure you want to delete this content?",
					                                                "Delete", "Cancel");
					if (shouldDelete)
					{
						_contentService.DeleteContent(entry.FullId);
						Selection.activeObject = null;
						Event.current.Use();
						Repaint();
					}
				}
			}
		}
		
		private void DrawGroupNode(string parentPath = "", int indentLevel = 0)
		{
			var contentTypeItems = SortContentGroups(_contentTypeHierarchy);
			if (!contentTypeItems.TryGetValue(parentPath, out List<string> value))
			{
				return;
			}

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
				
				var items = GetFilteredItems(contentType, true);
				bool isFilteringActive = !string.IsNullOrEmpty(GetNameSearchPartValue());
				isFilteringActive |= GetFilterTypeActiveItems(ContentSearchFilterType.Status).Count > 0;
				isFilteringActive |= GetFilterTypeActiveItems(ContentSearchFilterType.Tag).Count > 0;
				if (items.Count == 0 && isFilteringActive)
				{
					continue;
				}

				string displayName = string.IsNullOrEmpty(parentPath)
					? contentType
					: contentType.Substring(parentPath.Length + 1);

				bool hasChildrenItems = items.Count > 0;
				
				bool hasChildrenSubtypes = contentTypeItems.ContainsKey(contentType) &&
				                        contentTypeItems[contentType].Count > 0;
				GUILayout.Space(BASE_PADDING);
				GUIStyle rowStyle = EditorStyles.helpBox;

				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(ITEM_GROUP_HEIGHT));
				rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
				rowRect.width -= BASE_PADDING * 3;
				
				GUI.Box(rowRect, GUIContent.none, rowStyle);
				

				var isRowHover = rowRect.Contains(Event.current.mousePosition);
				var isRowClicked = isRowHover && Event.current.rawType == EventType.MouseDown;
				var isRowLeftClick = isRowClicked && Event.current.button == 0;
				var isRowRightClick = isRowClicked && Event.current.button == 1;
				
				Rect contentRect = new Rect(rowRect);
				
				contentRect.xMin += BASE_PADDING;
				var foldoutRect = new Rect(contentRect.x, contentRect.y, FOLDOUT_WIDTH, contentRect.height);

				bool isGroupExpanded = _itemsExpandedStates[contentType];
				if (hasChildrenSubtypes || hasChildrenItems)
				{
					_itemsExpandedStates[contentType] = EditorGUI.Foldout(foldoutRect,
					                                                      isGroupExpanded,
					                                                      GUIContent.none
					);
				}
				
				contentRect.xMin += FOLDOUT_WIDTH;
				
				Texture texture = _contentConfiguration.ContentTextureConfiguration.GetTextureForType(contentType);
				float iconSize = 25f;
				var iconRect = new Rect(contentRect.x, contentRect.center.y - iconSize/2f, iconSize, iconSize);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);

				contentRect.xMin += iconSize + BASE_PADDING;
				GUI.Label(contentRect, $"{displayName} - [{items.Count}]", new GUIStyle(EditorStyles.label)
				{
					alignment = TextAnchor.MiddleLeft
				});

				float buttonSize = 20;
				var buttonCreateRect = new Rect(contentRect.xMax - contentRect.height, contentRect.center.y - buttonSize/2f, buttonSize, buttonSize);
				if (GUI.Button(buttonCreateRect, BeamGUI.iconPlus, EditorStyles.iconButton))
				{
					CreateNewItem(contentType);
				}


				if (hasChildrenSubtypes || hasChildrenItems)
				{
					EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);
				}
				
				if (isRowLeftClick)
				{
					if (!foldoutRect.Contains(Event.current.mousePosition) &&
					    !buttonCreateRect.Contains(Event.current.mousePosition))
					{
						_itemsExpandedStates[contentType] = !_itemsExpandedStates[contentType];
						GUI.changed = true;
					}
				} else if (isRowRightClick)
				{
					ShowTypeMenu(contentType);
				}
				
				if ((hasChildrenSubtypes || hasChildrenItems) && isGroupExpanded)
				{
					if (items.Count > 0)
					{
						float availableSpace = Mathf.Max(0, rowRect.width - (indentLevel * CONTENT_GROUP_INDENT_WIDTH));
						
						DrawTypeItems(items, indentLevel, availableSpace, contentType);
						GUILayout.Space(BASE_PADDING);
					}
					DrawGroupNode(contentType, indentLevel + 1);
				}
			}
		}

		private void ShowTypeMenu(string contentType)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent($"Create {contentType} Content"), false, () =>
			{
				CreateNewItem(contentType);
			});
			menu.AddSeparator("");

			menu.AddItem(new GUIContent("Open Settings"), false, () =>
			{
				SettingsService.OpenProjectSettings("Project/Beamable/Content");
			});
			if (_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(contentType, out var classType))
			{
				menu.AddItem(new GUIContent("Open Class File"), false, () =>
				{
					MethodInfo fromTypeMethod = typeof(MonoScript).GetMethod(
						"FromType", 
						BindingFlags.NonPublic | BindingFlags.Static);

					if (fromTypeMethod != null)
					{
						MonoScript script = fromTypeMethod.Invoke(null, new object[] { classType }) as MonoScript;
						if (script != null)
						{
							AssetDatabase.OpenAsset(script);
							return;
						}
					}
				});	
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Open Class File"), false);
			}
					
			menu.ShowAsContext();
		}
		
		private void CreateNewItem(string itemType)
		{
			if(!_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(itemType, out var type))
			{
				return;
			}
			
			var contentObject = CreateInstance(type) as ContentObject;
			string baseName = $"New_{itemType.Replace(".","_")}";
			int itemsWithBaseNameCount =  _contentService.GetContentsFromType(type).Count(item => item.Name.StartsWith(baseName));
			contentObject.SetContentName($"{baseName}_{itemsWithBaseNameCount}");
			contentObject.ContentStatus = ContentStatus.Created;
			contentObject.OnEditorChanged = () =>
			{
				_contentService.SaveContent(contentObject);
			};
			_contentService.ValidateForInvalidFields(contentObject);
			_contentService.SaveContent(contentObject);
			Selection.activeObject = contentObject;
			_itemsExpandedStates[itemType] = true;
			GUI.changed = true;
		}

		private void DrawTypeItems(List<LocalContentManifestEntry> items, int indentLevel, float availableWidth, string groupName)
		{
			float[] columnWidths = CalculateColumnWidths(availableWidth);
			DrawItemsPanelHeader(columnWidths, indentLevel);
			DrawTypeItemsNodes(items, indentLevel, columnWidths, availableWidth, groupName);
		}
		
		private void DrawItemsPanelHeader(float[] columnWidths, int indentLevel)
		{
			Rect headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(ITEMS_TABLE_ROW_HEIGHT));
			headerRect.width -= BASE_PADDING * 3;
			headerRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
			
			string[] labels = { "Name", "Tags", "Latest Update" };
    
			GUIStyle itemPanelHeaderRowStyle = _itemPanelHeaderRowStyle ?? EditorStyles.toolbar;
			GUIStyle itemFieldStyle = _itemPanelHeaderFieldStyle ?? EditorStyles.boldLabel;
			BeamGUI.DrawHorizontalSeparatorLine(headerRect.xMin, headerRect.yMin - 1f, headerRect.width + 1, _tableSeparatorColor);
			DrawTableRow(labels, columnWidths, itemPanelHeaderRowStyle, itemFieldStyle, headerRect);
		}

		private void DrawTypeItemsNodes(List<LocalContentManifestEntry> items, int indentLevel, float[] columnWidths, float totalWidth, string groupName)
		{
			var maxVisibleItems = _contentConfiguration.MaxContentVisibleItems;
			if (items.Count <= maxVisibleItems)
			{
				// Draw all items if there are few enough
				for (int index = 0; index < items.Count; index++)
				{
					Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(ITEMS_TABLE_ROW_HEIGHT));
					rowRect.width -= BASE_PADDING * 3;
					rowRect.xMin += indentLevel * CONTENT_GROUP_INDENT_WIDTH;
					DrawItemRow(items[index], index, rowRect, columnWidths);

					if (index == items.Count - 1)
					{
						BeamGUI.DrawHorizontalSeparatorLine(rowRect.xMin, rowRect.yMax + 2f, rowRect.width + 1,
						                                    _tableSeparatorColor);
					}
				}
			}
			else
			{
				if (!_groupScrollPositions.TryGetValue(groupName, out var scrollPos))
				{
					scrollPos = Vector2.zero;
					_groupScrollPositions[groupName] = scrollPos;
				}

				float totalHeight = items.Count * ITEMS_TABLE_ROW_HEIGHT;
				float visibleHeight = maxVisibleItems * ITEMS_TABLE_ROW_HEIGHT;

				
				Rect areaRect = GUILayoutUtility.GetRect(totalWidth, visibleHeight);
				areaRect.width -= BASE_PADDING * 3.5f;
				
				GUI.Box(areaRect, GUIContent.none);
				
				
				GUI.SetNextControlName(groupName);
				
				if (areaRect.Contains(Event.current.mousePosition))
				{
					GUI.FocusControl(groupName);
				}
				
				Rect contentRect = new Rect(0, 0, totalWidth - 18, totalHeight); // -18 for scrollbar width
				
				// Using this to prevent Unity from reusing ScrollID when there is multiple scrolls in screen
				// causing the scroll move each other
				if (Event.current.type == EventType.ScrollWheel && areaRect.Contains(Event.current.mousePosition))
				{
					scrollPos.y += Event.current.delta.y * 10f;
					scrollPos.y = Mathf.Clamp(scrollPos.y, 0, totalHeight - visibleHeight);
					_groupScrollPositions[groupName] = scrollPos;
					Event.current.Use();
				}
				
				// Begin scroll view
				var newScrollPos = GUI.BeginScrollView(
					areaRect,
					scrollPos,
					contentRect,
					false,
					true // vertical scrollbar only
				);
				
				// Calculate visible range
				int firstVisible = Mathf.FloorToInt(scrollPos.y / ITEMS_TABLE_ROW_HEIGHT);
				int lastVisible = Mathf.Min(items.Count - 1, firstVisible + maxVisibleItems - 1);

				
				for (int index = firstVisible; index <= lastVisible; index++)
				{
					Rect rowRect = new Rect(
						indentLevel * CONTENT_GROUP_INDENT_WIDTH + 3f,
						index * ITEMS_TABLE_ROW_HEIGHT,
						contentRect.width,
						ITEMS_TABLE_ROW_HEIGHT
					);

					DrawItemRow(items[index], index, rowRect, columnWidths);
				}

				
				GUI.EndScrollView();
				
				if (Event.current.type == EventType.Repaint || areaRect.Contains(Event.current.mousePosition))
				{
					_groupScrollPositions[groupName] = newScrollPos;
				}
				
				BeamGUI.DrawHorizontalSeparatorLine(
					areaRect.x,
					areaRect.y + areaRect.height,
					areaRect.width,
					_tableSeparatorColor
				);
			}
		}

		private void DrawItemRow(LocalContentManifestEntry entry, int index, Rect rowRect, float[] columnWidths)
		{
			GUIStyle style = SelectedItemId == entry.FullId ? _rowSelectedItemStyle :
				index % 2 == 0 ? _rowEvenItemStyle : _rowOddItemStyle;

			GUIStyle guiStyle = style ?? EditorStyles.toolbar;
			GUIStyle labelStyle = _itemLabelStyle ?? EditorStyles.label;

			bool isEditingName = entry.FullId == _editItemId;
			string nameLabel = isEditingName && _editLabels is {Length: > 0} ? _editLabels[0] : entry.Name;
			string[] values = {nameLabel, entry.Tags != null ? string.Join(", ", entry.Tags) : "-", "-----"};
			Texture iconForEntry = !_contentService.IsContentInvalid(entry.FullId)
								? GetIconForStatus(entry.IsInConflict, entry.StatusEnum)
								: BeamGUI.iconStatusInvalid;
			Texture[] icons = {iconForEntry};
			
			bool[] isEditable = {isEditingName};
			
			EditorGUI.BeginChangeCheck();
			if (isEditingName)
			{
				_editLabels = DrawTableRow(values, columnWidths, guiStyle, labelStyle, rowRect, icons, isEditable);
				
				bool isEditLabelFocused = GUI.GetNameOfFocusedControl() == FOCUS_NAME;
				if (isEditLabelFocused && Event.current.type is EventType.KeyDown or EventType.KeyUp &&
				    Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
				{
					RenameEntry(entry.FullId, _editLabels[0]);
				}
				
				if (Event.current.type == EventType.MouseDown && !rowRect.Contains(Event.current.mousePosition))
				{
					RenameEntry(entry.FullId, _editLabels[0]);
				}
			}
			else
			{
				DrawTableRow(values, columnWidths, guiStyle, labelStyle, rowRect, icons, isEditable);
			}
			
			if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
			{
				if (Event.current.button == 1)
				{
					ShowItemOptionsMenu(entry);
					return;
				}
				if (SelectedItemId == entry.FullId)
				{
					Selection.activeObject = null;
				}
				else
				{
					SetEntryIdAsSelected(entry.FullId);
				}
				Event.current.Use();
				Repaint();
			}
		}

		private void RenameEntry(string entryId, string newName)
		{
			_editItemId = string.Empty;
			var newId = _contentService.RenameContent(entryId, newName);
			SetEntryIdAsSelected(newId);
			Repaint();
		}

		private void ShowItemOptionsMenu(LocalContentManifestEntry entry)
		{
			GenericMenu menu = new GenericMenu();
			
			
			
			if (entry.StatusEnum is ContentStatus.Deleted)
			{
				menu.AddDisabledItem(new GUIContent("Duplicate Item"));
				menu.AddDisabledItem(new GUIContent("Rename Item"));
				menu.AddDisabledItem(new GUIContent("Delete Item"));
			}
			else
			{
				menu.AddItem(new  GUIContent("Open File Item"), false, () =>
				{
					InternalEditorUtility.OpenFileAtLineExternal(entry.JsonFilePath, 1);
				});
				menu.AddItem(new GUIContent("Duplicate Item"), false, () =>
				{
					if (_contentService.ContentScriptableCache.TryGetValue(entry.FullId, out var contentObject))
					{
						var duplicatedObject = Instantiate(contentObject);
						string baseName = $"{contentObject.ContentName}_Copy";
						int itemsWithBaseNameCount =  _contentService.GetContentsFromType(contentObject.GetType()).Count(item => item.Name.StartsWith(baseName));
						duplicatedObject.SetContentName($"{baseName}{itemsWithBaseNameCount}");
						duplicatedObject.ContentStatus = ContentStatus.Created;
						_contentService.SaveContent(duplicatedObject);
						Selection.activeObject = duplicatedObject;
					}
				
				});
				
				menu.AddItem(new GUIContent("Rename Item"), false, () =>
				{
					_editItemId = entry.FullId;
					_editLabels = new[] {entry.Name};
					_needToFocusLabel = true;
				});


				menu.AddItem(new GUIContent("Delete Item"), false, () =>
				{
					if (EditorUtility.DisplayDialog("Delete Content",
					                                "Are you sure you want to delete this content?", "Delete", "Cancel"))
					{
						_contentService.DeleteContent(entry.FullId);
					}

				});
			}

			if (entry.StatusEnum is not ContentStatus.UpToDate)
			{
				menu.AddItem(new GUIContent("Revert Item"), false, () =>
				{
					_ = _contentService.SyncContentsWithProgress(true, true, true, true, entry.FullId, ContentFilterType.ExactIds);
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Revert Item"));
			}
			
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Copy content ID to clipboard"), false, () =>
			{
				GUIUtility.systemCopyBuffer = entry.FullId;
			});

			menu.ShowAsContext();
		}

		private string[] DrawTableRow(string[] labels,
		                                 float[] columnWidths,
		                                 GUIStyle rowStyle,
		                                 GUIStyle fieldStyle,
		                                 Rect fullRect,
		                                 Texture[] icons = null, bool[] isEditLabel = null)
		{
			if (Event.current.type == EventType.Repaint)
			{
				rowStyle.Draw(fullRect, false, false, false, false);
			}
			
			float separatorHeight = fullRect.height + SEPARATOR_WIDTH_AREA;
			BeamGUI.DrawVerticalSeparatorLine(fullRect.xMin, fullRect.yMin, separatorHeight, _tableSeparatorColor);
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
						Rect iconRect = new Rect(fullRect.xMin, fullRect.center.y - iconSize / 2f, iconSize, iconSize);
						GUI.DrawTexture(iconRect, icons[i], ScaleMode.ScaleToFit);
					}
					itemWidth -= iconSize;
					fullRect.xMin += iconSize;
				}
				
				float contentWidth = itemWidth - BASE_PADDING;
				
				Rect nameRect = new Rect(fullRect.xMin, fullRect.y, contentWidth,
				                         fullRect.height);
				if (isEditLabel!= null && isEditLabel.Length > i && isEditLabel[i])
				{
					GUI.SetNextControlName(FOCUS_NAME);
					labels[i] = EditorGUI.DelayedTextField(nameRect, labels[i]);
					if (_needToFocusLabel)
					{
						EditorGUI.FocusTextInControl(FOCUS_NAME);
						_needToFocusLabel = false;
					}
				}
				else
				{
					EditorGUI.LabelField(nameRect, labelValue, fieldStyle);
				}

				fullRect.xMin += itemWidth;
				if (i + 1 < labels.Length)
				{
					BeamGUI.DrawVerticalSeparatorLine(fullRect.xMin, fullRect.yMin, separatorHeight, _tableSeparatorColor);
					fullRect.xMin += SEPARATOR_WIDTH_AREA;
				}
			}

			BeamGUI.DrawVerticalSeparatorLine(fullRect.xMax, fullRect.yMin, separatorHeight, _tableSeparatorColor);
			return labels;
		}

		private void SetEditorSelection()
		{
			if (Selection.activeObject is ContentObject contentObject)
			{
				if(!_contentService.ContentScriptableCache.ContainsKey(contentObject.Id))
				{
					Selection.activeObject = null;
				}
			}
		}

		public static Texture GetIconForStatus(bool isInConflict, ContentStatus statusEnum)
		{
			if(isInConflict)
				return BeamGUI.iconStatusConflicted;
			
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
		
		private void SetEntryIdAsSelected(string entryId)
		{
			if(_contentService.ContentScriptableCache.TryGetValue(entryId, out ContentObject value))
			{
				Selection.activeObject = value;
			}
		}

		private List<LocalContentManifestEntry> GetFilteredItems(string specificType = "", bool shouldSort = false)
		{
			string nameSearchPartValue = GetNameSearchPartValue();
			var types = GetFilterTypeActiveItems(ContentSearchFilterType.Type);
			var tags = GetFilterTypeActiveItems(ContentSearchFilterType.Tag);
			var statuses = GetFilterTypeActiveItems(ContentSearchFilterType.Status);
			
			var filterKey = $"{specificType}|{nameSearchPartValue}|{string.Join("-",tags)}|{string.Join("-",statuses)}|{string.Join("-",statuses)}";

			if (!_filteredCache.TryGetValue(filterKey, out var filteredItems))
			{
				var allItems = GetCachedManifestEntries();
				filteredItems = allItems.Where(entry => FilterItem(specificType, types, entry, tags, statuses, nameSearchPartValue)).ToList();
				_filteredCache[filterKey] = filteredItems;
			}

			List<LocalContentManifestEntry> contentManifestEntries = shouldSort ? SortItems(filterKey, filteredItems, _currentSortOption) : filteredItems;
			return contentManifestEntries;
		}

		private bool FilterItem(string specificType,
		                        HashSet<string> types,
		                        LocalContentManifestEntry entry,
		                        HashSet<string> tags,
		                        HashSet<string> statuses,
		                        string nameSearchPartValue)
		{
			// Check if it is the specificType
			if (!string.IsNullOrEmpty(specificType) &&
			    !entry.TypeName.Equals(specificType, StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}
			
			// Check if matches any Type
			if (types.Count > 0 && types.All(type => !entry.TypeName.Contains(type)))
			{
				return false;
			}

			// Check if matches any tag
			if (tags.Count > 0 && tags.All(tag => !entry.Tags.Contains(tag)))
			{
				return false;
			}

			// Check if matches any status
			if (statuses.Count > 0 && !statuses.Any(ValidateEntryStatus))
			{
				return false;
			}

			// Check if matches the name filter
			if (!string.IsNullOrEmpty(nameSearchPartValue) &&
			    !entry.Name.Contains(nameSearchPartValue))
			{
				return false;
			}
			
			// Check if it matches the Selected Content Types Groups (left bar UI)
			bool matchesContentGroup = SelectedContentType.Count == 0 ||
			                           SelectedContentType.Contains(entry.TypeName) ||
			                           SelectedContentType.Any(item => entry.TypeName.StartsWith(item + "."));
			
			return matchesContentGroup;

			bool ValidateEntryStatus(string status)
			{
				if (status == StatusMapToString[ContentFilterStatus.Invalid])
				{
					return _contentService.IsContentInvalid(entry.FullId);
				}

				if (status == StatusMapToString[ContentFilterStatus.Conflicted])
				{
					return entry.IsInConflict;
				}

				return FilterStatusToContentStatus[status] == entry.StatusEnum;
			}
		}

		private List<LocalContentManifestEntry> SortItems(string cacheKey, IEnumerable<LocalContentManifestEntry> items, ContentSortOptionType option)
		{
			var sortKey = (cacheKey, option);

			if (_sortedCache.TryGetValue(sortKey, out var sortedItems))
			{
				return sortedItems;
			}

			sortedItems = (_currentSortOption switch
			{
				ContentSortOptionType.IdAscending => items.OrderBy(item => item.Name, NaturalStringComparer),
				ContentSortOptionType.IdDescending => items.OrderByDescending(item => item.Name, NaturalStringComparer),
				ContentSortOptionType.TypeAscending => items.OrderBy(item => item.TypeName, NaturalStringComparer)
				                                            .ThenBy(item => item.Name),
				ContentSortOptionType.TypeDescending => items
				                                        .OrderByDescending(item => item.TypeName, NaturalStringComparer)
				                                        .ThenBy(item => item.Name),
				ContentSortOptionType.Status => items.OrderBy(item => item.CurrentStatus)
				                                     .ThenBy(item => item.Name, NaturalStringComparer),
				ContentSortOptionType.ValidStatus => items.OrderBy(item => _contentService.IsContentInvalid(item.FullId))
				                                          .ThenBy(item => item.CurrentStatus)
				                                          .ThenBy(item => item.Name, NaturalStringComparer),
				_ => throw new ArgumentOutOfRangeException()
			}).ToList();
			_sortedCache[sortKey] = sortedItems;
			
			return sortedItems;
		}

		private Dictionary<string, List<string>> SortContentGroups(Dictionary<string, List<string>> baseDictionary)
		{
			switch (_currentSortOption)
			{
				
				case ContentSortOptionType.TypeAscending:
					return baseDictionary.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
					                     .ToDictionary(item => item.Key,
					                                   item => 
						                                   item.Value.OrderBy(listItem => listItem).ToList());
				case ContentSortOptionType.TypeDescending:
					return baseDictionary.OrderByDescending(item => item.Key, StringComparer.OrdinalIgnoreCase)
					                     .ToDictionary(item => item.Key,
					                                   item => 
						                                   item.Value.OrderByDescending(listItem => listItem).ToList());
				default:
					return baseDictionary;
			}
		}
		
		private static float[] CalculateColumnWidths(float totalAvailableWidth)
		{
			// Name, Tags, Last Modified
			float[] minWidths = {200f, 100f, 100f};
			float[] weights = {2f, 1.25f, 1f};

			totalAvailableWidth -= BASE_PADDING * 2 * minWidths.Length;
			totalAvailableWidth -= SEPARATOR_WIDTH_AREA * Mathf.Max(0, minWidths.Length - 1);

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
