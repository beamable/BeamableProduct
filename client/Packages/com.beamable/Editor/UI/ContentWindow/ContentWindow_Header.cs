using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Util;
using Editor.UI2.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const int HEADER_BUTTON_WIDTH = 50;
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
		
		private static readonly Dictionary<ContentStatus, string> StatusMapToString = new()
		{
			{ContentStatus.Invalid, "invalid"},
			{ContentStatus.Created, "created"},
			{ContentStatus.Deleted, "deleted"},
			{ContentStatus.Modified, "modified"},
			{ContentStatus.UpToDate, "up to date"}
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
				alignment = TextAnchor.MiddleLeft,
				margin = new RectOffset(0, 15, 5, 0),
			};
		}

		private void DrawHeader()
		{
			bool clickedSync = false;
			bool clickedPublish = false;
			BeamGUI.DrawHeaderSection(this, ActiveContext, () =>
			{
				
				clickedSync = BeamGUI.HeaderButton("Sync", BeamGUI.iconRotate, width: HEADER_BUTTON_WIDTH);
				clickedPublish = BeamGUI.HeaderButton("Publish", BeamGUI.iconUpload, width: HEADER_BUTTON_WIDTH);
				EditorGUILayout.Space(5, false);
				this.DrawSearchBar(_contentSearchData, true);
				DrawFilterButton(ContentFilterType.Tag, BeamGUI.iconTag, _allTags);
				DrawFilterButton(ContentFilterType.Type, BeamGUI.iconType, _allTypes);
				DrawFilterButton(ContentFilterType.Status, BeamGUI.iconStatus, AllStatus);
			}, DrawLowBarHeader, () =>
			{
				Application.OpenURL("https://docs.beamable.com/docs/content-manager-overview");
			}, Repaint);

			if (clickedSync)
			{
				
			}

			if (clickedPublish)
			{
				ShowPublishMenu();
			}
		}

		private void DrawLowBarHeader()
		{
			GUILayout.Space(15);
			
			var itemsFilterLabelRect =
				GUILayoutUtility.GetRect(GUIContent.none, _lowBarTextStyle, GUILayout.Width(50), GUILayout.ExpandHeight(true));
			var contentTreeLabelRect =
				GUILayoutUtility.GetRect(GUIContent.none, _lowBarTextStyle, GUILayout.MinWidth(350), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			var entries = GetCachedManifestEntries();
			int filteredItemsCount = GetFilteredItems().Count;
			int totalItems = entries.Count;
			string contentTreeLabelValue = "All Content";
			contentTreeLabelValue += SelectedContentType.Count == 0
				? ""
				: $" > {string.Join(" | ", SelectedContentType.OrderBy(item => item).Select(item => item.Replace(".", ">")))}";


			GUI.Label(itemsFilterLabelRect, $"{filteredItemsCount}/{totalItems}", _lowBarTextStyle);
			GUI.Label(contentTreeLabelRect, contentTreeLabelValue, _lowBarTextStyle);

			EditorGUILayout.Space(1, true);
			GUIContent dropdownContent = new GUIContent($"{SortTypeNameMap[_currentSortOption]} ▼");
			
			if (EditorGUILayout.DropdownButton(
				    dropdownContent,
				    FocusType.Passive,
				    _lowBarDropdownStyle,
				    GUILayout.Width(100)))
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

		private static void ShowPublishMenu()
		{
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Open Publish Window"), false, () => { });
			menu.AddItem(new GUIContent("Publish New Content namespace"), false, () => { });
			menu.AddItem(new GUIContent("Archive namespaces"), false, () => { });
			menu.AddItem(new GUIContent("Publish (default)"), false, () => { });
			menu.ShowAsContext();
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
