using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common;
using Beamable.Editor.Util;
using Beamable.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		private static float TableScrollbarWidth => GUI.skin.verticalScrollbar.fixedWidth + GUI.skin.verticalScrollbar.margin.left;
		private const int STATUS_COLUMN = 1;
		private const float ITEMS_TABLE_ROW_HEIGHT = 20;
		private const float ITEM_GROUP_HEIGHT = 24;
		private const string FOCUS_NAME = "EditLabel";
		private const float FOLDOUT_WIDTH = 20f;
		private const float TYPE_ICON_SIZE = 16f;
		private const float ITEM_LIST_MIN_WIDTH = 600;
		private static readonly string[] ItemColumnLabels = { "Name", "Status", "Tags", "Latest Update" };
		private float _itemsTableWidth = ITEM_LIST_MIN_WIDTH;
		private float[] _itemsColumnWidths = CalculateColumnWidths(ITEM_LIST_MIN_WIDTH);

		private readonly Dictionary<string, Vector2> _groupScrollPositions = new();
		
		private readonly Dictionary<string, List<LocalContentManifestEntry>> _filteredCache = new();
		private readonly Dictionary<(string filterKey, ContentSortOptionType sortOption), List<LocalContentManifestEntry>> _sortedCache = new();

		private Vector2 _itemsPanelScrollPos;

		private readonly ContentWindow_TooltipsHelper _tooltipsHelper = new();

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
		private GUIStyle _incomingStatusStyle;
		private GUIStyle _conflictStatusStyle;
		private GUIStyle _invalidStatusStyle;
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

			_itemPanelHeaderFieldStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
			{
				alignment = TextAnchor.MiddleLeft,
				fontSize = 12,
				padding = new RectOffset(0, 0, 0, 0),
				fixedHeight = EditorGUIUtility.singleLineHeight * 1.15f
			};
			
			_itemLabelStyle ??= new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft, padding = new RectOffset(0, 0, 0, 0),
				#if UNITY_6000_3_OR_NEWER
				clipping = TextClipping.Ellipsis,
#else
				clipping = TextClipping.Clip,
#endif
				wordWrap = false
			};
			

			_incomingStatusStyle ??= new GUIStyle(_itemLabelStyle) { normal = { textColor = new Color(0.2f, 0.75f, 0.95f) } };
			_conflictStatusStyle ??= new GUIStyle(_itemLabelStyle) { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
			_invalidStatusStyle ??= new GUIStyle(_itemLabelStyle) { normal = { textColor = new Color(1f, 0.8f, 0.25f) } };
			
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
			
			GUILayout.BeginVertical(GUIStyle.none, GUILayout.ExpandHeight(true));
			DrawItemsPanelHeader();
			_itemsPanelScrollPos = GUILayout.BeginScrollView(_itemsPanelScrollPos, false, true);
			GUILayout.BeginVertical(GUIStyle.none, GUILayout.MinWidth(_itemsTableWidth + TableScrollbarWidth));
			DrawGroupNode();
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			
			if (CanHandleContentInput && MultiSelectItemIds.Count > 0 && Event.current.type == EventType.KeyDown &&
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
					
					bool shouldDelete = ConfirmContentInput(() => EditorUtility.DisplayDialog("Delete Content",
					                                                message,
					                                                "Delete", "Cancel"));
					if (shouldDelete)
					{
						_contentService.TempDisableWatcher(() => DeleteContentEntries(toBeDeleted)).Error(Debug.LogException);
						
						Selection.activeObject = null;
						Event.current.Use();
						Repaint();
					}
				}
			}
		}
		
		private async Promise DeleteContentEntries(IEnumerable<LocalContentManifestEntry> entries)
		{
			// The watcher may have taken time to stop since the deletion was requested.
			if (!CanHandleContentInput) return;
			var scope = _contentService.GetContentScopeKey();
			// Start every deletion before awaiting, so all targets retain the original scope and block new writes.
			var deletions = entries.Select(entry => _contentService.DeleteContent(entry.FullId)).ToArray();
			Exception failure = null;
			foreach (var deletion in deletions)
			{
				try { await deletion; }
				catch (Exception ex) { failure = failure ?? ex; }
			}
			if (scope == _contentService.GetContentScopeKey()) ClearSelection();
			// Keep the watcher paused until every deletion settles, even if one file cannot be removed.
			if (failure != null) throw failure;
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

				
				Rect rowRect = EditorGUILayout.GetControlRect(false, ITEM_GROUP_HEIGHT, GUIStyle.none);
				rowRect.width = _itemsTableWidth + TableScrollbarWidth;

				BeamGUI.DrawHorizontalSeparatorLine(rowRect.x, rowRect.y, rowRect.width, _tableSeparatorColor);
				

				var isRowHover = rowRect.Contains(Event.current.mousePosition);
				var isRowClicked = isRowHover && Event.current.rawType == EventType.MouseDown;
				var isRowLeftClick = isRowClicked && Event.current.button == 0;
				var isRowRightClick = isRowClicked && Event.current.button == 1;
				
				Rect contentRect = new Rect(rowRect);
				
				contentRect.xMin += BASE_PADDING + indentLevel * INDENT_WIDTH;
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
				float iconSize = TYPE_ICON_SIZE;
				var iconRect = new Rect(contentRect.x, contentRect.center.y - iconSize/2f, iconSize, iconSize);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);

				contentRect.xMin += iconSize + BASE_PADDING;


				float buttonSize = 20;
				var buttonCreateRect = new Rect(contentRect.xMax - contentRect.height, contentRect.center.y - buttonSize/2f, buttonSize, buttonSize);
				var labelRect = contentRect;
				labelRect.xMax = buttonCreateRect.xMin - BASE_PADDING;
				GUI.Label(labelRect, new GUIContent($"{displayName} - [{items.Count}]", contentType), _itemLabelStyle);
				using (new EditorGUI.DisabledScope(!_contentTypeReflectionCache.ContentTypeToClass.ContainsKey(contentType)))
				{
					if (GUI.Button(buttonCreateRect, BeamGUI.iconPlus, EditorStyles.iconButton)) CreateNewItem(contentType);
				}


				if (hasChildrenSubtypes || hasChildrenItems)
				{
					EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);
				}
				
				if (isRowLeftClick && (hasChildrenSubtypes || hasChildrenItems))
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
						DrawTypeItemsNodes(items, _itemsColumnWidths, _itemsTableWidth, contentType, indentLevel);
					}
					DrawGroupNode(contentType, indentLevel + 1);
				}
			}
		}

		private void ShowTypeMenu(string contentType)
		{
			if (!CanHandleContentInput) return;
			var menu = new GenericMenu();
			var createLabel = new GUIContent($"Create {contentType} Content");
			if (_contentTypeReflectionCache.ContentTypeToClass.ContainsKey(contentType))
				menu.AddItem(createLabel, false, () => CreateNewItem(contentType));
			else menu.AddDisabledItem(createLabel);
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
			if (!CanHandleContentInput) return;
			if(!_contentTypeReflectionCache.ContentTypeToClass.TryGetValue(itemType, out var type))
			{
				return;
			}
			
			var contentObject = CreateInstance(type) as ContentObject;
			// Force Unity's serializer to instantiate all nested [Serializable] reference fields
			// (e.g. Optional abd strings types) with proper default instances instead of leaving them null.
			JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(contentObject), contentObject);
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

		private void DrawItemsPanelHeader()
		{
			var viewport = EditorGUILayout.GetControlRect(false, ITEMS_TABLE_ROW_HEIGHT, GUIStyle.none);
			// Reserve both the outer scrollbar and the optional virtualized-group scrollbar.
			// A single width keeps nested groups and virtualized rows under the same header.
			if (Event.current.type == EventType.Repaint)
			{
				var width = Mathf.Max(ITEM_LIST_MIN_WIDTH, viewport.width - 2 * TableScrollbarWidth);
				if (!Mathf.Approximately(width, _itemsTableWidth))
				{
					_itemsTableWidth = width;
					_itemsColumnWidths = CalculateColumnWidths(width);
				}
			}
			viewport.width = Mathf.Max(0, viewport.width - TableScrollbarWidth);
			_itemsPanelScrollPos.x = Mathf.Clamp(_itemsPanelScrollPos.x, 0,
				Mathf.Max(0, _itemsTableWidth + TableScrollbarWidth - viewport.width));
			GUI.BeginGroup(viewport);
			DrawTableRow(ItemColumnLabels, _itemsColumnWidths, _itemPanelHeaderRowStyle, _itemPanelHeaderFieldStyle,
				new Rect(-_itemsPanelScrollPos.x, 0, _itemsTableWidth, ITEMS_TABLE_ROW_HEIGHT));
			GUI.EndGroup();
		}

		public List<LocalContentManifestEntry> _frameRenderedItems = new List<LocalContentManifestEntry>();
		
		private void DrawTypeItemsNodes(List<LocalContentManifestEntry> items, float[] columnWidths, float totalWidth, string groupName, int indentLevel)
		{
			_frameRenderedItems.AddRange(items);
			var maxVisibleItems = _contentConfiguration.MaxContentVisibleItems;
			if (items.Count <= maxVisibleItems)
			{
				// Draw all items if there are few enough
				for (int index = 0; index < items.Count; index++)
				{
					Rect rowRect = EditorGUILayout.GetControlRect(false, ITEMS_TABLE_ROW_HEIGHT, GUIStyle.none);
					rowRect.width = totalWidth;
					DrawItemRow(items[index], index, rowRect, columnWidths, indentLevel);

					if (index == items.Count - 1)
					{
						var insetRow = GetItemRowRect(rowRect, columnWidths[0], indentLevel);
						BeamGUI.DrawHorizontalSeparatorLine(insetRow.xMin, insetRow.yMax + 2f, insetRow.width,
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

				
				Rect areaRect = GUILayoutUtility.GetRect(totalWidth + TableScrollbarWidth, visibleHeight);
				areaRect.width = totalWidth + TableScrollbarWidth;
				
				

				Rect contentRect = new Rect(0, 0, totalWidth, totalHeight);

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
				
				// Include the partially visible bottom row while retaining bounded virtualization.
				int firstVisible = Mathf.FloorToInt(scrollPos.y / ITEMS_TABLE_ROW_HEIGHT);
				int lastVisible = Mathf.Min(items.Count - 1, firstVisible + maxVisibleItems);

				
				for (int index = firstVisible; index <= lastVisible; index++)
				{
					Rect rowRect = new Rect(
						0,
						index * ITEMS_TABLE_ROW_HEIGHT,
						contentRect.width,
						ITEMS_TABLE_ROW_HEIGHT
					);

					DrawItemRow(items[index], index, rowRect, columnWidths, indentLevel);
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

		private void DrawItemRow(LocalContentManifestEntry entry, int index, Rect rowRect, float[] columnWidths, int indentLevel)
		{
			GUIStyle style = MultiSelectItemIds.Contains(entry.FullId)
				? _rowSelectedItemStyle
				: index % 2 == 0
					? _rowEvenItemStyle
					: _rowOddItemStyle;

			GUIStyle guiStyle = style ?? EditorStyles.toolbar;
			GUIStyle labelStyle = _itemLabelStyle ?? EditorStyles.label;

			var hitRect = GetItemRowRect(rowRect, columnWidths[0], indentLevel);
			bool isEditingName = entry.FullId == _editItemId;
			string nameLabel = isEditingName && _editLabels is {Length: > 0} ? _editLabels[0] : entry.Name;

			string lastUpdateDate = DateTimeOffset.FromUnixTimeMilliseconds(entry.LatestUpdateAtDate).ToLocalTime().ToString("g");

			bool isRenamed = _contentService.TryGetRenameInfo(entry.FullId, out var renameInfo)
			                 && renameInfo.CreatedFullId == entry.FullId;
			if (isRenamed)
				nameLabel = isEditingName ? nameLabel : $"{entry.Name} (renamed from {renameInfo.OldName})";

			bool isInvalid = _contentService.IsContentInvalid(entry.FullId);
			string[] values = {nameLabel, isRenamed && !isInvalid ? "Renamed" : ContentWindow_TooltipsHelper.GetStatusLabel(entry, isInvalid), entry.Tags != null ? string.Join(", ", entry.Tags) : "-", lastUpdateDate};
			GUIContent badge = _tooltipsHelper.GetContentStatusBadge(
				_contentService,
				entry,
				renamedFrom: isRenamed ? renameInfo.OldName : null);

			// Keep Inspector information icons; the table only emphasizes actionable differences.
			if (!isInvalid && !isRenamed && ContentWindow_TooltipsHelper.GetChangeKind(entry) == ContentChangeKind.UpToDate)
				badge.image = null;
			GUIContent[] icons = { null, badge };
			var statusStyle = isInvalid ? _invalidStatusStyle : entry.IsInConflict ? _conflictStatusStyle :
				ContentWindow_TooltipsHelper.IsIncoming(ContentWindow_TooltipsHelper.GetChangeKind(entry)) ? _incomingStatusStyle : labelStyle;

			bool[] isEditable = {isEditingName};
			string[] editableId = {entry.FullId};

		EditorGUI.BeginChangeCheck();
			if (isEditingName)
			{
				_editLabels = DrawTableRow(values, columnWidths, guiStyle, labelStyle, rowRect, icons, isEditable, editableId,
					ContentWindow_TooltipsHelper.CanReviewEntry(entry) ? entry : (LocalContentManifestEntry?)null, statusStyle, indentLevel);
				
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
				
				if (Event.current.type == EventType.MouseDown && !hitRect.Contains(Event.current.mousePosition))
				{
					RenameEntry(entry.FullId, _editLabels[0]);
				}
			}
			else
			{
				DrawTableRow(values, columnWidths, guiStyle, labelStyle, rowRect, icons, isEditable, editableId,
					ContentWindow_TooltipsHelper.CanReviewEntry(entry) ? entry : (LocalContentManifestEntry?)null, statusStyle, indentLevel);
			}

			_itemContentIdToRowRect[entry.FullId] = (int)rowRect.center.y;
			// Untyped entries are reviewed through their status link, without creating a fake Inspector object.
			if (!_contentTypeReflectionCache.ContentTypeToClass.ContainsKey(entry.TypeName))
			{
				RegisterButtonTooltip(hitRect, $"No Unity content class for {entry.TypeName}. Click its status to reconcile incoming changes or conflicts.");
				return;
			}
			
			if (CanHandleContentInput && Event.current.type == EventType.MouseDown && hitRect.Contains(Event.current.mousePosition))
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

		private bool CanHandleContentInput => !_contentService.CurrentProgress.IsActive;

		// Menus can outlive the GUI event that opened them. Check at invocation, not registration.
		private GenericMenu.MenuFunction GuardContentInput(GenericMenu.MenuFunction action) => () =>
		{
			if (CanHandleContentInput) action();
		};

		// A modal confirmation can pump Editor callbacks that start another operation.
		private bool ConfirmContentInput(Func<bool> confirm) =>
			CanHandleContentInput && confirm() && CanHandleContentInput;

		private void RenameEntry(string entryId, string newName)
		{
			if (!CanHandleContentInput) return;
			_editItemId = string.Empty;
			var newId = _contentService.RenameContent(entryId, newName);
			SetEntryIdAsSelected(newId);
			Repaint();
		}

		private void ShowItemOptionsMenu(Vector2 screenPosition)
		{
			if (!CanHandleContentInput) return;
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
					menu.AddItem(new GUIContent("Duplicate Item"), false, GuardContentInput(() =>
					{
						DuplicateContent(entry);
					}));

					menu.AddItem(new GUIContent("Rename Item"), false, GuardContentInput(() =>
					{
						StartEditEntryName(entry);
					}));


					menu.AddItem(new GUIContent("Delete Item"), false, GuardContentInput(() =>
					{
						if (ConfirmContentInput(() => EditorUtility.DisplayDialog("Delete Content",
						                                "Are you sure you want to delete this content?", "Delete",
						                                "Cancel")))
						{
							var scope = _contentService.GetContentScopeKey();
							_contentService.DeleteContent(entry.FullId).Then(_ =>
							{
								if (scope != _contentService.GetContentScopeKey()) return;
								if (entry.StatusEnum is ContentStatus.Created) ClearSelection();
								else SetEntryIdAsSelected(entry.FullId);
								Repaint();
							}).Error(Debug.LogException);
						}

					}));
				}

				if (_contentService.TryGetRenameInfo(entry.FullId, out var ctxRenameInfo)
				    && ctxRenameInfo.CreatedFullId == entry.FullId)
				{
					menu.AddItem(new GUIContent($"Revert Item (renamed from {ctxRenameInfo.OldName})"), false,
					             GuardContentInput(() =>
					             {
						             var bothIds = $"{ctxRenameInfo.CreatedFullId},{ctxRenameInfo.DeletedFullId}";
						             _ = _contentService.SyncContentsWithProgress(
							             true, true, true, true, bothIds, ContentFilterType.ExactIds);
					             }));
				}
				else if (entry.StatusEnum is not ContentStatus.UpToDate and not ContentStatus.Created)
				{
					menu.AddItem(new GUIContent("Revert Item"), false,
					             GuardContentInput(() =>
					             {
						             _ = _contentService.SyncContentsWithProgress(
							             true, true, true, true, entry.FullId, ContentFilterType.ExactIds);
					             }));
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
					menu.AddItem(new GUIContent("Delete Existing Items"), false, GuardContentInput(() =>
					{
						if (ConfirmContentInput(() => EditorUtility.DisplayDialog("Delete Content",
						                                $"Are you sure you want to delete these {entries.Count} contents?", "Delete",
						                                "Cancel")))
						{
							_contentService.TempDisableWatcher(() => DeleteContentEntries(entries)).Error(Debug.LogException);
						}
					}));
				}

				menu.AddItem(new GUIContent("Duplicate Items"), false, GuardContentInput(() =>
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

				}));

				if (allUpToDate)
				{
					menu.AddDisabledItem(new GUIContent("Revert Items"));
				}
				else
				{
					menu.AddItem(new GUIContent("Revert changed Items"), false, GuardContentInput(() =>
					{
						_ = _contentService.SyncContentsWithProgress(
							true, true, true, true, string.Join(",",entries.Select(e => e.FullId)), ContentFilterType.ExactIds);
					}));
				}
				
			}

			menu.DropDown(new Rect(GUIUtility.ScreenToGUIPoint(screenPosition), Vector2.zero));
		}

		private void DuplicateContent(LocalContentManifestEntry entry)
		{
			if (!CanHandleContentInput) return;
			if (_contentService.DuplicateContent(entry, out var duplicatedObject))
			{
				SetEntryIdAsSelected(duplicatedObject.Id);
			}
			
		}

		private void StartEditEntryName(LocalContentManifestEntry entry)
		{
			if (!CanHandleContentInput) return;
			_editItemId = entry.FullId;
			_editLabels = new[] {entry.Name};
		}

		private string[] DrawTableRow(string[] labels,
		                              float[] columnWidths,
		                              GUIStyle rowStyle,
		                              GUIStyle fieldStyle,
		                              Rect fullRect,
		                              GUIContent[] icons = null, bool[] isEditLabel = null, string[] fieldID = null, LocalContentManifestEntry? reviewEntry = null, GUIStyle statusStyle = null, int indentLevel = -1)
		{
			if (Event.current.type == EventType.Repaint)
			{
				rowStyle.Draw(indentLevel < 0 ? fullRect : GetItemRowRect(fullRect, columnWidths[0], indentLevel), false, false, false, false);
			}
			
			for (int i = 0; i < labels.Length; i++)
			{
				string labelValue = labels[i];
				var cellRect = new Rect(fullRect.x, fullRect.y, columnWidths[i], fullRect.height);
				var nameRect = i == 0 && indentLevel >= 0 ? GetItemNameRect(cellRect, indentLevel) : GetTableLabelRect(cellRect, i);
				var badge = icons != null && icons.Length > i ? icons[i] : null;
				if (badge?.image != null)
				{
					var iconRect = new Rect(cellRect.x + BASE_PADDING, cellRect.center.y - 7.5f, 15, 15);
					GUI.DrawTexture(iconRect, badge.image, ScaleMode.ScaleToFit);
					GUI.Label(iconRect, new GUIContent(string.Empty, badge.tooltip), GUIStyle.none);
				}
				
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
				else if (i == STATUS_COLUMN && reviewEntry.HasValue)
				{
					EditorGUIUtility.AddCursorRect(nameRect, MouseCursor.Link);
					if (GUI.Button(nameRect, new GUIContent(labelValue + "  \u203A", badge?.tooltip + "\nClick to review this item and choose Use Realm or Keep Local."), statusStyle ?? fieldStyle))
					{
						var entry = reviewEntry.Value;
						var scope = _contentService.GetContentScopeKey();
						AddDelayedAction(() =>
						{
							if (CanHandleContentInput && scope == _contentService.GetContentScopeKey())
								ContentWindow_TooltipsHelper.ShowReconciliationDialog(_contentService, entry);
						});
					}
				}
				else
				{
					EditorGUI.LabelField(nameRect, new GUIContent(labelValue, badge?.tooltip ?? labelValue), i == STATUS_COLUMN ? statusStyle ?? fieldStyle : fieldStyle);
				}

				fullRect.x += columnWidths[i];
			}

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
			
			var filterKey = BuildFilterCacheKey(specificType, nameSearchPartValue, types, tags, statuses);

			if (!_filteredCache.TryGetValue(filterKey, out var filteredItems))
			{
				var allItems = GetCachedManifestEntries();
				filteredItems = allItems.Where(entry => FilterItem(specificType, types, entry, tags, statuses, nameSearchPartValue)).ToList();
				_filteredCache[filterKey] = filteredItems;
			}

			List<LocalContentManifestEntry> contentManifestEntries = shouldSort ? SortItems(filterKey, filteredItems, _currentSortOption) : filteredItems;
			return contentManifestEntries;
		}
		private static string BuildFilterCacheKey(string specificType, string name,
			IEnumerable<string> types, IEnumerable<string> tags, IEnumerable<string> statuses)
		{
			// Length prefixes keep values containing separators distinct; sorting makes set order irrelevant.
			string Encode(string value) => $"{value.Length}:{value}";
			string EncodeSet(IEnumerable<string> values) =>
				Encode(string.Concat(values.OrderBy(value => value, StringComparer.Ordinal).Select(Encode)));
			return Encode(specificType) + Encode(name) + EncodeSet(types) + EncodeSet(tags) + EncodeSet(statuses);
		}

		private bool HasContentIssue(LocalContentManifestEntry entry)
		{
			return _contentService.IsContentInvalid(entry.FullId) || entry.IsInConflict ||
			       ContentWindow_TooltipsHelper.NeedsReconciliation(entry);
		}

		private bool FilterItem(string specificType,
		                        HashSet<string> types,
		                        LocalContentManifestEntry entry,
		                        HashSet<string> tags,
		                        HashSet<string> statuses,
		                        string nameSearchPartValue)
		{
			// Hide the Deleted side of a rename unless Issues needs to show a problem on that entry.
			if (entry.StatusEnum == ContentStatus.Deleted
			    && _contentService.TryGetRenameInfo(entry.FullId, out var ri)
			    && ri.DeletedFullId == entry.FullId
			    && !(statuses.Contains(StatusMapToString[ContentFilterStatus.Issues]) && HasContentIssue(entry))
			    && !ContentWindow_TooltipsHelper.NeedsReconciliation(entry))
			{
				return false;
			}

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
				if (status == StatusMapToString[ContentFilterStatus.Issues])
				{
					return HasContentIssue(entry);
				}

				if (status == StatusMapToString[ContentFilterStatus.Invalid])
				{
					return _contentService.IsContentInvalid(entry.FullId);
				}

				if (status == StatusMapToString[ContentFilterStatus.Conflicted])
				{
					return entry.IsInConflict;
				}

				return FilterStatusToContentStatus.TryGetValue(status, out var contentStatus) && contentStatus == entry.StatusEnum;
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
		
		private static Rect GetItemRowRect(Rect row, float nameColumnWidth, int indentLevel)
		{
			row.xMin += Mathf.Min((indentLevel + 1) * INDENT_WIDTH, nameColumnWidth - BASE_PADDING);
			return row;
		}

		private static Rect GetItemNameRect(Rect cell, int indentLevel)
		{
			var label = GetTableLabelRect(cell, 0);
			// Match the type heading's foldout/icon gutter, then indent its children one level.
			float inset = FOLDOUT_WIDTH + TYPE_ICON_SIZE + BASE_PADDING + (indentLevel + 1) * INDENT_WIDTH;
			label.xMin = Mathf.Min(label.xMin + inset, label.xMax);
			return label;
		}

		private static Rect GetTableLabelRect(Rect cell, int column)
		{
			// Reserve the icon slot even for healthy rows and the shared header.
			cell.xMin += BASE_PADDING + (column == STATUS_COLUMN ? 20 : 0);
			cell.xMax = Mathf.Max(cell.xMin, cell.xMax - BASE_PADDING);
			return cell;
		}

		private static float[] CalculateColumnWidths(float totalAvailableWidth)
		{
			// Name, Status, Tags, Latest Update. Widths include padding and the status icon slot.
			float[] minWidths = {200f, 160f, 70f, 170f};
			float[] weights = {0.5f, 0.2f, 0.1f, 0.2f};
			float scale = Mathf.Clamp01(totalAvailableWidth / ITEM_LIST_MIN_WIDTH);
			float remaining = Mathf.Max(0, totalAvailableWidth - ITEM_LIST_MIN_WIDTH);
			for (int i = 0; i < minWidths.Length; i++)
				minWidths[i] = minWidths[i] * scale + remaining * weights[i];
			return minWidths;
		}
	}
}
