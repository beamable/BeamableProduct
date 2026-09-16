using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.ContentService;
using Beamable.Editor.Util;
using Beamable.Editor.UI2.Utils;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Util;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const int HEADER_BUTTON_WIDTH = 50;
		private const string CONTENT_SEARCH_CONTROL = "BeamContentSearch";
		private Rect _contentSearchScreenRect;
		private bool _focusContentSearch;
		private const string REVERT_ALL_MENU_ITEM = "Revert All Local Changes (Modified, Created, Deleted, and Conflicted)";
		private const string REVERT_MODIFIED_MENU_ITEM = "Revert Modified Local Changes";
		private const string REVERT_CONFLICTED_MENU_ITEM = "Revert Conflicted Changes Only";
		private const string REVERT_DELETED_MENU_ITEM = "Revert Deleted Contents Only";
		private const string REVERT_NEW_CONTENTS_MENU_ITEM = "Delete All New Created Changes";
		
		
		private readonly Dictionary<ContentSearchFilterType, HashSet<string>> _activeFilters = new();
		private List<string> _allTypes = new();
		private List<string> _allTags = new();
		
		private ContentSortOptionType _currentSortOption;
		private GUIStyle _lowBarTextStyle;
		private GUIStyle _lowBarDropdownStyle;
		private GUIStyle _clearFiltersButtonStyle;
		
		private List<string> _oldItemsSelected;
		
		private static List<string> AllStatus => StatusMapToString.Values.ToList();

		private static readonly Dictionary<ContentSearchFilterType, string> ContentFilterTypeToQueryTag = new()
		{
			{ContentSearchFilterType.Tag, "tag:"},
			{ContentSearchFilterType.Status, "status:"},
			{ContentSearchFilterType.Type, "type:"}
		};
		
		private static readonly Dictionary<ContentFilterStatus, string> StatusMapToString = new()
		{
			{ContentFilterStatus.Invalid, "invalid"},
			{ContentFilterStatus.Created, "created"},
			{ContentFilterStatus.Deleted, "deleted"},
			{ContentFilterStatus.Modified, "modified"},
			{ContentFilterStatus.UpToDate, "upToDate"},
			{ContentFilterStatus.Conflicted, "conflicted"},
			{ContentFilterStatus.Issues, "issues"}
		};

		private static readonly Dictionary<string, ContentStatus> FilterStatusToContentStatus = new()
		{
			{StatusMapToString[ContentFilterStatus.Created], ContentStatus.Created},
			{StatusMapToString[ContentFilterStatus.Deleted], ContentStatus.Deleted},
			{StatusMapToString[ContentFilterStatus.Modified], ContentStatus.Modified},
			{StatusMapToString[ContentFilterStatus.UpToDate], ContentStatus.UpToDate},
		};

		private static readonly Dictionary<ContentSortOptionType, string> SortTypeNameMap = new()
		{
			{ContentSortOptionType.IdAscending, "ID (A-Z)"},
			{ContentSortOptionType.IdDescending, "ID (Z-A)"},
			{ContentSortOptionType.TypeAscending, "Type (A-Z)"},
			{ContentSortOptionType.TypeDescending, "Type (Z-A)"},
			{ContentSortOptionType.Status, "Status"},
			{ContentSortOptionType.ValidStatus, "Validation Status"}
		};
		
		

		private void BuildHeaderFilters()
		{
			_contentSearchData = new SearchData() {onEndCheck = OnTextChange};
			
			_allTypes = _contentTypeReflectionCache.GetAll()
			                                       .OrderBy(pair => pair.Name)
			                                       .Select(pair => pair.Name)
			                                       .ToList();
			
			_allTags = _contentService.TagsCache;
		}

		private void BuildHeaderStyles()
		{
			_lowBarTextStyle = new GUIStyle(EditorStyles.boldLabel) {alignment = TextAnchor.MiddleLeft};
			_clearFiltersButtonStyle = new GUIStyle(EditorStyles.miniButton)
			{
				fixedHeight = EditorGUIUtility.singleLineHeight + 6f,
				alignment = TextAnchor.MiddleCenter,
				margin = new RectOffset(EditorStyles.miniButton.margin.left, EditorStyles.miniButton.margin.right, 0, 0),
				padding = new RectOffset(EditorStyles.miniButton.padding.left, EditorStyles.miniButton.padding.right, 3, 3)
			};

			if (_lowBarDropdownStyle == null || _lowBarDropdownStyle.normal.background == null)
			{
				_lowBarDropdownStyle = new GUIStyle(EditorStyles.toolbarDropDown)
				{
					normal = {background = BeamGUI.CreateColorTexture(new Color(0.35f, 0.35f, 0.35f))},
					alignment = TextAnchor.MiddleRight,
					margin = new RectOffset(0, 15, 5, 0),
				};
			}
		}

		private void DrawHeader()
		{
			BeamGUI.ShowDisabled(NeedsMigration == false, () =>
			{
				BeamGUI.DrawHeaderSection(this, ActiveContext, DrawTopBarHeader, DrawLowBarHeader, () =>
				{
					
					Application.OpenURL(DocsPageHelper.GetUnityDocsPageUrl("unity/user-reference/beamable-services/profile-storage/content/content-unity/", EditorConstants.UNITY_CURRENT_DOCS_VERSION));
				}, () => _ = _contentService.Reload(), (refreshRect, helpRect) =>
				{
					RegisterButtonTooltip(refreshRect, "Refresh content status");
					RegisterButtonTooltip(helpRect, "Open content documentation");
				});
			});
			
		}

		private void DrawTopBarHeader()
		{
			if (_windowStatus != ContentWindowStatus.Normal)
			{
				if (BeamGUI.HeaderButton("Content Editor", BeamGUI.iconContentEditorIcon, width: 90, iconPadding: 2))
				{
					ChangeWindowStatusDelayed(ContentWindowStatus.Normal);
				}
			}
			if (_windowStatus is ContentWindowStatus.Normal || _windowStatus is ContentWindowStatus.Validate)
			{
				var hasContentToPublish = _contentService.HasChangedContents;
				var hasConflictedOrInvalid = _contentService.HasConflictedContent || _contentService.HasInvalidContent;

				string validateTooltip = "Validate Local Changes";
				string syncTooltip = "Sync contents with Current Realm";

				if (!hasContentToPublish && !hasConflictedOrInvalid)
				{
					syncTooltip = "There are no local changes or issues to sync.";
				}

				if (_windowStatus != ContentWindowStatus.Validate)
				{
					
					if (DrawHeaderButtonWithTooltip("Validate", BeamGUI.iconCheck, validateTooltip))
					{
						ChangeToValidateMode();
					}
				}

				if (BeamGUI.ShowDisabled(hasContentToPublish || hasConflictedOrInvalid,
				                         () => DrawHeaderButtonWithTooltip("Sync", BeamGUI.iconSync, syncTooltip)))
				{
					ShowSyncMenu();
				}

				DrawPublishHeaderButton(hasContentToPublish, hasConflictedOrInvalid);

				if (BeamGUI.HeaderButton("Snapshot", BeamGUI.iconContentSnapshotWhite, width: HEADER_BUTTON_WIDTH, iconPadding: 2,
				                         tooltip: "Manages content snapshots"))
				{
					ChangeToSnapshotManager();
				}

				if (BeamGUI.HeaderButton("History", BeamGUI.iconRefresh, width: HEADER_BUTTON_WIDTH, iconPadding: 2,
				                         tooltip: "View published content history"))
				{
					ChangeToHistory();
				}

				if (_windowStatus != ContentWindowStatus.Validate)
				{
					EditorGUILayout.Space(5, false);
					// Target 480px (50% wider than the previous ~320px field), shrinking on narrow windows.
					float reservedToolbarWidth = 5 * HEADER_BUTTON_WIDTH + 3 * 30 + 60 + 40 +
					                             _clearFiltersButtonStyle.CalcSize(new GUIContent("Clear all filters")).x;
					float searchWidth = Mathf.Clamp(position.width - reservedToolbarWidth, 30f, 480f);
					GUILayout.BeginVertical(GUILayout.Width(searchWidth));
					this.DrawSearchBar(_contentSearchData, true, CONTENT_SEARCH_CONTROL, OnContentSearchFieldDrawn);
					GUILayout.EndVertical();
					DrawFilterButton(ContentSearchFilterType.Tag, BeamGUI.iconTag, _allTags);
					DrawFilterButton(ContentSearchFilterType.Type, BeamGUI.iconType, _allTypes);
					DrawFilterButton(ContentSearchFilterType.Status, BeamGUI.iconStatus, AllStatus);
					GUILayout.BeginVertical(GUILayout.ExpandWidth(false));
					GUILayout.FlexibleSpace();
					using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_contentSearchData.searchText) &&
					                                  !_activeFilters.Values.Any(values => values.Count > 0)))
					{
						if (GUILayout.Button("Clear all filters", _clearFiltersButtonStyle,
							GUILayout.ExpandWidth(false)))
							ClearAllContentFilters();
					}
					RegisterButtonTooltip(GUILayoutUtility.GetLastRect(), "Clear the name search and all type, tag, and status filters.");
					GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
				}
			}
		}
		private void HandleContentSearchInput()
		{
			var evt = Event.current;
			bool searchFocused = GUI.GetNameOfFocusedControl() == CONTENT_SEARCH_CONTROL;
			if (_windowStatus != ContentWindowStatus.Normal || NeedsMigration)
			{
				_focusContentSearch = false;
				if (searchFocused) UnfocusContentSearch();
				return;
			}

			// Run before toolbar buttons or list rows can consume the click.
			if (searchFocused && evt.type == EventType.MouseDown &&
			    !_contentSearchScreenRect.Contains(GUIUtility.GUIToScreenPoint(evt.mousePosition)))
			{
				UnfocusContentSearch();
				// Leave the click available to the control under the pointer.
			}

			if (focusedWindow != this || evt.type != EventType.KeyDown) return;
			if (searchFocused && evt.keyCode == KeyCode.Escape)
			{
				UnfocusContentSearch();
				evt.Use();
			}
			else if (!searchFocused && !EditorGUIUtility.editingTextField && GUIUtility.hotControl == 0 &&
			         !evt.control && !evt.command && !evt.alt && !evt.shift &&
			         (evt.character == '/' || evt.keyCode == KeyCode.Slash) && _contentSearchScreenRect.width > 30f)
			{
				_focusContentSearch = true;
				evt.Use();
				Repaint();
			}
		}

		private void OnContentSearchFieldDrawn(Rect rect)
		{
			if (Event.current.type == EventType.Repaint)
				_contentSearchScreenRect = GUIUtility.GUIToScreenRect(rect);

			// Used and Layout events return placeholder rectangles. Wait until the field
			// has been drawn with its real bounds before applying the queued shortcut.
			if (_focusContentSearch && Event.current.type == EventType.Repaint)
			{
				_focusContentSearch = false;
				if (rect.width > 30f && GUI.enabled && focusedWindow == this)
					EditorGUI.FocusTextInControl(CONTENT_SEARCH_CONTROL);
				Repaint();
			}
		}

		private void UnfocusContentSearch()
		{
			GUI.FocusControl(null);
			EditorGUIUtility.editingTextField = false;
			_focusContentSearch = false;
			Repaint();
		}

		/// <summary>
		/// Draws Publish with an Issues badge when content problems block publishing.
		/// Clicking reviews issues or opens the publish panel, depending on eligibility.
		/// </summary>
		private void DrawPublishHeaderButton(bool hasContentToPublish, bool hasConflictedOrInvalid)
		{
			bool hasPublishPermission = _cli.Permissions.CanPushContent;
			bool canReviewIssues = hasConflictedOrInvalid;
			bool canPublish = hasPublishPermission && hasContentToPublish && !canReviewIssues;

			string tooltip;

			if (canReviewIssues)
			{
				int issueCount = _contentService.EntriesCache.Values
				                                .Count(HasContentIssue);

				string summary = issueCount > 0
					? $"{issueCount} {(issueCount == 1 ? "item needs" : "items need")} attention."
					: "Content has validation errors or conflicts.";

				tooltip = $"Publishing blocked: {summary}\n" + "Click to review validation errors and conflicts.";

				if (!hasPublishPermission)
				{
					tooltip += "\nYou also do not have permission to publish to this realm.";
				}
			}
			else if (!hasPublishPermission)
			{
				tooltip = "You do not have permission to publish to this realm.";
			}
			else if (!hasContentToPublish)
			{
				tooltip = "No local changes to publish.";
			}
			else
			{
				tooltip = "Publish content to the current realm.";
			}

			bool clicked = BeamGUI.ShowDisabled(canPublish || canReviewIssues,
			                                    () => DrawHeaderButtonWithTooltip("Publish",
				                                    BeamGUI.iconPublish,
				                                    tooltip,
				                                    badge: canReviewIssues ? BeamGUI.iconStatusInvalid : null));

			if (!clicked) return;

			if (canReviewIssues) ShowContentIssues();
			else if (canPublish) ChangeToPublishMode();
		}

		/// <summary>
		/// Opens the content list filtered to items with validation errors or conflicts.
		/// Clears existing filters, search text, cached results, and the active tooltip.
		/// Defers the changes until the current UI draw completes.
		/// </summary>
		private void ShowContentIssues()
		{
			AddDelayedAction(() =>
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);

				_activeFilters.Clear();
				_contentSearchData.searchText = string.Empty;
				GUI.FocusControl(null);

				GetFilterTypeActiveItems(ContentSearchFilterType.Status)
					.Add(StatusMapToString[ContentFilterStatus.Issues]);

				ClearCaches();
				UpdateActiveFilterSearchText();
				ResetButtonTooltip();
			});
		}

		private void ChangeToPublishMode()
		{
			AddDelayedAction(() =>
			{
				ChangeWindowStatus(ContentWindowStatus.Publish);
				_statusToDraw = ContentStatus.Modified | ContentStatus.Created | ContentStatus.Deleted;
			});
		}
		
		private void ChangeToValidateMode()
		{
			AddDelayedAction(() =>
			{
				ChangeWindowStatus(ContentWindowStatus.Validate);
				_statusToDraw = ContentStatus.Invalid;
			});
		}
		
		private void ChangeToRevertAll()
		{
			ChangeWindowStatus(ContentWindowStatus.Revert);
			_statusToDraw = ContentStatus.Modified | ContentStatus.Created | ContentStatus.Deleted;
			_revertAction = RevertAllContents;
		}
		
		private void ChangeToSnapshotManager()
		{
			AddDelayedAction(() =>
			{
				_ = CacheSnapshots();
				ChangeWindowStatus(ContentWindowStatus.SnapshotManager);
			});
		}

		private void ChangeToHistory()
		{
			AddDelayedAction(() => ChangeWindowStatus(ContentWindowStatus.History));
		}

		private void ChangeWindowStatus(ContentWindowStatus windowStatus, bool shouldRepaint = true)
		{
			if(_windowStatus == windowStatus)
				return;
			
			var previousWindowStatus = _windowStatus;
			_windowStatus = windowStatus;
			if (previousWindowStatus == ContentWindowStatus.History && _windowStatus != ContentWindowStatus.History)
			{
				ResetHistorySelection();
			}
			if (_windowStatus == ContentWindowStatus.History)
			{
				ResetHistorySelection();
				_contentService?.StartContentHistory();
			}
			else
			{
				_contentService?.StopContentHistory();
			}
			if (_windowStatus is ContentWindowStatus.Normal)
			{
				var selection = new List<Object> { };
				if (_oldItemsSelected != null)
				{
					foreach (var id in _oldItemsSelected)
					{
						if (_contentService.TryGetContentObject(id, out var value))
						{
							selection.Add(value);
						}
					}
					Selection.objects = selection.ToArray();
				}

				_oldItemsSelected = new List<string>();
			}
			else
			{
				var items = MultiSelectItemIds;
				if (items?.Count > 0)
				{
					_oldItemsSelected = items.ToList();
					Selection.activeObject = null;
				}
			}

			if(shouldRepaint)
				Repaint();
		}

		/// <summary>
		/// Defers Content Manager state changes until the current IMGUI event has finished.
		/// </summary>
		/// <remarks>
		/// Some state transitions remove or add controls. Deferring them avoids mismatched layout groups during repaint.
		/// </remarks>
		private void ChangeWindowStatusDelayed(ContentWindowStatus windowStatus)
		{
			EditorApplication.delayCall += () => ChangeWindowStatus(windowStatus);
		}

		private void DrawLowBarHeader(Rect rect)
		{
			if (NeedsMigration) return;
			
			if (_windowStatus is not ContentWindowStatus.Normal)
			{
				GUILayout.Space(40);
				return;
			}
				
			
			GUILayout.Space(15);

			GUIStyle lowBarTextStyle = _lowBarTextStyle ?? EditorStyles.boldLabel;
			
			int filteredItemsCount = GetFilteredItems().Count;
			int totalItems = GetCachedManifestEntries().Count;
			
			var itemsCounts = new GUIContent($"{filteredItemsCount}/{totalItems}");
			var itemsCountsSize = lowBarTextStyle.CalcSize(itemsCounts);

			var itemsFilterLabelRect = new Rect(rect.x + 4, rect.y, itemsCountsSize.x, rect.height);
			var contentTreeLabelRect = new Rect(itemsFilterLabelRect.xMax + 2, rect.y, 350, rect.height);

			string contentTreeLabelValue = "All Content";
			contentTreeLabelValue += SelectedContentType.Count == 0
				? ""
				: $" > {string.Join(" | ", SelectedContentType.OrderBy(item => item).Select(item => item.Replace(".", ">")))}";
			
			GUI.Label(itemsFilterLabelRect, $"{filteredItemsCount}/{totalItems}", lowBarTextStyle);
			GUI.Label(contentTreeLabelRect, contentTreeLabelValue, lowBarTextStyle);
			EditorGUILayout.Space(1, true);
			GUIContent dropdownContent = new GUIContent($"{SortTypeNameMap[_currentSortOption]}"); // ▼


			if (_contentService?.availableManifestIds?.Count > 1)
			{
				var manifestId = _contentService.manifestIdOverride;
				if (string.IsNullOrEmpty(manifestId))
				{
					manifestId =  "global";
				}

				if (BeamGUI.LayoutDropDownButton(new GUIContent(manifestId), tooltip: "Content Namespace"))
				{
					var menu = new GenericMenu();

					for (var i = 0; i < _contentService.availableManifestIds.Count; i++)
					{
						var id = _contentService.availableManifestIds[i];
						var enabled = id == manifestId;
						menu.AddItem(new GUIContent(id), enabled, () =>
						{
							_contentService.SetManifestId(id);
							GUI.changed = true;
						});
					}
					menu.ShowAsContext();
				}
			}
			

			EditorGUILayout.Space(6, false);
			
			if (BeamGUI.LayoutDropDownButton(dropdownContent))
			{
				GenericMenu menu = new GenericMenu();
				foreach ((ContentSortOptionType type, string stringValue) in SortTypeNameMap)
				{
					menu.AddItem(new GUIContent(stringValue), _currentSortOption == type, () =>
					{
						_currentSortOption = type;
						Repaint();
					});
				}
				menu.ShowAsContext();
			}

			EditorGUILayout.Space(4, false);

		}
		
		private void ShowSyncMenu()
		{
			bool hasModified = _contentService.GetAllContentFromStatus(ContentStatus.Modified).Count > 0;
			bool hasNewItems = _contentService.GetAllContentFromStatus(ContentStatus.Created).Count > 0;
			bool hasDeleted = _contentService.GetAllContentFromStatus(ContentStatus.Deleted).Count > 0;
			bool hasConflictedItems = _contentService.HasConflictedContent;
			
			GenericMenu menu = new GenericMenu();
			if (hasModified || hasNewItems || hasConflictedItems || hasDeleted)
			{
				menu.AddItem(new GUIContent(REVERT_ALL_MENU_ITEM), false, ChangeToRevertAll);
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_ALL_MENU_ITEM), false);
			}

			if (hasModified)
			{
				menu.AddItem(new GUIContent(REVERT_MODIFIED_MENU_ITEM), false, () =>
				{
					ChangeWindowStatus(ContentWindowStatus.Revert);
					_statusToDraw = ContentStatus.Modified;
					_revertAction = RevertModifiedContents;
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_MODIFIED_MENU_ITEM), false);
			}

			if (hasConflictedItems)
			{
				menu.AddItem(new GUIContent(REVERT_CONFLICTED_MENU_ITEM), false, () =>
				{
					ChangeWindowStatus(ContentWindowStatus.Revert);
					_statusToDraw = ContentStatus.Modified | ContentStatus.Created | ContentStatus.Deleted;
					_revertAction = RevertConflictedContents;
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_CONFLICTED_MENU_ITEM), false);
			}
			
			if (hasDeleted)
			{
				menu.AddItem(new GUIContent(REVERT_DELETED_MENU_ITEM), false, () =>
				{
					ChangeWindowStatus(ContentWindowStatus.Revert);
					_statusToDraw = ContentStatus.Deleted;
					_revertAction = RevertDeletedContents;
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_DELETED_MENU_ITEM), false);
			}

			if (hasNewItems)
			{
				menu.AddItem(new GUIContent(REVERT_NEW_CONTENTS_MENU_ITEM), false, () =>
				{
					ChangeWindowStatus(ContentWindowStatus.Revert);
					_statusToDraw = ContentStatus.Created;
					_revertAction = RevertAllNewContents;
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_NEW_CONTENTS_MENU_ITEM), false);
			}

			menu.ShowAsContext();
		}

		private async Promise RevertAllContents()
		{
			await _contentService.SyncContentsWithProgress(true, true, true, true, showUnityModalProgress: false);
		}

		private async Promise RevertModifiedContents()
		{
			// Capture renames before the first sync clears the registry via Reload()
			var renames = _contentService.GetAllRenames();

			await _contentService.SyncContentsWithProgress(true, false, false, false, showUnityModalProgress: false);

			if (renames.Count > 0)
			{
				var renameIds = string.Join(",",
					renames.SelectMany(r => new[] { r.CreatedFullId, r.DeletedFullId }));
				await _contentService.SyncContentsWithProgress(
					true, true, true, true, renameIds, ContentFilterType.ExactIds);
			}
		}

		private async Promise RevertConflictedContents()
		{
			await _contentService.SyncContentsWithProgress(false, false, true, false, showUnityModalProgress: false);
		}

		private async Promise RevertDeletedContents()
		{
			await _contentService.SyncContentsWithProgress(false, false, false, true, showUnityModalProgress: false);
		}

		private async Promise RevertAllNewContents()
		{
			await _contentService.SyncContentsWithProgress(false, true, false, false, showUnityModalProgress: false);
		}

		


		private void DrawFilterButton(ContentSearchFilterType searchFilterType, Texture icon, IEnumerable<string> items)
		{
			bool hasActiveFilter = _activeFilters.TryGetValue(searchFilterType, out var activeItems) && activeItems.Count > 0;
			Color backgroundColor = hasActiveFilter ? Color.gray : default;
			bool isClicked = BeamGUI.HeaderButton(null, icon,
			                                      width: 30,
			                                      padding: 4,
			                                      iconPadding: -5,
			                                      drawBorder: true,
			                                      backgroundColor: backgroundColor);
			Rect buttonRect = GUILayoutUtility.GetLastRect();
			string tooltip = searchFilterType switch
			{
				ContentSearchFilterType.Tag => "Filter by tag",
				ContentSearchFilterType.Type => "Filter by content type",
				ContentSearchFilterType.Status => "Filter by status",
				_ => "Filter content"
			};
			RegisterButtonTooltip(buttonRect, tooltip);
			if (!isClicked)
			{
				return;
			}

			var activeItemsOnFilter = GetFilterTypeActiveItems(searchFilterType);
			var itemStatus = items.ToDictionary(item => item, s => activeItemsOnFilter.Contains(s));
			ToggleListWindow.Show(buttonRect, new Vector2(200, 250), itemStatus, (item, state) =>
			{
				// Resolve the current set: typing or clearing the query can replace it while the popup exists.
				var currentItems = GetFilterTypeActiveItems(searchFilterType);
				if (state)
				{
					currentItems.Add(item);
				}
				else
				{
					currentItems.Remove(item);
				}

				UpdateActiveFilterSearchText();
			});
		}

		private void OnTextChange()
		{
			_activeFilters.Clear();
			foreach (var part in (_contentSearchData.searchText ?? string.Empty).Split(','))
			{
				if (!TryGetSearchFilter(part, out var type, out var value))
					continue;

				var items = GetFilterTypeActiveItems(type);
				foreach (var item in value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
				{
					// Preserve unfinished/unknown statuses as filters, but normalize known names for the menus.
					var normalized = type == ContentSearchFilterType.Status
						? StatusMapToString.Values.FirstOrDefault(status => string.Equals(status, item, StringComparison.OrdinalIgnoreCase)) ?? item
						: item;
					items.Add(normalized);
				}
			}
			ClearCaches();
			Repaint();
		}

		private void UpdateActiveFilterSearchText()
		{
			var parts = new List<string>();
			var name = GetNameSearchPartValue();
			if (!string.IsNullOrEmpty(name))
				parts.Add(name);
			foreach (var pair in ContentFilterTypeToQueryTag)
			{
				if (_activeFilters.TryGetValue(pair.Key, out var values) && values.Count > 0)
					parts.Add($"{pair.Value} {string.Join(" ", values.OrderBy(value => value, StringComparer.Ordinal))}");
			}

			// Release the focused text editor before changing its text from a tree/menu/button action.
			GUI.FocusControl(null);
			_contentSearchData.searchText = string.Join(", ", parts);
			ClearCaches();
			Repaint();
		}

		private void ClearAllContentFilters()
		{
			GUI.FocusControl(null);
			_contentSearchData.searchText = string.Empty;
			OnTextChange();
			ResetButtonTooltip();
		}

		private static bool TryGetSearchFilter(string part, out ContentSearchFilterType type, out string value)
		{
			part = part.Trim();
			foreach (var pair in ContentFilterTypeToQueryTag)
			{
				if (!part.StartsWith(pair.Value, StringComparison.OrdinalIgnoreCase))
					continue;
				type = pair.Key;
				value = part.Substring(pair.Value.Length).Trim();
				return true;
			}
			type = default;
			value = string.Empty;
			return false;
		}

		private HashSet<string> GetFilterTypeActiveItems(ContentSearchFilterType type)
		{
			if (!_activeFilters.TryGetValue(type, out HashSet<string> items))
			{
				_activeFilters[type] = items = new HashSet<string>();
			}

			return items;
		}

		private string GetNameSearchPartValue()
		{
			return string.Join(", ", (_contentSearchData?.searchText ?? string.Empty).Split(',')
				.Where(part => !TryGetSearchFilter(part, out _, out _))
				.Select(part => part.Trim()).Where(part => part.Length > 0));
		}
		
	}
}
