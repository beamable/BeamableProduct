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
using Object = UnityEngine.Object;

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
		private const float ITEM_LIST_MIN_WIDTH = 450;

		private readonly Dictionary<string, Vector2> _groupScrollPositions = new();
		
		private readonly Dictionary<string, List<LocalContentManifestEntry>> _filteredCache = new();
		private readonly Dictionary<(string filterKey, ContentSortOptionType sortOption), List<LocalContentManifestEntry>> _sortedCache = new();

		private Vector2 _itemsPanelScrollPos;

		private List<string> MultiSelectItemIds
		{
			get
			{
				// TODO: cache this once per frame; could be a lot of allocation :(
				var ids = new List<string>();
				foreach (var obj in Selection.objects)
				{
					if (obj && obj is ContentObject content)
					{
						ids.Add(content.Id);
					}
				}

				return ids;
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
		
		private readonly Color _tableSeparatorColor = new Color(0.13f, 0.13f, 0.13f);
		
		private void BuildItemsPanelStyles()
		{
			if (_itemPanelHeaderRowStyle == null || _itemPanelHeaderRowStyle.normal.background == null)
			{
				_itemPanelHeaderRowStyle = new GUIStyle(EditorStyles.toolbar)
				{
					normal = {background = BeamGUI.CreateColorTexture(new Color(0.28f, 0.28f, 0.28f))},
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.MiddleLeft,
					fixedHeight = EditorGUIUtility.singleLineHeight * 1.15f
				};
			}

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

			if (_rowEvenItemStyle == null || _rowEvenItemStyle.normal.background == null)
			{
				_rowEvenItemStyle = new GUIStyle(EditorStyles.label)
				{
					normal = {background = BeamGUI.CreateColorTexture(new Color(0.2f, 0.2f, 0.2f))},
					margin = new RectOffset(0, 0, 0, 0),
					fixedHeight = itemRowHeight,
					alignment = TextAnchor.MiddleLeft
				};
			}

			if (_rowOddItemStyle == null || _rowOddItemStyle.normal.background == null)
			{
				_rowOddItemStyle = new GUIStyle(EditorStyles.label)
				{
					normal = {background = BeamGUI.CreateColorTexture(new Color(0.22f, 0.22f, 0.22f))},
					margin = new RectOffset(0, 0, 0, 0),
					fixedHeight = itemRowHeight,
					alignment = TextAnchor.MiddleLeft
				};
			}


			if (_rowSelectedItemStyle == null || _rowSelectedItemStyle.normal.background == null)
			{
				_rowSelectedItemStyle = new GUIStyle(EditorStyles.label)
				{
					normal = {background = BeamGUI.CreateColorTexture(new Color(0.21f, 0.32f, 0.49f))},
					margin = new RectOffset(0, 0, 0, 0),
					fixedHeight = itemRowHeight,
					alignment = TextAnchor.MiddleLeft
				};
			}
		}
		
		
		private void DrawContentItemPanel()
		{
			
			if (_contentService.isReloading)
			{
				GUILayout.BeginVertical();
				GUILayout.Space(24);
				
				BeamGUI.LoadingSpinnerWithState("Fetching Content...");

				if (_contentService.LatestProgressUpdate != null && _contentService.LatestProgressUpdate.total > 0)
				{
					var update = _contentService.LatestProgressUpdate;
					var ratio = update.completed / (float) update.total;
					BeamGUI.DrawLoadingBar(update.message, ratio);
				}
				
				GUILayout.EndVertical();
				return;
			}
			
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true));
			{
				_itemsPanelScrollPos = GUILayout.BeginScrollView(_itemsPanelScrollPos);
				{
					var options = new List<GUILayoutOption> { GUILayout.ExpandWidth(true) };
					var availableWidth = _mainSplitter.cellNormalizedSizes.Last() * EditorGUIUtility.currentViewWidth;
					if(availableWidth < ITEM_LIST_MIN_WIDTH) 
						options.Add(GUILayout.MinWidth(ITEM_LIST_MIN_WIDTH));
					GUILayout.BeginVertical(options.ToArray());
					DrawGroupNode();
					GUILayout.EndVertical();
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();
			
			if (MultiSelectItemIds.Count > 0 && Event.current.type == EventType.KeyDown &&
			    (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace))
			{
				
				if (Event.current.control || Event.current.alt || Event.current.shift || Event.current.command)
				{
					return;
				}

				var toBeDeleted = new List<LocalContentManifestEntry>();
				foreach (var id in MultiSelectItemIds)
				{
					if (_contentService.EntriesCache.TryGetValue(id, out var contentEntry) &&
					    contentEntry.StatusEnum is not ContentStatus.Deleted)
					{
						toBeDeleted.Add(contentEntry);
					}
				}
				
				if (toBeDeleted.Count > 0)
				{
					var message = toBeDeleted.Count == 1
						? "Are you sure you want to delete this content?"
						: $"Are you sure you want to delete these {toBeDeleted.Count} contents?";
					
					bool shouldDelete = EditorUtility.DisplayDialog("Delete Content",
					                                                message,
					                                                "Delete", "Cancel");
					if (shouldDelete)
					{
						_contentService.TempDisableWatcher(() =>
						{
							foreach (var id in toBeDeleted)
							{
								_contentService.DeleteContent(id.FullId);
							}
							ClearSelection();
							
						});
						
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
					// We need to try to drawn subtype to check if any subtype content is matching the filter
					DrawGroupNode(contentType, 0);
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
				rowRect.xMin += indentLevel * INDENT_WIDTH;
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
						float availableSpace = Mathf.Max(0, rowRect.width - (indentLevel * INDENT_WIDTH));
						
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
			string baseName = $"New_{itemType.Replace(".","_")}_";
			int nextNumber = _contentService.GetContentsFromType(type)
			                                .Select(item => item.Name)
			                                .Where(itemName => itemName.StartsWith(baseName))
			                                .Select(itemName => {
				                                string numPart = itemName.Substring(baseName.Length);
				                                return int.TryParse(numPart, out int num) ? num : -1;
			                                })
			                                .Where(num => num >= 0)
			                                .DefaultIfEmpty(-1)
			                                .Max() + 1;
			contentObject.SetContentName($"{baseName}{nextNumber}");
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
			headerRect.xMin += indentLevel * INDENT_WIDTH;
			
			string[] labels = { "Name", "Tags", "Latest Update"};
    
			GUIStyle itemPanelHeaderRowStyle = _itemPanelHeaderRowStyle ?? EditorStyles.toolbar;
			GUIStyle itemFieldStyle = _itemPanelHeaderFieldStyle ?? EditorStyles.boldLabel;
			BeamGUI.DrawHorizontalSeparatorLine(headerRect.xMin, headerRect.yMin - 1f, headerRect.width + 1, _tableSeparatorColor);
			DrawTableRow(labels, columnWidths, itemPanelHeaderRowStyle, itemFieldStyle, headerRect);
		}

		public List<LocalContentManifestEntry> _frameRenderedItems = new List<LocalContentManifestEntry>();
		
		private void DrawTypeItemsNodes(List<LocalContentManifestEntry> items, int indentLevel, float[] columnWidths, float totalWidth, string groupName)
		{
			_frameRenderedItems.AddRange(items);
			var maxVisibleItems = _contentConfiguration.MaxContentVisibleItems;
			if (items.Count <= maxVisibleItems)
			{
				// Draw all items if there are few enough
				for (int index = 0; index < items.Count; index++)
				{
					Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(ITEMS_TABLE_ROW_HEIGHT));
					rowRect.width -= BASE_PADDING * 3;
					rowRect.xMin += indentLevel * INDENT_WIDTH;
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
				
				
				Rect contentRect = new Rect(0, 0, totalWidth - 18, totalHeight); // -18 for scrollbar width

				bool isEditingFieldOnGroup = !string.IsNullOrEmpty(_editItemId) && _editItemId.Contains(groupName);
				
				// Using this to prevent Unity from reusing ScrollID when there is multiple scrolls in screen
				// causing the scroll move each other
				if (Event.current.type == EventType.ScrollWheel && areaRect.Contains(Event.current.mousePosition))
				{
					scrollPos.y += Event.current.delta.y * 10f;
					scrollPos.y = Mathf.Clamp(scrollPos.y, 0, totalHeight - visibleHeight);
					_groupScrollPositions[groupName] = scrollPos;
					if(!isEditingFieldOnGroup)
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
						indentLevel * INDENT_WIDTH + 3f,
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

		public SerializableDictionaryStringToInt _itemContentIdToRowRect = new SerializableDictionaryStringToInt();

		private void DrawItemRow(LocalContentManifestEntry entry, int index, Rect rowRect, float[] columnWidths)
		{
			GUIStyle style = MultiSelectItemIds.Contains(entry.FullId)
				? _rowSelectedItemStyle
				: index % 2 == 0
					? _rowEvenItemStyle
					: _rowOddItemStyle;

			GUIStyle guiStyle = style ?? EditorStyles.toolbar;
			GUIStyle labelStyle = _itemLabelStyle ?? EditorStyles.label;

			bool isEditingName = entry.FullId == _editItemId;
			string nameLabel = isEditingName && _editLabels is {Length: > 0} ? _editLabels[0] : entry.Name;

			string lastUpdateDate = DateTimeOffset.FromUnixTimeMilliseconds(entry.LatestUpdateAtDate).ToLocalTime().ToString("g");

			string[] values = {nameLabel, entry.Tags != null ? string.Join(", ", entry.Tags) : "-", lastUpdateDate};
			Texture iconForEntry = !_contentService.IsContentInvalid(entry.FullId)
				? GetIconForStatus(entry.IsInConflict, entry.StatusEnum)
				: BeamGUI.iconStatusInvalid;
			Texture[] icons = {iconForEntry};

			bool[] isEditable = {isEditingName};
			string[] editableId = {entry.FullId};

		EditorGUI.BeginChangeCheck();
			if (isEditingName)
			{
				_editLabels = DrawTableRow(values, columnWidths, guiStyle, labelStyle, rowRect, icons, isEditable, editableId);
				
				bool isEditLabelFocused = GUI.GetNameOfFocusedControl() == entry.FullId;
				if (isEditLabelFocused && Event.current.type is EventType.KeyDown or EventType.KeyUp &&
				    Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
				{
					RenameEntry(entry.FullId, _editLabels[0]);
				}
				
				if (isEditLabelFocused && Event.current.type is EventType.KeyDown or EventType.KeyUp &&
				    Event.current.keyCode is KeyCode.Escape)
				{
					_editItemId = string.Empty;
				}
				
				if (Event.current.type == EventType.MouseDown && !rowRect.Contains(Event.current.mousePosition))
				{
					RenameEntry(entry.FullId, _editLabels[0]);
				}
			}
			else
			{
				DrawTableRow(values, columnWidths, guiStyle, labelStyle, rowRect, icons, isEditable, editableId);
			}

			_itemContentIdToRowRect[entry.FullId] = (int)rowRect.center.y;
			
			if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
			{
				var isShiftClick = Event.current.shift;
				var isLeftClick = Event.current.button == 0;
				var isRightClick = Event.current.button == 1;
				var isDoubleClick = Event.current.clickCount == 2;
				
				var isAddClick = (Event.current.control || Event.current.alt  ||
				                    Event.current.command);
				var isRepeatClick = MultiSelectItemIds.Contains(entry.FullId);

				if (entry.StatusEnum is not ContentStatus.Deleted && isDoubleClick && !isAddClick && isLeftClick && !isEditingName && !isShiftClick)
				{
					SetEntryIdAsSelected(entry.FullId);
					StartEditEntryName(entry);
					Event.current.Use();
					Repaint();
					return;
				}
				
				var shouldShowMenu = false;
				if (!isShiftClick)
				{
					// either adding, setting, or removing selection.
					switch (isAddClick, isRepeatClick)
					{
						case (true, false):
							AddEntryIdAsSelected(entry.FullId);
							shouldShowMenu = isRightClick;
							break;
						case (false, false):
							ClearSelection();
							SetEntryIdAsSelected(entry.FullId);
							shouldShowMenu = isRightClick;
							break;
						case (true, true):
							if (isRightClick)
							{
								shouldShowMenu = true;
							}
							else
							{
								RemoveEntryIdAsSelected(entry.FullId);	
							}
							break;
						case (false, true):
							if (isRightClick)
							{
								shouldShowMenu = true;
							}
							else
							{
								ClearSelection();
							}
							break;
					}
				}
				else if (isShiftClick && isLeftClick)
				{
					if (Selection.objects.Length == 0)
					{
						// there is no selection yet, so handle this as the first click.
						ClearSelection();
						SetEntryIdAsSelected(entry.FullId);
					}
					else
					{
						var first = Selection.objects.OfType<ContentObject>().FirstOrDefault();
						if (first == null)
						{
							// there is no CONTENT selection yet, so handle this as the first click.
							ClearSelection();
							SetEntryIdAsSelected(entry.FullId);
						}
						else
						{
							var firstId = first.Id;
							var entryId = entry.FullId;
							AddDelayedAction(() => {
								var from = _frameRenderedItems.FindIndex(c => c.FullId == firstId);
								var to = _frameRenderedItems.FindIndex(c => c.FullId == entryId);
								
								if (from == -1 || to == -1)
								{
									Debug.LogError($"Beam Content Manager cannot find index map between ids=[{firstId}:{entryId}] as index=[{from}:{to}] ");
								}
								else
								{
									var min = Math.Min(from, to);
									var max = Math.Max(from, to);
									for (var i = min; i <= max; i++)
									{
										AddEntryIdAsSelected(_frameRenderedItems[i].FullId);
									}
								}
							});
						}
					}
				}

				if (shouldShowMenu)
				{
					var evt = Event.current;
					var screenMousePosition = GUIUtility.GUIToScreenPoint(evt.mousePosition);
					Event.current.Use();
					Repaint();
					// Fix: https://github.com/beamable/BeamableProduct/issues/4479
					// We need to do a double-delay call to ensure that the ActiveSelected items will be properly drawn as
					// selected, the second delay is necessary because when setting the Selection.activeObject Unity forces
					// a repaint, but the IMGUI can still access the old selection state.
					// So we need to wait another frame to make it right
					EditorApplication.delayCall += () =>
					{
						Repaint();
						EditorApplication.delayCall += () => ShowItemOptionsMenu(screenMousePosition);
					};
					return;
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

		private void ShowItemOptionsMenu(Vector2 screenPosition)
		{
			GenericMenu menu = new GenericMenu();

			var selection = Selection.objects;
			var entries = new List<LocalContentManifestEntry>();
			foreach (var selected in selection)
			{
				if (selected is ContentObject content)
				{
					if (_contentService.EntriesCache.TryGetValue(content.Id, out var entry))
					{
						entries.Add(entry);
					}
				}
			}

			if (entries.Count == 1)
			{
				var entry = entries[0];
				if (entry.StatusEnum is ContentStatus.Deleted)
				{
					menu.AddDisabledItem(new GUIContent("Duplicate Item"));
					menu.AddDisabledItem(new GUIContent("Rename Item"));
					menu.AddDisabledItem(new GUIContent("Delete Item"));
				}
				else
				{
					menu.AddItem(new GUIContent("Open File Item"), false,
					             () => { InternalEditorUtility.OpenFileAtLineExternal(entry.JsonFilePath, 1); });
					menu.AddItem(new GUIContent("Duplicate Item"), false, () =>
					{
						DuplicateContent(entry);
					});

					menu.AddItem(new GUIContent("Rename Item"), false, () =>
					{
						StartEditEntryName(entry);
					});


					menu.AddItem(new GUIContent("Delete Item"), false, () =>
					{
						if (EditorUtility.DisplayDialog("Delete Content",
						                                "Are you sure you want to delete this content?", "Delete",
						                                "Cancel"))
						{
							_contentService.DeleteContent(entry.FullId);
							if (entry.StatusEnum is ContentStatus.Created)
							{
								ClearSelection();
							}
							else
							{
								SetEntryIdAsSelected(entry.FullId);
							}
						}

					});
				}

				if (entry.StatusEnum is not ContentStatus.UpToDate and not ContentStatus.Created)
				{
					menu.AddItem(new GUIContent("Revert Item"), false,
					             () =>
					             {
						             _ = _contentService.SyncContentsWithProgress(
							             true, true, true, true, entry.FullId, ContentFilterType.ExactIds);
					             });
				}
				else
				{
					menu.AddDisabledItem(new GUIContent("Revert Item"));
				}

				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Copy content ID to clipboard"), false,
				             () => { GUIUtility.systemCopyBuffer = entry.FullId; });
			}
			else if (entries.Count > 1)
			{
				var allDeleted = entries.All(e => e.StatusEnum == ContentStatus.Deleted);
				var allUpToDate = entries.All(e => e.StatusEnum == ContentStatus.UpToDate);
				if (allDeleted)
				{
					menu.AddDisabledItem(new GUIContent("Delete Items"));
				}
				else
				{
					menu.AddItem(new GUIContent("Delete Existing Items"), false, () =>
					{
						if (EditorUtility.DisplayDialog("Delete Content",
						                                $"Are you sure you want to delete these {entries.Count} contents?", "Delete",
						                                "Cancel"))
						{
							_contentService.TempDisableWatcher(() =>
							{
								foreach (var entry in entries)
								{
									_contentService.DeleteContent(entry.FullId);
								}

								ClearSelection();
								
							});
						}
					});
				}

				menu.AddItem(new GUIContent("Duplicate Items"), false, () =>
				{
					ClearSelection();

					// TODO: it would be better to pre-calculate all of the file names for the new content
					//       and then temporarily disable the content watcher.
					foreach (var entry in entries)
					{
						if (_contentService.TryGetContentObject(entry.FullId, out var contentObject))
						{
							var duplicatedObject = Instantiate(contentObject);
							string baseName = $"{contentObject.ContentName}_Copy";
							int itemsWithBaseNameCount = _contentService
							                             .GetContentsFromType(contentObject.GetType())
							                             .Count(item => item.Name.StartsWith(baseName));
							duplicatedObject.SetContentName($"{baseName}{itemsWithBaseNameCount}");
							duplicatedObject.ContentStatus = ContentStatus.Created;
							_contentService.SaveContent(duplicatedObject);
						}
					}

				});

				if (allUpToDate)
				{
					menu.AddDisabledItem(new GUIContent("Revert Items"));
				}
				else
				{
					menu.AddItem(new GUIContent("Revert changed Items"), false, () =>
					{
						_ = _contentService.SyncContentsWithProgress(
							true, true, true, true, string.Join(",",entries.Select(e => e.FullId)), ContentFilterType.ExactIds);
					});
				}
				
			}

			menu.DropDown(new Rect(GUIUtility.ScreenToGUIPoint(screenPosition), Vector2.zero));
		}

		private void DuplicateContent(LocalContentManifestEntry entry)
		{
			if (_contentService.DuplicateContent(entry, out var duplicatedObject))
			{
				SetEntryIdAsSelected(duplicatedObject.Id);
			}
			
		}

		private void StartEditEntryName(LocalContentManifestEntry entry)
		{
			_editItemId = entry.FullId;
			_editLabels = new[] {entry.Name};
		}

		private string[] DrawTableRow(string[] labels,
		                              float[] columnWidths,
		                              GUIStyle rowStyle,
		                              GUIStyle fieldStyle,
		                              Rect fullRect,
		                              Texture[] icons = null, bool[] isEditLabel = null, string[] fieldID = null)
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
					string controlName = fieldID != null && fieldID.Length > i ? fieldID[i] : FOCUS_NAME;
					GUI.SetNextControlName(controlName);
					// Had to change to EditorGUI.TextField as clicking outside the Window was not working with EditorGUI.DelayedTextField
					labels[i] = EditorGUI.TextField(nameRect, labels[i]);
					if (GUI.GetNameOfFocusedControl() != controlName && Event.current.type == EventType.Repaint)
					{
						GUI.FocusControl(controlName);
						EditorGUI.FocusTextInControl(controlName);
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
				if(!_contentService.TryGetContentObject(contentObject.Id, out _))
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
			if (!string.IsNullOrEmpty(_editItemId))
			{
				RenameEntry(_editItemId, _editLabels[0]);
			}
			
			if(_contentService.TryGetContentObject(entryId, out ContentObject value))
			{
				Selection.activeObject = value;
			}
		}

		private void ClearSelection()
		{
			Selection.activeObject = null;
			Selection.objects = new Object[] { };
		}
		private void AddEntryIdAsSelected(string entryId)
		{
			var selection = Selection.objects;
			foreach (var existing in selection)
			{
				if (existing is ContentObject content && content.Id == entryId)
				{
					// the item already exists in the selection.
					return; 
				}
			}
			
			var newSelection = selection.ToList();
			
			if (_contentService.TryGetContentObject(entryId, out ContentObject value))
			{
				newSelection.Add(value);
			}

			Selection.objects = newSelection.ToArray();
		}
		
		private void RemoveEntryIdAsSelected(string entryId)
		{
			var selection = Selection.objects;
			var found = false;
			var newSelection = selection.ToList();
			foreach (var existing in selection)
			{
				if (existing is ContentObject content && content.Id == entryId)
				{
					found = true;
					newSelection.Remove(existing);
				}
			}

			// the item does not exist in the selection
			if (!found) return; 

			Selection.objects = newSelection.ToArray();
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
