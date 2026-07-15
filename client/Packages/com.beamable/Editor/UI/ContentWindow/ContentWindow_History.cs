using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
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
		private bool _isLoadingHistoryChanges;
		private bool _isLoadingHistoryPreview;
		private bool _isRestoringHistory;
		private string _historyRestoreError;

		private void DrawContentHistory()
		{
			EnsureHistorySearchData();
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

			_historySearchData = new SearchData { onEndCheck = () => _historyPageIndex = 0 };
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
			var currentEntries = filteredEntries.Skip(_historyPageIndex * pageSize).Take(pageSize).ToList();

			_historyEntriesScroll = EditorGUILayout.BeginScrollView(_historyEntriesScroll, GUILayout.ExpandHeight(true));
			if (currentEntries.Count == 0)
			{
				EditorGUILayout.HelpBox("No published content history matches this search.", MessageType.Info);
			}
			else
			{
				foreach (var entry in currentEntries)
				{
					DrawHistoryEntry(entry);
				}
			}
			EditorGUILayout.EndScrollView();

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

		private List<BeamContentHistoryEntry> GetFilteredHistoryEntries()
		{
			var search = _historySearchData?.searchText?.Trim();
			var entries = _contentService.ContentHistoryEntries;
			if (string.IsNullOrEmpty(search))
			{
				return entries.ToList();
			}

			return entries.Where(entry =>
				(entry.ManifestUid?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
				(entry.PublishedBy?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
				(entry.PublishedByName?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
				.ToList();
		}

		private void DrawHistoryEntry(BeamContentHistoryEntry entry)
		{
			var isSelected = entry.ManifestUid == _selectedHistoryManifestUid;
			var rowRect = EditorGUILayout.GetControlRect(false, 42);
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
			EditorGUILayout.LabelField("Change", GUILayout.Width(88));
			EditorGUILayout.LabelField("Content", GUILayout.ExpandWidth(true));
			EditorGUILayout.LabelField("Type", GUILayout.Width(110));
			EditorGUILayout.LabelField("Preview", GUILayout.Width(55));
			EditorGUILayout.EndHorizontal();

			var pageCount = ContentHistoryPagination.GetPageCount(_selectedHistoryChanges.Length, HistoryChangesPageSize);
			_historyChangesPageIndex = ContentHistoryPagination.ClampPageIndex(_historyChangesPageIndex, pageCount);
			var currentChanges = _selectedHistoryChanges
				.Skip(_historyChangesPageIndex * HistoryChangesPageSize)
				.Take(HistoryChangesPageSize);

			using (new EditorGUI.DisabledScope(IsHistoryRightPaneLocked()))
			{
				_historyChangesScroll = EditorGUILayout.BeginScrollView(_historyChangesScroll, GUILayout.ExpandHeight(true));
				foreach (var entry in currentChanges)
				{
					EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
					DrawHistoryChangeStatus(entry.ChangeStatus);
					EditorGUILayout.LabelField(entry.FullId, GUILayout.ExpandWidth(true));
					EditorGUILayout.LabelField(entry.TypeName, GUILayout.Width(110));
					using (new EditorGUI.DisabledScope(_isLoadingHistoryPreview || string.IsNullOrEmpty(entry.JsonFilePath)))
					{
						if (GUILayout.Button("View", GUILayout.Width(55))) _ = LoadHistoryPreview(entry.FullId);
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndScrollView();
				DrawHistoryChangesPagination(_selectedHistoryChanges.Length, pageCount);
			}

			if (!string.IsNullOrEmpty(_selectedHistoryJson))
			{
				EditorGUILayout.LabelField("Historical JSON", EditorStyles.boldLabel);
				_historyPreviewScroll = EditorGUILayout.BeginScrollView(_historyPreviewScroll, GUILayout.Height(120));
				EditorGUILayout.TextArea(_selectedHistoryJson, GUILayout.ExpandHeight(true));
				EditorGUILayout.EndScrollView();
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

		private static void DrawHistoryChangeStatus(int changeStatus)
		{
			var (label, icon, color) = ((ContentStatus)changeStatus) switch
			{
				ContentStatus.Created => ("Added", BeamGUI.iconStatusAdded, new Color(.45f, .8f, .3f)),
				ContentStatus.Modified => ("Modified", BeamGUI.iconStatusModified, new Color(1f, .65f, .15f)),
				ContentStatus.Deleted => ("Removed", BeamGUI.iconStatusDeleted, new Color(1f, .35f, .3f)),
				_ => ("Changed", BeamGUI.iconStatusModified, EditorStyles.label.normal.textColor)
			};

			var rect = GUILayoutUtility.GetRect(110, EditorGUIUtility.singleLineHeight, GUILayout.Width(110));
			var iconRect = new Rect(rect.x + 2, rect.y + 1, 16, 16);
			GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
			var labelStyle = new GUIStyle(EditorStyles.label);
			labelStyle.normal.textColor = color;
			GUI.Label(new Rect(iconRect.xMax + 5, rect.y, rect.width - 23, rect.height), label, labelStyle);
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
			if (!string.IsNullOrEmpty(_historyRestoreError))
			{
				EditorGUILayout.HelpBox(_historyRestoreError, MessageType.Error);
				if (GUILayout.Button("Dismiss error")) _historyRestoreError = string.Empty;
			}
		}

		private static bool DrawHistoryRestoreButton(bool isEnabled)
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
			GUI.Label(rect, content, new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = Color.white }
			});
			return isEnabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
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
			var requestVersion = ++_historySelectionRequestVersion;
			_ = LoadHistoryChanges(manifestUid, requestVersion);
		}

		private void ResetHistorySelection()
		{
			_historySelectionRequestVersion++;
			_selectedHistoryManifestUid = string.Empty;
			_selectedHistoryChanges = Array.Empty<BeamContentHistoryChangelistEntry>();
			_selectedHistoryJson = string.Empty;
			_historyChangesPageIndex = 0;
			_historyChangesScroll = Vector2.zero;
			_historyPreviewScroll = Vector2.zero;
			_isLoadingHistoryChanges = false;
			_isLoadingHistoryPreview = false;
			_isRestoringHistory = false;
			_historyRestoreError = string.Empty;
		}

		private async Task LoadHistoryChanges(string manifestUid, int requestVersion)
		{
			_isLoadingHistoryChanges = true;
			Repaint();
			try
			{
				var changes = await _contentService.GetContentHistoryChanges(manifestUid);
				if (requestVersion == _historySelectionRequestVersion)
				{
					_selectedHistoryChanges = changes;
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
			_isLoadingHistoryPreview = true;
			Repaint();
			try
			{
				_selectedHistoryJson = await _contentService.GetContentHistoryJson(_selectedHistoryManifestUid, contentId);
			}
			finally
			{
				_isLoadingHistoryPreview = false;
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
			_historyRestoreError = string.Empty;
			Repaint();
			try
			{
				await _contentService.RestoreContentHistory(_selectedHistoryManifestUid);
				ChangeWindowStatus(ContentWindowStatus.Normal);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				_historyRestoreError = $"Restore failed. Check your connection and try again. {exception.Message}";
			}
			finally
			{
				_isRestoringHistory = false;
				Repaint();
			}
		}

		private static string GetChangeLabel(int changeStatus)
		{
			return ((ContentStatus)changeStatus) switch
			{
				ContentStatus.Created => "Added",
				ContentStatus.Modified => "Modified",
				ContentStatus.Deleted => "Removed",
				_ => "Changed"
			};
		}
	}
}
