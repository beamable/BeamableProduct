using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Util;
using Beamable.Editor.UI2.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Util;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const int HEADER_BUTTON_WIDTH = 50;
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
			{ContentFilterStatus.Conflicted, "conflicted"}
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
				}, () => _ = _contentService.Reload());
			});
			
		}

		private void DrawTopBarHeader()
		{
			if (_windowStatus != ContentWindowStatus.Normal)
			{
				if (BeamGUI.HeaderButton("Content Editor", BeamGUI.iconContentEditorIcon, width: 90, iconPadding: 2))
				{
					ChangeWindowStatus(ContentWindowStatus.Normal);
				}
			}
			if (_windowStatus is ContentWindowStatus.Normal || _windowStatus is ContentWindowStatus.Validate)
			{
				var hasContentToPublish = _contentService.HasChangedContents;
				var hasConflictedOrInvalid = _contentService.HasConflictedContent || _contentService.HasInvalidContent;

				string publishTooltip = "Publish Content to Current Realm";
				string syncTooltip = "Sync contents with Current Realm";
				string validateTooltip = "Validate Local Changes";
				if (hasConflictedOrInvalid)
				{
					publishTooltip = "There is Conflicted or Invalid Content, unable to Publish.";
				}
				else if (!hasContentToPublish)
				{
					publishTooltip = "There is not any modified items to publish. You are up-to-date.";
					syncTooltip = "There is not any modified items to sync. You are up-to-date.";
				}

				if (_windowStatus != ContentWindowStatus.Validate)
				{
					
					if (BeamGUI.HeaderButton("Validate", BeamGUI.iconCheck,
					                                                    width: HEADER_BUTTON_WIDTH, iconPadding: 2,
					                                                    tooltip: validateTooltip))
					{
						ChangeToValidateMode();
					}
				}

				if (BeamGUI.ShowDisabled(hasContentToPublish || hasConflictedOrInvalid,
				                         () => BeamGUI.HeaderButton("Sync", BeamGUI.iconSync,
				                                                    width: HEADER_BUTTON_WIDTH, iconPadding: 2,
				                                                    tooltip: syncTooltip)))
				{
					ShowSyncMenu();
				}

				var hasPrivs = _cli.Permissions.CanPushContent;
				if (!hasPrivs)
				{
					publishTooltip = $"{_cli?.latestUser?.email ?? "this user"} does not have sufficient permission to publish content on this realm.";
				}
				if (BeamGUI.ShowDisabled(hasPrivs && hasContentToPublish && !hasConflictedOrInvalid,
				                         () => BeamGUI.HeaderButton("Publish", BeamGUI.iconPublish,
				                                                    width: HEADER_BUTTON_WIDTH, iconPadding: 2,
				                                                    tooltip: publishTooltip)))
				{
					ChangeToPublishMode();
				}

				if (BeamGUI.HeaderButton("Snapshot", BeamGUI.iconContentSnapshotWhite, width: HEADER_BUTTON_WIDTH, iconPadding: 2,
				                         tooltip: "Manages content snapshots"))
				{
					ChangeToSnapshotManager();
				}

				if (_windowStatus != ContentWindowStatus.Validate)
				{
					EditorGUILayout.Space(5, false);
					this.DrawSearchBar(_contentSearchData, true);
					DrawFilterButton(ContentSearchFilterType.Tag, BeamGUI.iconTag, _allTags);
					DrawFilterButton(ContentSearchFilterType.Type, BeamGUI.iconType, _allTypes);
					DrawFilterButton(ContentSearchFilterType.Status, BeamGUI.iconStatus, AllStatus);
				}
			}
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

		private void ChangeWindowStatus(ContentWindowStatus windowStatus, bool shouldRepaint = true)
		{
			if(_windowStatus == windowStatus)
				return;
			
			_windowStatus = windowStatus;
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
			await _contentService.SyncContentsWithProgress(true, true, true, true);
		}

		private async Promise RevertModifiedContents()
		{
			await _contentService.SyncContentsWithProgress(true, false, false, false);
		}

		private async Promise RevertConflictedContents()
		{
			await _contentService.SyncContentsWithProgress(false, false, true, false);
		}

		private async Promise RevertDeletedContents()
		{
			await _contentService.SyncContentsWithProgress(false, false, false, true);
		}

		private async Promise RevertAllNewContents()
		{
			await _contentService.SyncContentsWithProgress(false, true, false, false);
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
			if (!isClicked)
			{
				return;
			}

			var activeItemsOnFilter = GetFilterTypeActiveItems(searchFilterType);
			var itemStatus = items.ToDictionary(item => item, s => activeItemsOnFilter.Contains(s));
			ToggleListWindow.Show(buttonRect, new Vector2(200, 250), itemStatus, (item, state) =>
			{
				if (state)
				{
					activeItemsOnFilter.Add(item);
				}
				else
				{
					activeItemsOnFilter.Remove(item);
				}

				UpdateActiveFilterSearchText();
			});
		}

		private void OnTextChange()
		{
			if (string.IsNullOrEmpty(_contentSearchData.searchText))
			{
				_activeFilters.Clear();
				return;
			}

			string searchText = _contentSearchData.searchText;
			string[] searchTextParts = searchText.Split(",");
			foreach ((ContentSearchFilterType contentType, string filterTag) in ContentFilterTypeToQueryTag)
			{
				foreach (string searchTextPart in searchTextParts)
				{
					if (searchTextPart.Contains(filterTag))
					{
						var searchItems = searchTextPart.Replace(filterTag, string.Empty).Trim().Split(" ").ToList();
						if (!_activeFilters.TryGetValue(contentType, out var activeFilter))
						{
							activeFilter = new HashSet<string>();
						}
						activeFilter.Clear();
						searchItems.ForEach(searchItem => activeFilter.Add(searchItem));
						break;
					}
				}
			}
			
			AddDelayedAction(Repaint);
			
		}

		private void UpdateActiveFilterSearchText()
		{
			var searchText = _contentSearchData.searchText ?? string.Empty;
			var searchTextParts = searchText.Split(',');
			var newSearchTextParts = new List<string>();

			foreach ((ContentSearchFilterType type, string filterLabel) in ContentFilterTypeToQueryTag)
			{
				bool hasFilterToType = _activeFilters.TryGetValue(type, out var filterData) && filterData.Count > 0;
				HashSet<string> data = filterData ?? new HashSet<string>();
				string typeSearchString = $"{filterLabel} {string.Join(' ', data.OrderBy(item => item))}";
				bool isUpdated = false;
				for (int index = 0; index < searchTextParts.Length; index++)
				{
					if (searchTextParts[index].Contains(filterLabel))
					{
						searchTextParts[index] = hasFilterToType ? typeSearchString : string.Empty;
						isUpdated = true;
						break;
					}
				}

				if (isUpdated || !hasFilterToType)
				{
					continue;
				}

				newSearchTextParts.Add(typeSearchString);
			}

			IEnumerable<string> items = searchTextParts.Concat(newSearchTextParts)
			                                           .Where(s => !string.IsNullOrEmpty(s))
			                                           .Select(s => s.Trim());
			_contentSearchData.searchText = string.Join(", ", items);
			Repaint();
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
			string searchText = _contentSearchData?.searchText ?? string.Empty;
			string[] searchTextParts = searchText.Split(',');
			foreach (string searchTextPart in searchTextParts)
			{
				if (ContentFilterTypeToQueryTag.Any(typeQueryTag => searchTextPart.Contains(typeQueryTag.Value)))
				{
					continue;
				}
				return searchTextPart.Trim();
			}
			return string.Empty;
		}
		
	}
}
