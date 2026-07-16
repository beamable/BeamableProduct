using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.ContentService;
using Beamable.Editor.ThirdParty.Splitter;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private static readonly int[] HistoryPageSizes = { 10, 20, 30, 40, 50 };
		private const int HistoryChangesPageSize = 25;
		private const float HistoryEntryRowHeight = 24f;
		private const float HistoryChangeRowHeight = 22f;
		private const float HistoryTimeColumnWidth = 74f;
		private const float HistoryChangesColumnWidth = 82f;
		private const float HistoryAuthorColumnWidth = 112f;
		private static readonly Color HistoryEntryColor = new(.18f, .18f, .18f, 1f);
		private static readonly Color HistoryEntryHoverColor = new(.25f, .28f, .31f, 1f);
		private static readonly Color HistoryExpandedBucketColor = new(.20f, .21f, .23f, 1f);
		private static readonly Color HistoryCollapsedBucketColor = new(.12f, .13f, .15f, 1f);
		private static readonly Color HistoryExpandedBucketHoverColor = new(.27f, .30f, .34f, 1f);
		private static readonly Color HistoryCollapsedBucketHoverColor = new(.20f, .23f, .27f, 1f);
		private EditorGUISplitView _historySplitter;
		private SearchData _historySearchData;
		private Vector2 _historyEntriesScroll;
		private Vector2 _historyChangesScroll;
		private Vector2 _historyPreviewScroll;
		private string _selectedHistoryManifestUid;
		private BeamContentHistoryChangelistEntry[] _selectedHistoryChanges = Array.Empty<BeamContentHistoryChangelistEntry>();
		private string _selectedHistoryJson = string.Empty;
		private int _historyPageIndex;
		private int _historyChangesPageIndex;
		private int _historySelectionRequestVersion;
		private int _historyPreviewRequestVersion;
		private int _filteredHistoryVersion = -1;
		private string _historyFilterKey;
		private IReadOnlyList<BeamContentHistoryEntry> _filteredHistoryEntries = Array.Empty<BeamContentHistoryEntry>();
		private IReadOnlyList<ContentHistoryTimeBucket> _historyBuckets = Array.Empty<ContentHistoryTimeBucket>();
		private readonly HashSet<string> _expandedHistoryBucketKeys = new();
		private int _historyBucketVersion = -1;
		private string _historyBucketFilterKey;
		private DateTime _historyBucketLocalDate;
		private string _historyBucketAutoExpandKey;
		private IReadOnlyList<ContentHistoryTimeBucketRow> _historyDisplayRows = Array.Empty<ContentHistoryTimeBucketRow>();
		private bool _historyDisplayRowsDirty = true;
		private string _hoveredHistoryRowKey;
		private bool _isLoadingHistoryChanges;
		private bool _isLoadingHistoryPreview;
		private bool _isRestoringHistory;
		private string _historyPreviewContentId;
		private ContentHistoryOperationException _historyChangesError;
		private ContentHistoryOperationException _historyPreviewError;
		private ContentHistoryOperationException _historyRestoreError;
		private GUIStyle _historyAddedChangeStyle;
		private GUIStyle _historyModifiedChangeStyle;
		private GUIStyle _historyRemovedChangeStyle;
		private GUIStyle _historyDefaultChangeStyle;
		private GUIStyle _historyRestoreButtonStyle;
		private GUIStyle _historyEllipsisLabelStyle;
		private readonly GUIContent _historyEllipsisMeasureContent = new();

		private void DrawContentHistory()
		{
			EnsureHistorySearchData();
			EnsureHistoryStyles();
			if (IsContentHistoryLoading() || _isRestoringHistory)
			{
				Repaint();
			}
			if (_historySplitter == null || _historySplitter.cellCount < 2)
			{
				_historySplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, .5f, .5f);
				EditorApplication.delayCall += Repaint;
			}

			EditorGUILayout.BeginVertical();
			_historySplitter.BeginSplitView();
			DrawHistoryEntries();
			_historySplitter.Split(this);
			DrawHistoryChanges();
			_historySplitter.EndSplitView();
			EditorGUILayout.EndVertical();
		}

		private void EnsureHistorySearchData()
		{
			if (_historySearchData != null)
			{
				return;
			}

			_historySearchData = new SearchData
			{
				onEndCheck = () =>
				{
					_historyPageIndex = 0;
					_historyEntriesScroll = Vector2.zero;
					_historyFilterKey = null;
					_historyBucketAutoExpandKey = null;
					_historyDisplayRowsDirty = true;
				}
			};
		}

		private void DrawHistoryEntries()
		{
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField("Publish History", EditorStyles.boldLabel);
			this.DrawSearchBar(_historySearchData);
			if (IsContentHistoryLoading())
			{
				DrawHistoryLoadingState();
				EditorGUILayout.EndVertical();
				return;
			}

			var filteredEntries = GetFilteredHistoryEntries();
			var historyBuckets = GetHistoryBuckets(filteredEntries);
			AutoExpandHistoryBucketsForSearch(historyBuckets, _historySearchData?.searchText?.Trim());

			/*
			 * Publish pagination is intentionally disabled while Smart Time Buckets are enabled.
			 * Paging entries before flattening the expandable hierarchy made a bucket header describe
			 * history that was not actually visible on that page. Keep the existing pagination code
			 * below for a future non-hierarchical view, but use one virtualized hierarchy here.
			 *
			var pageSize = ContentHistoryPagination.ClampPageSize(_contentConfiguration.HistoryEntriesPerPage);
			var pageCount = ContentHistoryPagination.GetPageCount(filteredEntries.Count, pageSize);
			_historyPageIndex = ContentHistoryPagination.ClampPageIndex(_historyPageIndex, pageCount);
			var firstEntryIndex = _historyPageIndex * pageSize;
			var currentEntryCount = Math.Min(pageSize, Math.Max(0, filteredEntries.Count - firstEntryIndex));
			*/

			if (_contentService.ContentHistoryWatcherError != null)
			{
				DrawHistoryOperationError(_contentService.ContentHistoryWatcherError, RetryContentHistory);
			}

			if (filteredEntries.Count == 0)
			{
				if (_contentService.ContentHistoryWatcherError == null)
				{
					EditorGUILayout.HelpBox("No published content history matches this search.", MessageType.Info);
				}
			}
			else
			{
				DrawHistoryTableHeader();
				DrawVirtualHistoryEntries(historyBuckets);
			}

			// DrawHistoryPagination(filteredEntries.Count, pageSize, pageCount);
			EditorGUILayout.EndVertical();
		}

		private bool IsContentHistoryLoading()
		{
			return _contentService != null && _contentService.IsContentHistoryWatching &&
			       !_contentService.HasReceivedInitialContentHistory;
		}

		private void DrawHistoryLoadingState()
		{
			var frame = (int)(EditorApplication.timeSinceStartup * 12) % 12;
			var spinner = EditorGUIUtility.IconContent($"WaitSpin{frame:00}").image ?? BeamGUI.iconRefresh;
			EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(new GUIContent(" Loading published history...", spinner), EditorStyles.label);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
		}

		private IReadOnlyList<BeamContentHistoryEntry> GetFilteredHistoryEntries()
		{
			var search = _historySearchData?.searchText?.Trim();
			var historyVersion = _contentService.ContentHistoryVersion;
			if (_filteredHistoryVersion == historyVersion && string.Equals(_historyFilterKey, search, StringComparison.Ordinal))
			{
				return _filteredHistoryEntries;
			}

			var entries = _contentService.ContentHistoryEntries;
			_filteredHistoryEntries = string.IsNullOrEmpty(search)
				? entries
				: entries.Where(entry =>
					(entry.ManifestUid?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
					(entry.PublishedBy?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
					(entry.PublishedByName?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
					.ToList();
			_filteredHistoryVersion = historyVersion;
			_historyFilterKey = search;
			return _filteredHistoryEntries;
		}

		private IReadOnlyList<ContentHistoryTimeBucket> GetHistoryBuckets(IReadOnlyList<BeamContentHistoryEntry> entries)
		{
			var search = _historySearchData?.searchText?.Trim();
			var historyVersion = _contentService.ContentHistoryVersion;
			var localNow = DateTime.Now;
			if (_historyBucketVersion == historyVersion && _historyBucketLocalDate == localNow.Date &&
			    string.Equals(_historyBucketFilterKey, search, StringComparison.Ordinal))
			{
				return _historyBuckets;
			}

			var resetExpansion = _historyBucketVersion < 0 || !string.Equals(_historyBucketFilterKey, search, StringComparison.Ordinal);
			if (resetExpansion)
			{
				_expandedHistoryBucketKeys.Clear();
			}

			_historyBuckets = ContentHistoryTimeBuckets.Create(entries, localNow);
			_historyBucketVersion = historyVersion;
			_historyBucketFilterKey = search;
			_historyBucketLocalDate = localNow.Date;
			_historyBucketAutoExpandKey = null;
			_historyDisplayRowsDirty = true;
			if (resetExpansion)
			{
				_expandedHistoryBucketKeys.UnionWith(ContentHistoryTimeBuckets.GetDefaultExpandedKeys(_historyBuckets));
			}
			return _historyBuckets;
		}

		private void AutoExpandHistoryBucketsForSearch(IReadOnlyList<ContentHistoryTimeBucket> buckets, string search)
		{
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			var key = $"{_historyBucketVersion}|{search}";
			if (string.Equals(_historyBucketAutoExpandKey, key, StringComparison.Ordinal))
			{
				return;
			}

			var didExpandBucket = false;
			foreach (var entry in _filteredHistoryEntries)
			{
				foreach (var ancestorKey in ContentHistoryTimeBuckets.GetAncestorKeys(buckets, entry.ManifestUid))
				{
					didExpandBucket |= _expandedHistoryBucketKeys.Add(ancestorKey);
				}
			}

			_historyBucketAutoExpandKey = key;
			_historyDisplayRowsDirty |= didExpandBucket;
		}

		private void DrawHistoryTableHeader()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			EditorGUILayout.LabelField("Time", EditorStyles.miniLabel, GUILayout.Width(HistoryTimeColumnWidth));
			EditorGUILayout.LabelField("Changes", EditorStyles.miniLabel, GUILayout.Width(HistoryChangesColumnWidth));
			EditorGUILayout.LabelField("Manifest ID", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
			EditorGUILayout.LabelField("Author", EditorStyles.miniLabel, GUILayout.Width(HistoryAuthorColumnWidth));
			EditorGUILayout.EndHorizontal();
		}

		private void DrawVirtualHistoryEntries(IReadOnlyList<ContentHistoryTimeBucket> buckets)
		{
			var rows = GetHistoryDisplayRows(buckets);
			var areaRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var contentRect = new Rect(0, 0, Math.Max(0, areaRect.width - 16), rows.Count * HistoryEntryRowHeight);
			_historyEntriesScroll = GUI.BeginScrollView(areaRect, _historyEntriesScroll, contentRect, false, true);
			var visibleRange = ContentHistoryPagination.GetVisibleRange(rows.Count, _historyEntriesScroll.y, areaRect.height, HistoryEntryRowHeight);
			UpdateHistoryHover(rows, visibleRange, contentRect.width);
			for (var index = visibleRange.FirstIndex; index < visibleRange.LastExclusive; index++)
			{
				DrawHistoryDisplayRow(rows[index], new Rect(0, index * HistoryEntryRowHeight, contentRect.width, HistoryEntryRowHeight));
			}
			GUI.EndScrollView();
		}

		private void UpdateHistoryHover(IReadOnlyList<ContentHistoryTimeBucketRow> rows, ContentHistoryVisibleRange visibleRange,
			float contentWidth)
		{
			if (Event.current.type != EventType.MouseMove && Event.current.type != EventType.MouseLeaveWindow)
			{
				return;
			}

			string nextHoveredRowKey = null;
			if (Event.current.type == EventType.MouseMove)
			{
				for (var index = visibleRange.FirstIndex; index < visibleRange.LastExclusive; index++)
				{
					var rowRect = new Rect(0, index * HistoryEntryRowHeight, contentWidth, HistoryEntryRowHeight);
					if (rowRect.Contains(Event.current.mousePosition))
					{
						nextHoveredRowKey = GetHistoryRowKey(rows[index]);
						break;
					}
				}
			}

			if (!ContentHistoryRowInteraction.ShouldRepaint(_hoveredHistoryRowKey, nextHoveredRowKey))
			{
				return;
			}

			_hoveredHistoryRowKey = nextHoveredRowKey;
			Repaint();
		}

		private IReadOnlyList<ContentHistoryTimeBucketRow> GetHistoryDisplayRows(IReadOnlyList<ContentHistoryTimeBucket> buckets)
		{
			if (_historyDisplayRowsDirty)
			{
				_historyDisplayRows = ContentHistoryTimeBuckets.BuildVisibleRows(buckets, _expandedHistoryBucketKeys);
				_historyDisplayRowsDirty = false;
			}

			return _historyDisplayRows;
		}

		private void DrawHistoryDisplayRow(ContentHistoryTimeBucketRow row, Rect rowRect)
		{
			if (row.Bucket != null)
			{
				DrawHistoryBucket(row.Bucket, rowRect, GetHistoryRowKey(row));
				return;
			}

			DrawHistoryEntry(row.Entry, rowRect, GetHistoryRowKey(row));
		}

		private static string GetHistoryRowKey(ContentHistoryTimeBucketRow row)
		{
			return row.Bucket != null ? row.Bucket.Key : row.Entry.ManifestUid;
		}

		private bool IsHistoryBucketExpanded(ContentHistoryTimeBucket bucket)
		{
			return _expandedHistoryBucketKeys.Contains(bucket.Key);
		}

		private void DrawHistoryBucket(ContentHistoryTimeBucket bucket, Rect rowRect, string rowKey)
		{
			var isExpanded = IsHistoryBucketExpanded(bucket);
			var isHovering = string.Equals(_hoveredHistoryRowKey, rowKey, StringComparison.Ordinal);
			var backgroundColor = isExpanded
				? (isHovering ? HistoryExpandedBucketHoverColor : HistoryExpandedBucketColor)
				: (isHovering ? HistoryCollapsedBucketHoverColor : HistoryCollapsedBucketColor);
			EditorGUI.DrawRect(rowRect, backgroundColor);
			EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.yMax - 1, rowRect.width, 1), new Color(0f, 0f, 0f, .35f));
			var chevron = isExpanded ? "v" : ">";
			GUI.Label(new Rect(rowRect.x + 6, rowRect.y + 3, rowRect.width - 12, EditorGUIUtility.singleLineHeight),
				$"{chevron} {bucket.Label} ({bucket.EntryCount})", EditorStyles.boldLabel);

			if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
			{
				if (isExpanded)
				{
					_expandedHistoryBucketKeys.Remove(bucket.Key);
					_historyDisplayRowsDirty = true;
				}
				else
				{
					_expandedHistoryBucketKeys.Add(bucket.Key);
					_historyDisplayRowsDirty = true;
				}
			}
		}

		private void DrawHistoryEntry(BeamContentHistoryEntry entry, Rect rowRect, string rowKey)
		{
			var isSelected = entry.ManifestUid == _selectedHistoryManifestUid;
			var visualState = ContentHistoryRowInteraction.GetVisualState(isSelected,
				string.Equals(_hoveredHistoryRowKey, rowKey, StringComparison.Ordinal));
			if (visualState == ContentHistoryRowVisualState.Selected)
			{
				GUI.Box(rowRect, GUIContent.none, EditorStyles.selectionRect);
			}
			else
			{
				EditorGUI.DrawRect(rowRect, visualState == ContentHistoryRowVisualState.Hovered
					? HistoryEntryHoverColor
					: HistoryEntryColor);
			}

			var date = DateTimeOffset.FromUnixTimeMilliseconds(entry.CreatedDate).LocalDateTime;
			var timeRect = new Rect(rowRect.x + 6, rowRect.y + 3, HistoryTimeColumnWidth - 6, EditorGUIUtility.singleLineHeight);
			var changesRect = new Rect(timeRect.xMax, rowRect.y + 3, HistoryChangesColumnWidth, EditorGUIUtility.singleLineHeight);
			var authorRect = new Rect(rowRect.xMax - HistoryAuthorColumnWidth, rowRect.y + 3, HistoryAuthorColumnWidth - 6, EditorGUIUtility.singleLineHeight);
			var manifestRect = new Rect(changesRect.xMax, rowRect.y + 3, Math.Max(0, authorRect.x - changesRect.xMax), EditorGUIUtility.singleLineHeight);

			using (new EditorGUI.DisabledScope(_isRestoringHistory))
			{
				if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
				{
					SelectHistoryEntry(entry.ManifestUid);
				}
			}

			GUI.Label(timeRect, date.ToString("h:mm tt"), EditorStyles.miniLabel);
			GUI.Label(changesRect, $"{entry.AffectedContentIds?.Length ?? 0} changed", EditorStyles.miniLabel);
			DrawHistoryTruncatedLabel(manifestRect, entry.ManifestUid);
			DrawHistoryTruncatedLabel(authorRect, string.IsNullOrEmpty(entry.PublishedByName) ? entry.PublishedBy : entry.PublishedByName);
		}

		private void DrawHistoryPagination(int entryCount, int pageSize, int pageCount)
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			var first = entryCount == 0 ? 0 : _historyPageIndex * pageSize + 1;
			var last = Math.Min(entryCount, (_historyPageIndex + 1) * pageSize);
			EditorGUILayout.LabelField($"{first}-{last} of {entryCount} publishes", GUILayout.ExpandWidth(true));

			using (new EditorGUI.DisabledScope(_historyPageIndex == 0))
			{
				if (GUILayout.Button("<", GUILayout.Width(24))) SetHistoryPage(_historyPageIndex - 1);
			}
			EditorGUILayout.LabelField($"{_historyPageIndex + 1}/{Math.Max(pageCount, 1)}", GUILayout.Width(48));
			using (new EditorGUI.DisabledScope(_historyPageIndex >= pageCount - 1))
			{
				if (GUILayout.Button(">", GUILayout.Width(24))) SetHistoryPage(_historyPageIndex + 1);
			}

			if (GUILayout.Button($"Per page: {pageSize}", EditorStyles.toolbarDropDown, GUILayout.Width(92)))
			{
				var menu = new GenericMenu();
				foreach (var size in HistoryPageSizes)
				{
					menu.AddItem(new GUIContent(size.ToString()), size == pageSize, () => SetHistoryPageSize(size));
				}
				menu.ShowAsContext();
			}
			EditorGUILayout.EndHorizontal();
		}

		private void SetHistoryPageSize(int pageSize)
		{
			_contentConfiguration.HistoryEntriesPerPage = ContentHistoryPagination.ClampPageSize(pageSize);
			EditorUtility.SetDirty(_contentConfiguration);
			AssetDatabase.SaveAssets();
			_historyPageIndex = 0;
			_historyEntriesScroll = Vector2.zero;
			_historyBucketAutoExpandKey = null;
			_historyDisplayRowsDirty = true;
			Repaint();
		}

		private void SetHistoryPage(int pageIndex)
		{
			_historyPageIndex = pageIndex;
			_historyEntriesScroll = Vector2.zero;
			_historyBucketAutoExpandKey = null;
			_historyDisplayRowsDirty = true;
		}

		private void DrawHistoryChanges()
		{
			EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField("Changes in selected publish", EditorStyles.boldLabel);
			if (string.IsNullOrEmpty(_selectedHistoryManifestUid))
			{
				EditorGUILayout.HelpBox("Select a published content history entry to inspect its changes.", MessageType.Info);
				EditorGUILayout.EndVertical();
				return;
			}

			EditorGUILayout.LabelField($"Manifest UID: {_selectedHistoryManifestUid}", EditorStyles.miniLabel);
			if (_isLoadingHistoryChanges)
			{
				EditorGUILayout.HelpBox("Loading historical content changes...", MessageType.Info);
			}
			else if (_historyChangesError != null)
			{
				DrawHistoryOperationError(_historyChangesError, RetryHistoryChanges);
			}
			else
			{
				DrawHistoryChangesTable();
			}

			DrawHistoryActions();
			EditorGUILayout.EndVertical();
		}

		private void DrawHistoryChangesTable()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			EditorGUILayout.LabelField("Change", GUILayout.Width(110));
			EditorGUILayout.LabelField("Content", GUILayout.ExpandWidth(true));
			EditorGUILayout.LabelField("Type", GUILayout.Width(110));
			EditorGUILayout.LabelField("Preview", GUILayout.Width(55));
			EditorGUILayout.EndHorizontal();

			var pageCount = ContentHistoryPagination.GetPageCount(_selectedHistoryChanges.Length, HistoryChangesPageSize);
			_historyChangesPageIndex = ContentHistoryPagination.ClampPageIndex(_historyChangesPageIndex, pageCount);
			var firstChangeIndex = _historyChangesPageIndex * HistoryChangesPageSize;
			var currentChangeCount = Math.Min(HistoryChangesPageSize, Math.Max(0, _selectedHistoryChanges.Length - firstChangeIndex));

			using (new EditorGUI.DisabledScope(IsHistoryRightPaneLocked()))
			{
				if (currentChangeCount == 0)
				{
					EditorGUILayout.HelpBox("This publish has no changed content items.", MessageType.Info);
				}
				else
				{
					DrawVirtualHistoryChanges(firstChangeIndex, currentChangeCount);
				}
				DrawHistoryChangesPagination(_selectedHistoryChanges.Length, pageCount);
			}

			if (_historyPreviewError != null)
			{
				DrawHistoryOperationError(_historyPreviewError, RetryHistoryPreview);
			}

			if (!string.IsNullOrEmpty(_selectedHistoryJson))
			{
				EditorGUILayout.LabelField("Historical JSON", EditorStyles.boldLabel);
				_historyPreviewScroll = EditorGUILayout.BeginScrollView(_historyPreviewScroll, GUILayout.Height(120));
				EditorGUILayout.TextArea(_selectedHistoryJson, GUILayout.ExpandHeight(true));
				EditorGUILayout.EndScrollView();
			}
		}

		private void DrawHistoryTruncatedLabel(Rect rect, string value)
		{
			value ??= string.Empty;
			GUI.Label(rect, value, _historyEllipsisLabelStyle);
			_historyEllipsisMeasureContent.text = value;
			if (!rect.Contains(Event.current.mousePosition) ||
				_historyEllipsisLabelStyle.CalcSize(_historyEllipsisMeasureContent).x <= rect.width)
			{
				return;
			}

			GUI.Label(rect, new GUIContent(string.Empty, value), GUIStyle.none);
		}

		private void DrawVirtualHistoryChanges(int firstChangeIndex, int changeCount)
		{
			var areaRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var contentRect = new Rect(0, 0, Math.Max(0, areaRect.width - 16), changeCount * HistoryChangeRowHeight);
			_historyChangesScroll = GUI.BeginScrollView(areaRect, _historyChangesScroll, contentRect, false, true);
			var visibleRange = ContentHistoryPagination.GetVisibleRange(changeCount, _historyChangesScroll.y, areaRect.height, HistoryChangeRowHeight);
			for (var index = visibleRange.FirstIndex; index < visibleRange.LastExclusive; index++)
			{
				DrawHistoryChange(_selectedHistoryChanges[firstChangeIndex + index],
					new Rect(0, index * HistoryChangeRowHeight, contentRect.width, HistoryChangeRowHeight));
			}
			GUI.EndScrollView();
		}

		private void DrawHistoryChange(BeamContentHistoryChangelistEntry entry, Rect rowRect)
		{
			GUI.Box(rowRect, GUIContent.none, EditorStyles.helpBox);
			var statusRect = new Rect(rowRect.x, rowRect.y, 110, rowRect.height);
			var previewRect = new Rect(rowRect.xMax - 55, rowRect.y + 1, 55, rowRect.height - 2);
			var typeRect = new Rect(previewRect.x - 110, rowRect.y, 110, rowRect.height);
			var contentRect = new Rect(statusRect.xMax, rowRect.y, Math.Max(0, typeRect.x - statusRect.xMax), rowRect.height);
			DrawHistoryChangeStatus(entry.ChangeStatus, statusRect);
			GUI.Label(contentRect, entry.FullId);
			GUI.Label(typeRect, entry.TypeName);
			using (new EditorGUI.DisabledScope(_isLoadingHistoryPreview || string.IsNullOrEmpty(entry.JsonFilePath)))
			{
				if (GUI.Button(previewRect, "View")) _ = LoadHistoryPreview(entry.FullId);
			}
		}

		private bool IsHistoryRightPaneLocked()
		{
			return _isLoadingHistoryChanges || _isRestoringHistory;
		}

		private void DrawHistoryChangesPagination(int changeCount, int pageCount)
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			var first = changeCount == 0 ? 0 : _historyChangesPageIndex * HistoryChangesPageSize + 1;
			var last = Math.Min(changeCount, (_historyChangesPageIndex + 1) * HistoryChangesPageSize);
			EditorGUILayout.LabelField($"{first}-{last} of {changeCount} changes", GUILayout.ExpandWidth(true));

			using (new EditorGUI.DisabledScope(_historyChangesPageIndex == 0))
			{
				if (GUILayout.Button("<", GUILayout.Width(24))) _historyChangesPageIndex--;
			}
			EditorGUILayout.LabelField($"{_historyChangesPageIndex + 1}/{Math.Max(pageCount, 1)}", GUILayout.Width(48));
			using (new EditorGUI.DisabledScope(_historyChangesPageIndex >= pageCount - 1))
			{
				if (GUILayout.Button(">", GUILayout.Width(24))) _historyChangesPageIndex++;
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawHistoryChangeStatus(int changeStatus, Rect rect)
		{
			var (label, icon, style) = ((ContentStatus)changeStatus) switch
			{
				ContentStatus.Created => ("Added", BeamGUI.iconStatusAdded, _historyAddedChangeStyle),
				ContentStatus.Modified => ("Modified", BeamGUI.iconStatusModified, _historyModifiedChangeStyle),
				ContentStatus.Deleted => ("Removed", BeamGUI.iconStatusDeleted, _historyRemovedChangeStyle),
				_ => ("Changed", BeamGUI.iconStatusModified, _historyDefaultChangeStyle)
			};

			var iconRect = new Rect(rect.x + 2, rect.y + 1, 16, 16);
			GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
			GUI.Label(new Rect(iconRect.xMax + 5, rect.y, rect.width - 23, rect.height), label, style);
		}

		private void DrawHistoryActions()
		{
			EditorGUILayout.BeginHorizontal();
			if (_isRestoringHistory)
			{
				DrawHistoryRestoreProgress();
			}
			else
			{
				using (new EditorGUI.DisabledScope(IsHistoryRightPaneLocked()))
				{
					if (GUILayout.Button("Copy manifest ID")) EditorGUIUtility.systemCopyBuffer = _selectedHistoryManifestUid;
					GUILayout.FlexibleSpace();
					if (DrawHistoryRestoreButton(!IsHistoryRightPaneLocked())) RestoreSelectedHistory();
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("Restores local files only — review and publish to update the realm.", EditorStyles.miniLabel);
			if (_historyRestoreError != null)
			{
				DrawHistoryOperationError(_historyRestoreError, RestoreSelectedHistory, () => _historyRestoreError = null);
			}
		}

		private bool DrawHistoryRestoreButton(bool isEnabled)
		{
			var content = new GUIContent("Restore changed files...");
			var rect = GUILayoutUtility.GetRect(content, GUI.skin.button, GUILayout.Width(172));
			var isHover = isEnabled && rect.Contains(Event.current.mousePosition);
			var isPressed = isHover && Event.current.type == EventType.MouseDown;
			var color = !isEnabled ? new Color(.28f, .12f, .1f) : isPressed
				? new Color(.42f, .08f, .06f)
				: isHover ? new Color(.78f, .2f, .16f) : new Color(.62f, .14f, .11f);
			EditorGUI.DrawRect(rect, color);
			if (isHover)
			{
				EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(1f, 1f, 1f, .45f));
			}
			GUI.Label(rect, content, _historyRestoreButtonStyle);
			return isEnabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
		}

		private void DrawHistoryOperationError(ContentHistoryOperationException error, Action retry, Action dismiss = null)
		{
			EditorGUILayout.HelpBox(error.UserMessage, MessageType.Error);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Retry")) retry?.Invoke();
			if (GUILayout.Button("Copy details")) EditorGUIUtility.systemCopyBuffer = error.Diagnostic;
			if (dismiss != null && GUILayout.Button("Dismiss")) dismiss();
			EditorGUILayout.EndHorizontal();
		}

		private void RetryContentHistory()
		{
			_contentService.RestartContentHistory();
			Repaint();
		}

		private void RetryHistoryChanges()
		{
			if (string.IsNullOrEmpty(_selectedHistoryManifestUid))
			{
				return;
			}

			var requestVersion = ++_historySelectionRequestVersion;
			_ = LoadHistoryChanges(_selectedHistoryManifestUid, requestVersion);
		}

		private void RetryHistoryPreview()
		{
			if (!string.IsNullOrEmpty(_historyPreviewContentId))
			{
				_ = LoadHistoryPreview(_historyPreviewContentId);
			}
		}

		private void EnsureHistoryStyles()
		{
			if (_historyAddedChangeStyle != null)
			{
				return;
			}

			_historyAddedChangeStyle = CreateHistoryChangeStyle(new Color(.45f, .8f, .3f));
			_historyModifiedChangeStyle = CreateHistoryChangeStyle(new Color(1f, .65f, .15f));
			_historyRemovedChangeStyle = CreateHistoryChangeStyle(new Color(1f, .35f, .3f));
			_historyDefaultChangeStyle = new GUIStyle(EditorStyles.label);
			_historyRestoreButtonStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = Color.white }
			};
			_historyEllipsisLabelStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				clipping = TextClipping.Ellipsis,
				wordWrap = false
			};
		}

		private static GUIStyle CreateHistoryChangeStyle(Color color)
		{
			var style = new GUIStyle(EditorStyles.label);
			style.normal.textColor = color;
			return style;
		}

		private static void DrawHistoryRestoreProgress()
		{
			var frame = (int)(EditorApplication.timeSinceStartup * 12) % 12;
			var spinner = EditorGUIUtility.IconContent($"WaitSpin{frame:00}").image ?? BeamGUI.iconRefresh;
			GUILayout.Label(new GUIContent(" Restoring local files...", spinner), EditorStyles.label);
			GUILayout.FlexibleSpace();
		}

		private void SelectHistoryEntry(string manifestUid)
		{
			if (_selectedHistoryManifestUid == manifestUid)
			{
				return;
			}

			_selectedHistoryManifestUid = manifestUid;
			_selectedHistoryChanges = Array.Empty<BeamContentHistoryChangelistEntry>();
			_selectedHistoryJson = string.Empty;
			_historyChangesPageIndex = 0;
			_historyChangesError = null;
			_historyPreviewError = null;
			_historyPreviewContentId = string.Empty;
			_historyPreviewRequestVersion++;
			_isLoadingHistoryPreview = false;
			_historyPreviewScroll = Vector2.zero;
			var requestVersion = ++_historySelectionRequestVersion;
			_ = LoadHistoryChanges(manifestUid, requestVersion);
		}

		private void ResetHistorySelection()
		{
			_historySelectionRequestVersion++;
			_historyPreviewRequestVersion++;
			_selectedHistoryManifestUid = string.Empty;
			_selectedHistoryChanges = Array.Empty<BeamContentHistoryChangelistEntry>();
			_selectedHistoryJson = string.Empty;
			_historyChangesPageIndex = 0;
			_historyChangesScroll = Vector2.zero;
			_historyPreviewScroll = Vector2.zero;
			_isLoadingHistoryChanges = false;
			_isLoadingHistoryPreview = false;
			_isRestoringHistory = false;
			_historyPreviewContentId = string.Empty;
			_historyChangesError = null;
			_historyPreviewError = null;
			_historyRestoreError = null;
		}

		private async Task LoadHistoryChanges(string manifestUid, int requestVersion)
		{
			_isLoadingHistoryChanges = true;
			_historyChangesError = null;
			Repaint();
			try
			{
				var changes = await _contentService.GetContentHistoryChanges(manifestUid);
				if (requestVersion == _historySelectionRequestVersion)
				{
					_selectedHistoryChanges = changes;
				}
			}
			catch (Exception exception)
			{
				if (requestVersion == _historySelectionRequestVersion)
				{
					_historyChangesError = ContentHistoryOperationException.FromException(
						"Load publish changes",
						"Unable to load changes for this publish. Check your connection and try again.",
						manifestUid,
						exception);
					Debug.LogError(_historyChangesError.Diagnostic);
				}
			}
			finally
			{
				if (requestVersion == _historySelectionRequestVersion)
				{
					_isLoadingHistoryChanges = false;
				}
				Repaint();
			}
		}

		private async Task LoadHistoryPreview(string contentId)
		{
			var manifestUid = _selectedHistoryManifestUid;
			var selectionRequestVersion = _historySelectionRequestVersion;
			var previewRequestVersion = ++_historyPreviewRequestVersion;
			_isLoadingHistoryPreview = true;
			_historyPreviewError = null;
			_historyPreviewContentId = contentId;
			Repaint();
			try
			{
				var historyJson = await _contentService.GetContentHistoryJson(manifestUid, contentId);
				if (selectionRequestVersion == _historySelectionRequestVersion && previewRequestVersion == _historyPreviewRequestVersion)
				{
					_selectedHistoryJson = historyJson;
				}
			}
			catch (Exception exception)
			{
				if (selectionRequestVersion == _historySelectionRequestVersion && previewRequestVersion == _historyPreviewRequestVersion)
				{
					_historyPreviewError = ContentHistoryOperationException.FromException(
						"Preview historical content",
						"Unable to load this historical content file. Check your connection and try again.",
						manifestUid,
						exception);
					Debug.LogError(_historyPreviewError.Diagnostic);
				}
			}
			finally
			{
				if (previewRequestVersion == _historyPreviewRequestVersion)
				{
					_isLoadingHistoryPreview = false;
				}
				Repaint();
			}
		}

		private void RestoreSelectedHistory()
		{
			if (_isRestoringHistory)
			{
				return;
			}

			if (!EditorUtility.DisplayDialog("Restore Changed Files",
					$"Restore local content files changed by publish {_selectedHistoryManifestUid}? This does not publish to the realm.",
					"Restore Local Files", "Cancel"))
			{
				return;
			}

			_ = RestoreSelectedHistoryAsync();
		}

		private async Task RestoreSelectedHistoryAsync()
		{
			_isRestoringHistory = true;
			_historyRestoreError = null;
			Repaint();
			try
			{
				await _contentService.RestoreContentHistory(_selectedHistoryManifestUid);
				ChangeWindowStatus(ContentWindowStatus.Normal);
			}
			catch (Exception exception)
			{
				_historyRestoreError = ContentHistoryOperationException.FromException(
					"Restore changed files",
					"Unable to restore changed files. Check your connection and try again.",
					_selectedHistoryManifestUid,
					exception);
				Debug.LogError(_historyRestoreError.Diagnostic);
			}
			finally
			{
				_isRestoringHistory = false;
				Repaint();
			}
		}

	}

	public enum ContentHistoryRowVisualState
	{
		Normal,
		Hovered,
		Selected
	}

	public static class ContentHistoryRowInteraction
	{
		public static ContentHistoryRowVisualState GetVisualState(bool isSelected, bool isHovered)
		{
			return isSelected
				? ContentHistoryRowVisualState.Selected
				: isHovered ? ContentHistoryRowVisualState.Hovered : ContentHistoryRowVisualState.Normal;
		}

		public static bool ShouldRepaint(string currentHoveredRowKey, string nextHoveredRowKey)
		{
			return !string.Equals(currentHoveredRowKey, nextHoveredRowKey, StringComparison.Ordinal);
		}
	}
}
