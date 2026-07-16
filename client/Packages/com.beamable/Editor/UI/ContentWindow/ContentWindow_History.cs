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
		private const float HistoryEntryRowHeight = 42f;
		private const float HistoryChangeRowHeight = 22f;
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
			var pageSize = ContentHistoryPagination.ClampPageSize(_contentConfiguration.HistoryEntriesPerPage);
			var pageCount = ContentHistoryPagination.GetPageCount(filteredEntries.Count, pageSize);
			_historyPageIndex = ContentHistoryPagination.ClampPageIndex(_historyPageIndex, pageCount);
			var firstEntryIndex = _historyPageIndex * pageSize;
			var currentEntryCount = Math.Min(pageSize, Math.Max(0, filteredEntries.Count - firstEntryIndex));

			if (_contentService.ContentHistoryWatcherError != null)
			{
				DrawHistoryOperationError(_contentService.ContentHistoryWatcherError, RetryContentHistory);
			}

			if (currentEntryCount == 0)
			{
				if (_contentService.ContentHistoryWatcherError == null)
				{
					EditorGUILayout.HelpBox("No published content history matches this search.", MessageType.Info);
				}
			}
			else
			{
				DrawVirtualHistoryEntries(filteredEntries, firstEntryIndex, currentEntryCount);
			}

			DrawHistoryPagination(filteredEntries.Count, pageSize, pageCount);
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

		private void DrawVirtualHistoryEntries(IReadOnlyList<BeamContentHistoryEntry> entries, int firstEntryIndex, int entryCount)
		{
			var areaRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var contentRect = new Rect(0, 0, Math.Max(0, areaRect.width - 16), entryCount * HistoryEntryRowHeight);
			_historyEntriesScroll = GUI.BeginScrollView(areaRect, _historyEntriesScroll, contentRect, false, true);
			var visibleRange = ContentHistoryPagination.GetVisibleRange(entryCount, _historyEntriesScroll.y, areaRect.height, HistoryEntryRowHeight);
			for (var index = visibleRange.FirstIndex; index < visibleRange.LastExclusive; index++)
			{
				DrawHistoryEntry(entries[firstEntryIndex + index], new Rect(0, index * HistoryEntryRowHeight, contentRect.width, HistoryEntryRowHeight));
			}
			GUI.EndScrollView();
		}

		private void DrawHistoryEntry(BeamContentHistoryEntry entry, Rect rowRect)
			{
			var isSelected = entry.ManifestUid == _selectedHistoryManifestUid;
			if (isSelected)
			{
				GUI.Box(rowRect, GUIContent.none, EditorStyles.selectionRect);
			}
			else
			{
				GUI.Box(rowRect, GUIContent.none, EditorStyles.helpBox);
			}

			var date = DateTimeOffset.FromUnixTimeMilliseconds(entry.CreatedDate).LocalDateTime;
			var primaryRect = new Rect(rowRect.x + 6, rowRect.y + 3, rowRect.width - 12, EditorGUIUtility.singleLineHeight);
			var secondaryRect = new Rect(primaryRect.x, primaryRect.yMax, primaryRect.width, EditorGUIUtility.singleLineHeight);
			GUI.Label(primaryRect, $"{date:g}   {entry.ManifestUid}   {entry.PublishedByName}");
			GUI.Label(secondaryRect, $"{entry.AffectedContentIds?.Length ?? 0} changed content item(s)", EditorStyles.miniLabel);

			using (new EditorGUI.DisabledScope(_isRestoringHistory))
			{
				if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
				{
					SelectHistoryEntry(entry.ManifestUid);
				}
			}
		}

		private void DrawHistoryPagination(int entryCount, int pageSize, int pageCount)
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			var first = entryCount == 0 ? 0 : _historyPageIndex * pageSize + 1;
			var last = Math.Min(entryCount, (_historyPageIndex + 1) * pageSize);
			EditorGUILayout.LabelField($"{first}-{last} of {entryCount} publishes", GUILayout.ExpandWidth(true));

			using (new EditorGUI.DisabledScope(_historyPageIndex == 0))
			{
				if (GUILayout.Button("<", GUILayout.Width(24))) _historyPageIndex--;
			}
			EditorGUILayout.LabelField($"{_historyPageIndex + 1}/{Math.Max(pageCount, 1)}", GUILayout.Width(48));
			using (new EditorGUI.DisabledScope(_historyPageIndex >= pageCount - 1))
			{
				if (GUILayout.Button(">", GUILayout.Width(24))) _historyPageIndex++;
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
			Repaint();
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
			if (isEnabled) EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
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
}
