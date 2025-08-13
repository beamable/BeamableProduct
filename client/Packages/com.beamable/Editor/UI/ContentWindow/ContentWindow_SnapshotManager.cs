using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI;
using Beamable.Editor.ThirdParty.Splitter;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const float SNAPSHOT_ICON_SIZE = 15f;
		public EditorGUISplitView snapshotSplitter;
		private Dictionary<string, BeamManifestSnapshotItem> _allSnapshots = new();
		private Dictionary<string, BeamManifestSnapshotItem> _sharedSnapshots = new();
		private Dictionary<string, BeamManifestSnapshotItem> _localSnapshots = new();
		private bool _gatheringSnapshots = false;
		private string _selectedSnapshot;
		private Vector2 _snapshotListScroll;
		private Vector2 _snapshotNewContentsScroll;
		private Vector2 _snapshotModifiedContentsScroll;
		private Vector2 _snapshotDeletedContentsScroll;
		private Vector2 _snapshotInfoScroll;
		private string _snapshotNameFieldValue = string.Empty;

		private void DrawSnapshotManager()
		{
			if(_gatheringSnapshots)
				return;
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			{
				if (snapshotSplitter == null)
				{
					var windowWidth = this.position.width;
					var startingWidthOfTypes = CONTENT_GROUP_PANEL_WIDTH;
					var normalizedWidth = startingWidthOfTypes / windowWidth;
					snapshotSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, normalizedWidth, 1f - normalizedWidth);

					// the first time the splitter gets created, the window needs to force redraw itself
					//  so that the splitter can size itself correctly. 
					EditorApplication.delayCall += Repaint;
				}
				snapshotSplitter.BeginSplitView();
				DrawSnapshots();
				snapshotSplitter.Split(this);
				DrawSnapshotContents();
				snapshotSplitter.EndSplitView();
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.Space(12);
			
			GUIStyle genericButtonStyle = BeamGUI.ColorizeButton(Color.gray);
			GUIStyle cancelBtnStyle = BeamGUI.ColorizeButton(new Color(1, .3f, .25f, 1));
			
			var buttonsRectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(40f)));
			buttonsRectController.ReserveWidth(BASE_PADDING);
			buttonsRectController.ReserveWidthFromRight(BASE_PADDING);
			buttonsRectController.ReserveHeight(BASE_PADDING);
			buttonsRectController.ReserveHeightFromBottom(BASE_PADDING);

			Rect snapshotNameRect = buttonsRectController.ReserveWidth(200);
			_snapshotNameFieldValue = EditorGUI.TextField(snapshotNameRect, _snapshotNameFieldValue, new GUIStyle(EditorStyles.textField)
			{
				alignment = TextAnchor.MiddleLeft
			});
			
			buttonsRectController.ReserveWidth(BASE_PADDING);
			bool hasValidField = !string.IsNullOrWhiteSpace(_snapshotNameFieldValue);
			bool isValidField = !Path.GetInvalidFileNameChars().Any(invalidChar => _snapshotNameFieldValue.Contains(invalidChar));
			
			var createLocal = new GUIContent("Create Local");
			var createLocalSize = GUI.skin.button.CalcSize(createLocal);
			Rect createLocalBtnRect = buttonsRectController.ReserveWidth(createLocalSize.x + BASE_PADDING * 2);
			bool existsInLocal = _localSnapshots.ContainsKey(_snapshotNameFieldValue);
			if (BeamGUI.ShowDisabled(hasValidField && !existsInLocal && isValidField, () => BeamGUI.CustomButton(createLocalBtnRect, createLocal, genericButtonStyle)))
			{
				_ = _contentService.TakeSnapshot(_snapshotNameFieldValue, true).Then(unit => { _ = CacheSnapshots(); });
			}
			
			buttonsRectController.ReserveWidth(BASE_PADDING);
			
			var createShared = new GUIContent("Create Shared");
			var createSharedSize = GUI.skin.button.CalcSize(createShared);
			Rect createSharedBtnRect = buttonsRectController.ReserveWidth(createSharedSize.x + BASE_PADDING * 2);
			bool existsInShared = _sharedSnapshots.ContainsKey(_snapshotNameFieldValue);
			if (BeamGUI.ShowDisabled(hasValidField && !existsInShared && isValidField, () => BeamGUI.CustomButton(createSharedBtnRect, createShared, genericButtonStyle)))
			{
				_ = _contentService.TakeSnapshot(_snapshotNameFieldValue, false).Then(unit => { _ = CacheSnapshots(); });
			}
			
			var hasSnapshot = !string.IsNullOrEmpty(_selectedSnapshot);
			
			var restoreSnapshotBtnContent = new GUIContent("Restore Snapshot");
			var restoreBtnSize = GUI.skin.button.CalcSize(restoreSnapshotBtnContent);
			Rect restoreBtnRect = buttonsRectController.ReserveWidthFromRight(restoreBtnSize.x + BASE_PADDING * 2);
			if (BeamGUI.ShowDisabled(hasSnapshot, () => BeamGUI.PrimaryButton(restoreBtnRect, restoreSnapshotBtnContent)))
			{
				if (_allSnapshots.TryGetValue(_selectedSnapshot, out var snapshotItem))
				{
					if (EditorUtility.DisplayDialog("Restore Snapshot",
					                                $"Are you sure you want to restore your local content to match the snapshot {snapshotItem.Name}? This will delete all your local content.",
					                                "Restore", "Cancel"))
					{
						_ = _contentService.RestoreSnapshot(snapshotItem.Path).Then(unit => { _ = CacheSnapshots(); });
					}
				}

				_selectedSnapshot = string.Empty;
			}

			buttonsRectController.ReserveWidthFromRight(BASE_PADDING);
			
			var deleteSnapshot = new GUIContent("Delete Snapshot");
			var deleteSnapshotSize = GUI.skin.button.CalcSize(deleteSnapshot);
			Rect deleteBtnRect = buttonsRectController.ReserveWidthFromRight(deleteSnapshotSize.x + BASE_PADDING * 2);
			if (BeamGUI.ShowDisabled(hasSnapshot, () => BeamGUI.CustomButton(deleteBtnRect, deleteSnapshot, cancelBtnStyle)))
			{
				if (_allSnapshots.TryGetValue(_selectedSnapshot, out var snapshotItem))
				{
					if (EditorUtility.DisplayDialog("Delete Snapshot", $"Are you sure you want to delete the snapshot {snapshotItem.Name}?",
					                                "Delete", "Cancel"))
					{
						File.Delete(snapshotItem.Path);
						_ = CacheSnapshots();
					}
				}
				_selectedSnapshot = string.Empty;

			}

			EditorGUILayout.EndVertical();
		}

		private void DrawSnapshots()
		{
			BeamManifestSnapshotItem manifestWithBiggerName = _allSnapshots.Values.OrderByDescending(item => item.Name.Length).First();
			float biggerNameSize = EditorStyles.label.CalcSize(new GUIContent(manifestWithBiggerName.Name)).x;
			float snapshotMinWidth = Mathf.Max(biggerNameSize + INDENT_WIDTH + BASE_PADDING * 4 + SNAPSHOT_ICON_SIZE, snapshotSplitter.cellNormalizedSizes[0] * EditorGUIUtility.currentViewWidth);
			_snapshotListScroll = EditorGUILayout.BeginScrollView(_snapshotListScroll, GUILayout.MinWidth(snapshotMinWidth), GUILayout.ExpandWidth(true));
			var rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			rectController.ReserveWidth(BASE_PADDING);
			EditorGUI.LabelField(rectController.rect, "Local Snapshots");
			DrawSnapshotsList(_localSnapshots.Values.ToList());
			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			rectController.ReserveWidth(BASE_PADDING);
			EditorGUI.LabelField(rectController.rect, "Shared Snapshots");
			DrawSnapshotsList(_sharedSnapshots.Values.ToList());
			EditorGUILayout.EndScrollView();
		}

		private void DrawSnapshotsList(List<BeamManifestSnapshotItem> snapshotItems)
		{
			foreach (var snapshotItem in snapshotItems)
			{
				string displayName = snapshotItem.Name;

				bool isSelected = _selectedSnapshot == snapshotItem.Path;
				GUIStyle rowStyle = isSelected ? EditorStyles.selectionRect : EditorStyles.label;

				Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));
				rowRect.xMin += INDENT_WIDTH;
				
				if (isSelected)
				{
					GUI.Box(rowRect, GUIContent.none, rowStyle ?? EditorStyles.label);
				}
				
				Rect contentRect = new Rect(rowRect);
				
				contentRect.xMin += BASE_PADDING;
				
				Texture texture = BeamGUI.iconContentSnapshotColor;
				var iconRect = new Rect(contentRect.x, contentRect.center.y - SNAPSHOT_ICON_SIZE/2f, SNAPSHOT_ICON_SIZE, SNAPSHOT_ICON_SIZE);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);
				
				contentRect.xMin += SNAPSHOT_ICON_SIZE + BASE_PADDING;
				
				GUI.Label(contentRect, displayName);

				if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition) && Event.current.button == 0)
				{
					if (_selectedSnapshot == snapshotItem.Path)
					{
						_selectedSnapshot = string.Empty;
						return;
					}
					
					_selectedSnapshot =  snapshotItem.Path;
					Event.current.Use();
					GUI.changed = true;
				}
			}
		}

		private void DrawSnapshotContents()
		{
			if (string.IsNullOrEmpty(_selectedSnapshot))
			{
				EditorGUILayout.LabelField("No Snapshot Selected");
				return;
			}
			
			if (!_allSnapshots.TryGetValue(_selectedSnapshot, out var snapshot))
			{
				_selectedSnapshot = string.Empty;
				return;
			}
			
			GUIStyle boldLabel = new GUIStyle(EditorStyles.boldLabel)
			{
				padding = new RectOffset(BASE_PADDING, BASE_PADDING, 0,0)
			};

			var snapshotFullNameSize = boldLabel.CalcSize(new GUIContent($"Snapshot Name: {snapshot.Name}")).x + BASE_PADDING * 3;
			var contentWithLongestName = snapshot.Contents.OrderByDescending(item => item.Name.Length).First();
			var longestNameSize = EditorStyles.label.CalcSize(new GUIContent(contentWithLongestName.Name)).x + INDENT_WIDTH + SNAPSHOT_ICON_SIZE + BASE_PADDING * 4;
			var contentMinSize = Mathf.Max(snapshotFullNameSize, longestNameSize, snapshotSplitter.cellNormalizedSizes[1]* EditorGUIUtility.currentViewWidth);
			
			
			
			EditorGUILayout.BeginHorizontal();
			_snapshotInfoScroll = EditorGUILayout.BeginScrollView(_snapshotInfoScroll, GUILayout.MinWidth(contentMinSize));
			
			EditorGUILayout.BeginVertical();
			var rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			var snapshotContent = new GUIContent("Snapshot Name: ");
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(snapshotContent).x), snapshotContent, boldLabel);
			EditorGUI.LabelField(rectController.rect, snapshot.Name);
			
			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			var timestampLabel = new GUIContent("Timestamp: ");
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(timestampLabel).x), timestampLabel, boldLabel);
			EditorGUI.LabelField(rectController.rect, DateTimeOffset.FromUnixTimeMilliseconds(snapshot.SavedTimestamp).ToString("g"));
			
			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			var manifestLabel = new GUIContent("Manifest ID: ");
			var manifestValue = new GUIContent($"{snapshot.ManifestId} | ");
			var pidLabel = new GUIContent("PID: ");
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(manifestLabel).x), manifestLabel, boldLabel);
			EditorGUI.LabelField(rectController.ReserveWidth(EditorStyles.label.CalcSize(manifestValue).x), manifestValue, EditorStyles.label);
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(pidLabel).x), pidLabel, boldLabel);
			EditorGUI.LabelField(rectController.rect, snapshot.Pid);
			
			var newContents = snapshot.Contents.Where(item => item.CurrentStatus == (int)ContentStatus.Created).ToList();
			var modifiedContents = snapshot.Contents.Where(item => item.CurrentStatus == (int)ContentStatus.Modified).ToList();
			var deletedContents = snapshot.Contents.Where(item => item.CurrentStatus == (int)ContentStatus.Deleted).ToList();

			if (newContents.Count > 0)
			{
				EditorGUILayout.LabelField("Contents that will be locally added:", boldLabel);
				var newVisHeight = Mathf.Min(300, (int)EditorGUIUtility.singleLineHeight * newContents.Count);
				BeamCliWindow.DrawVirtualScroller((int)EditorGUIUtility.singleLineHeight, newContents.Count, ref _snapshotNewContentsScroll,
				                                  (index, rect) =>
				                                  {
					                                  var snapshotItem = newContents[index];
					                                  rectController = new EditorGUIRectController(rect);
					                                  rectController.ReserveWidth(BASE_PADDING + INDENT_WIDTH);
					                                  EditorGUI.LabelField(rectController.rect, snapshotItem.Name);
				                                  }, newVisHeight);

				EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
			}

			if (modifiedContents.Count > 0)
			{
				EditorGUILayout.LabelField("Contents that will be locally modified:", boldLabel);
				var modifiedVisHeight = Mathf.Min(300, (int)EditorGUIUtility.singleLineHeight * modifiedContents.Count);
				BeamCliWindow.DrawVirtualScroller((int)EditorGUIUtility.singleLineHeight, modifiedContents.Count, ref _snapshotModifiedContentsScroll,
				                                  (index, rect) =>
				                                  {
					                                  var snapshotItem = modifiedContents[index];
					                                  rectController = new EditorGUIRectController(rect);
					                                  rectController.ReserveWidth(BASE_PADDING + INDENT_WIDTH);
					                                  EditorGUI.LabelField(rectController.rect, snapshotItem.Name);
				                                  }, modifiedVisHeight);

				EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
			}

			if (deletedContents.Count > 0)
			{
				EditorGUILayout.LabelField("Contents that will be locally deleted:", boldLabel);
				var deletedVisHeight = Mathf.Min(300, (int)EditorGUIUtility.singleLineHeight * deletedContents.Count);
				BeamCliWindow.DrawVirtualScroller((int)EditorGUIUtility.singleLineHeight, deletedContents.Count, ref _snapshotDeletedContentsScroll,
				                                  (index, rect) =>
				                                  {
					                                  var snapshotItem = deletedContents[index];
					                                  rectController = new EditorGUIRectController(rect);
					                                  rectController.ReserveWidth(BASE_PADDING + INDENT_WIDTH);
					                                  EditorGUI.LabelField(rectController.rect, snapshotItem.Name);
				                                  }, deletedVisHeight);
			}

			if (newContents.Count == 0 && modifiedContents.Count == 0 && deletedContents.Count == 0)
			{
				EditorGUILayout.LabelField("Snapshot matches all your local changes, so not change will be made");
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
			
		}

		private async Task CacheSnapshots()
		{
			_gatheringSnapshots = true;
			var snapshotListResult = await _contentService.GetContentSnapshots();
			_sharedSnapshots = snapshotListResult.SharedSnapshots.ToDictionary(item => item.Name, item => item);
			_localSnapshots = snapshotListResult.LocalSnapshots.ToDictionary(item => item.Name, item => item);
			_allSnapshots.Clear();
			_sharedSnapshots.Values.ToList().ForEach(snapshot => _allSnapshots.Add(snapshot.Path, snapshot));
			_localSnapshots.Values.ToList().ForEach(snapshot => _allSnapshots.Add(snapshot.Path, snapshot));
			_gatheringSnapshots = false;
		}
	}
}
