using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Util;
using Editor.UI2.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const int HEADER_BUTTON_WIDTH = 50;
		private const string REVERT_ALL_MENU_ITEM = "Revert All Local Changes (Modified, Created, Deleted, and Conflicted)";
		private const string REVERT_MODIFIED_MENU_ITEM = "Revert Modified Local Changes";
		private const string REVERT_CONFLICTED_MENU_ITEM = "Revert Conflicted Changes Only";
		private const string REVERT_DELETED_MENU_ITEM = "Revert Deleted Contents Only";
		private const string REVERT_NEW_CONTENTS_MENU_ITEM = "Delete All New Created Changes";
		
		
		private readonly Dictionary<ContentFilterType, HashSet<string>> _activeFilters = new();
		private List<string> _allTypes = new();
		private List<string> _allTags = new();
		
		private ContentSortOptionType _currentSortOption;
		private GUIStyle _lowBarTextStyle;
		private GUIStyle _lowBarDropdownStyle;
		private static List<string> AllStatus => StatusMapToString.Values.ToList();

		private static readonly Dictionary<ContentFilterType, string> ContentFilterTypeToQueryTag = new()
		{
			{ContentFilterType.Tag, "tag:"},
			{ContentFilterType.Status, "status:"},
			{ContentFilterType.Type, "type:"}
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
			{ContentSortOptionType.Status, "Status"}
		};
		
		

		private void BuildHeaderFilters()
		{
			_contentSearchData = new SearchData() {onEndCheck = OnTextChange};
			
			_allTypes = _contentTypeReflectionCache.GetAll()
			                                       .OrderBy(pair => pair.Name)
			                                       .Select(pair => pair.Name)
			                                       .ToList();
			
			HashSet<string> tags = new();
			var entries = GetCachedManifestEntries();
			foreach (LocalContentManifestEntry cachedManifestEntry in entries)
			{
				foreach (string tag in cachedManifestEntry.Tags)
				{
					tags.Add(tag);
				}
			}
			_allTags = new List<string>(tags);
		}

		private void BuildHeaderStyles()
		{
			_lowBarTextStyle = new GUIStyle(EditorStyles.boldLabel) {alignment = TextAnchor.MiddleLeft};
			
			_lowBarDropdownStyle = new GUIStyle(EditorStyles.toolbarDropDown)
			{
				normal = {background = BeamGUI.CreateColorTexture(new Color(0.35f, 0.35f, 0.35f))},
				alignment = TextAnchor.MiddleRight,
				margin = new RectOffset(0, 15, 5, 0),
			};
		}

		private void DrawHeader()
		{
			BeamGUI.DrawHeaderSection(this, ActiveContext, DrawTopBarHeader, DrawLowBarHeader, () =>
			{
				Application.OpenURL("https://docs.beamable.com/docs/content-manager-overview");
			}, () => _ = _contentService.Reload());
		}

		private void DrawTopBarHeader()
		{
			if (_windowStatus is ContentWindowStatus.Normal)
			{
				var items = GetCachedManifestEntries();
				var hasContentToPublish =
					items.Any(item => item.StatusEnum is ContentStatus.Deleted or ContentStatus.Created
						          or ContentStatus.Modified);
				var hasConflictedOrInvalid =
					items.Any(item => _contentService.IsContentInvalid(item.FullId) || item.IsInConflict);

				string tooltip = "Publish Content to Current Realm";
				if (hasConflictedOrInvalid)
				{
					tooltip = "There is Conflicted or Invalid Content, unable to Publish.";
				}
				else if (!hasContentToPublish)
				{
					tooltip = "There is not any modified items to publish. You are up-to-date.";
				}


				if (BeamGUI.HeaderButton("Sync", BeamGUI.iconSync, width: HEADER_BUTTON_WIDTH, iconPadding: 2))
				{
					ShowSyncMenu();
				}

				if (BeamGUI.ShowDisabled(hasContentToPublish && !hasConflictedOrInvalid,
				                         () => BeamGUI.HeaderButton("Publish", BeamGUI.iconPublish,
				                                                    width: HEADER_BUTTON_WIDTH, iconPadding: 2,
				                                                    tooltip: tooltip)))
				{
					PublishAction();
				}

				EditorGUILayout.Space(5, false);
				this.DrawSearchBar(_contentSearchData, true);
				DrawFilterButton(ContentFilterType.Tag, BeamGUI.iconTag, _allTags);
				DrawFilterButton(ContentFilterType.Type, BeamGUI.iconType, _allTypes);
				DrawFilterButton(ContentFilterType.Status, BeamGUI.iconStatus, AllStatus);
			}
			else
			{
				if (BeamGUI.HeaderButton("Content Editor", BeamGUI.iconContentEditorIcon, width: 90, iconPadding: 2))
				{
					_windowStatus = ContentWindowStatus.Normal;
					Repaint();
				}
			}
		}

		private void DrawLowBarHeader()
		{
			if(_windowStatus == ContentWindowStatus.Publish)
				return;
			
			GUILayout.Space(15);

			GUIStyle lowBarTextStyle = _lowBarTextStyle ?? EditorStyles.boldLabel;
			var itemsFilterLabelRect =
				GUILayoutUtility.GetRect(GUIContent.none, lowBarTextStyle, GUILayout.Width(50), GUILayout.ExpandHeight(true));
			var contentTreeLabelRect =
				GUILayoutUtility.GetRect(GUIContent.none, lowBarTextStyle, GUILayout.MinWidth(350), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			var entries = GetCachedManifestEntries();
			int filteredItemsCount = GetFilteredItems().Count;
			int totalItems = entries.Count;
			string contentTreeLabelValue = "All Content";
			contentTreeLabelValue += SelectedContentType.Count == 0
				? ""
				: $" > {string.Join(" | ", SelectedContentType.OrderBy(item => item).Select(item => item.Replace(".", ">")))}";


			GUI.Label(itemsFilterLabelRect, $"{filteredItemsCount}/{totalItems}", lowBarTextStyle);
			GUI.Label(contentTreeLabelRect, contentTreeLabelValue, lowBarTextStyle);

			EditorGUILayout.Space(1, true);
			GUIContent dropdownContent = new GUIContent($"{SortTypeNameMap[_currentSortOption]} ▼");
			GUIStyle lowBarDropdownStyle = _lowBarDropdownStyle ?? EditorStyles.toolbarDropDown;
			Vector2 itemWidth = lowBarDropdownStyle.CalcSize(dropdownContent);
			if (EditorGUILayout.DropdownButton(
				    dropdownContent,
				    FocusType.Passive,
				    lowBarDropdownStyle,
				    GUILayout.Width(itemWidth.x)))
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

		}
		
		private void ShowSyncMenu()
		{
			var entries = GetCachedManifestEntries();
			bool hasModified = false;
			bool hasNewItems = false;
			bool hasConflictedItems = false;
			bool hasDeleted = false;
			
			foreach (LocalContentManifestEntry localContentManifestEntry in entries)
			{
				hasModified |= localContentManifestEntry.StatusEnum is ContentStatus.Modified or ContentStatus.Invalid;
				hasNewItems |= localContentManifestEntry.StatusEnum is ContentStatus.Created;
				hasConflictedItems |= localContentManifestEntry.IsInConflict;
				hasDeleted |= localContentManifestEntry.StatusEnum is ContentStatus.Deleted;
				
				if (hasModified && hasNewItems && hasConflictedItems && hasDeleted)
					break;
			}
			
			GenericMenu menu = new GenericMenu();
			if (hasModified || hasNewItems || hasConflictedItems || hasDeleted)
			{
				menu.AddItem(new GUIContent(REVERT_ALL_MENU_ITEM), false, () => _ = RevertAllContents());
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_ALL_MENU_ITEM), false);
			}

			if (hasModified)
			{
				menu.AddItem(new GUIContent(REVERT_MODIFIED_MENU_ITEM), false, () => _ = RevertModifiedContents());
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_MODIFIED_MENU_ITEM), false);
			}

			if (hasConflictedItems)
			{
				menu.AddItem(new GUIContent(REVERT_CONFLICTED_MENU_ITEM), false, () => _ = RevertConflictedContents());
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_CONFLICTED_MENU_ITEM), false);
			}
			
			if (hasDeleted)
			{
				menu.AddItem(new GUIContent(REVERT_DELETED_MENU_ITEM), false, () => _ = RevertDeletedContents());
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_DELETED_MENU_ITEM), false);
			}

			if (hasNewItems)
			{
				menu.AddItem(new GUIContent(REVERT_NEW_CONTENTS_MENU_ITEM), false, () => _ = RevertAllNewContents());
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(REVERT_NEW_CONTENTS_MENU_ITEM), false);
			}

			menu.ShowAsContext();
		}

		private async Task RevertAllContents()
		{
			if (!EditorUtility.DisplayDialog("Revert Content", "Are you sure you want to revert all local changes?", "Yes", "No"))
			{
				return;
			}
			await _contentService.SyncContentsWithProgress(true, true, true, true);
		}

		private async Task RevertModifiedContents()
		{
			if (!EditorUtility.DisplayDialog("Revert Content", "Are you sure you want to revert all modified contents?", "Yes", "No"))
			{
				return;
			}

			await _contentService.SyncContentsWithProgress(true, false, false, false);
		}

		private async Task RevertConflictedContents()
		{
			if (!EditorUtility.DisplayDialog("Revert Content", "Are you sure you want to revert all conflicted contents?", "Yes", "No"))
			{
				return;
			}
			await _contentService.SyncContentsWithProgress(false, false, true, false);
		}

		private async Task RevertDeletedContents()
		{
			if (!EditorUtility.DisplayDialog("Revert Content", "Are you sure you want to revert all deleted contents?", "Yes", "No"))
			{
				return;
			}
			await _contentService.SyncContentsWithProgress(false, false, false, true);
		}

		private async Task RevertAllNewContents()
		{
			if (!EditorUtility.DisplayDialog("Revert Content", "Are you sure you want to revert all newly created contents?", "Yes", "No"))
			{
				return;
			}
			await _contentService.SyncContentsWithProgress(false, true, false, false);
		}

		private void PublishAction()
		{
			_windowStatus = ContentWindowStatus.Publish;
			Repaint();
		}


		private void DrawFilterButton(ContentFilterType filterType, Texture icon, IEnumerable<string> items)
		{
			bool hasActiveFilter = _activeFilters.TryGetValue(filterType, out var activeItems) && activeItems.Count > 0;
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

			var activeItemsOnFilter = GetFilterTypeActiveItems(filterType);
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
			foreach ((ContentFilterType contentType, string filterTag) in ContentFilterTypeToQueryTag)
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

			foreach ((ContentFilterType type, string filterLabel) in ContentFilterTypeToQueryTag)
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

		private HashSet<string> GetFilterTypeActiveItems(ContentFilterType type)
		{
			if (!_activeFilters.TryGetValue(type, out HashSet<string> items))
			{
				_activeFilters[type] = items = new HashSet<string>();
			}

			return items;
		}

		private string GetNameSearchPartValue()
		{
			string searchText = _contentSearchData.searchText ?? string.Empty;
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
