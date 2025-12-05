using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.ThirdParty.Splitter;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const float SNAPSHOT_ICON_SIZE = 15f;
		private EditorGUISplitView _snapshotSplitter;
		private Dictionary<string, BeamManifestSnapshotItem> _allSnapshots = new();
		private Dictionary<string, BeamManifestSnapshotItem> _sharedSnapshots = new();
		private Dictionary<string, BeamManifestSnapshotItem> _localSnapshots = new();
		private bool _gatheringSnapshots = false;
		private readonly List<string> _selectedSnapshotsPaths = new();
		private Vector2 _snapshotListScroll;
		private Vector2 _snapshotNewContentsScroll;
		private Vector2 _snapshotModifiedContentsScroll;
		private Vector2 _snapshotDeletedContentsScroll;
		private Vector2 _snapshotInfoScroll;
		private string _snapshotNameFieldValue = string.Empty;
		
		private bool _localSnapshotFoldout = true;
		private bool _sharedSnapshotFoldout = true;
		private SearchData _snapshotSearchData;
		private ContentSnapshotType? _currentSnapshotCreationMode;
		private bool _mustSetControl = false;
		private bool _isAdditiveRestore;
		private bool _deleteAfterRestore;

		private void DrawSnapshotManager()
		{
			if(_gatheringSnapshots)
				return;
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			{
				if (_snapshotSplitter == null)
				{
					var windowWidth = this.position.width;
					var startingWidthOfTypes = CONTENT_GROUP_PANEL_WIDTH;
					var normalizedWidth = startingWidthOfTypes / windowWidth;
					_snapshotSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, normalizedWidth, 1f - normalizedWidth);

					// the first time the splitter gets created, the window needs to force redraw itself
					//  so that the splitter can size itself correctly. 
					EditorApplication.delayCall += Repaint;
				}

				if (_snapshotSplitter.cellCount < 2)
				{
					_snapshotSplitter = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal, 2);
				}

				
				_snapshotSplitter.BeginSplitView();
				DrawSnapshots();
				_snapshotSplitter.Split(this);
				DrawSnapshotContents();
				_snapshotSplitter.EndSplitView();
			}
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.Space(12);
			
			
			var buttonsRectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(40f)));
			buttonsRectController.ReserveWidth(BASE_PADDING);
			buttonsRectController.ReserveWidthFromRight(BASE_PADDING);
			buttonsRectController.ReserveHeight(BASE_PADDING);
			buttonsRectController.ReserveHeightFromBottom(BASE_PADDING);

			var hasSingleSnapshot = _selectedSnapshotsPaths.Count == 1;
			var restoreSnapshotBtnContent = new GUIContent("Restore Snapshot");
			var restoreBtnSize = GUI.skin.button.CalcSize(restoreSnapshotBtnContent);
			Rect restoreBtnRect = buttonsRectController.ReserveWidthFromRight(restoreBtnSize.x + BASE_PADDING * 2);
			if (BeamGUI.ShowDisabled(hasSingleSnapshot, () => BeamGUI.PrimaryButton(restoreBtnRect, restoreSnapshotBtnContent)))
			{
				RestoreSnapshot(_selectedSnapshotsPaths[0]);

				_selectedSnapshotsPaths.Clear();
			}

			buttonsRectController.ReserveWidthFromRight(BASE_PADDING * 2);
			var additiveRestoreContent = new GUIContent("Additive Restore", "When enabled, this will restore values additively instead of replacing them completely.");
			var additiveRestoreToggleSize = GUI.skin.toggle.CalcSize(additiveRestoreContent);
			Rect additiveRestoreToggleRect = buttonsRectController.ReserveWidthFromRight(additiveRestoreToggleSize.x);
			_isAdditiveRestore = EditorGUI.ToggleLeft(additiveRestoreToggleRect, additiveRestoreContent, _isAdditiveRestore);
			
			buttonsRectController.ReserveWidthFromRight(BASE_PADDING * 2);
			var deleteAfterRestoreContent = new GUIContent("Delete After Restore", "When enabled, this will delete the snapshot right after restore.");
			var deleteAfterRestoreToggleSize = GUI.skin.toggle.CalcSize(deleteAfterRestoreContent);
			Rect deleteAfterRestoreRect = buttonsRectController.ReserveWidthFromRight(deleteAfterRestoreToggleSize.x);
			_deleteAfterRestore = EditorGUI.ToggleLeft(deleteAfterRestoreRect, deleteAfterRestoreContent, _deleteAfterRestore);

			EditorGUILayout.EndVertical();
		}

		private void RestoreSnapshot(string snapshotPath)
		{
			if (_allSnapshots.TryGetValue(snapshotPath, out var snapshotItem))
			{
				if (EditorUtility.DisplayDialog("Restore Snapshot",
				                                $"Are you sure you want to restore your local content to match the snapshot {snapshotItem.Name}? This will delete all your local content.",
				                                "Restore", "Cancel"))
				{
					_ = _contentService.RestoreSnapshot(snapshotItem.Path, _isAdditiveRestore, _deleteAfterRestore).Then(unit => { _ = CacheSnapshots(); });
				}
			}
		}

		private void DrawSnapshots()
		{
			BeamManifestSnapshotItem manifestWithBiggerName = _allSnapshots.Values.Where(FilterSnapshot).OrderByDescending(item => item.Name.Length).FirstOrDefault();
			float snapshotListAreaWidth = _snapshotSplitter.cellNormalizedSizes[0] * EditorGUIUtility.currentViewWidth;
			float biggerNameSize = manifestWithBiggerName != null
				? EditorStyles.label.CalcSize(new GUIContent(manifestWithBiggerName.Name)).x
				: snapshotListAreaWidth;
			List<GUILayoutOption> scrollOptions = new List<GUILayoutOption>() {GUILayout.ExpandWidth(true)};
			if (biggerNameSize > snapshotListAreaWidth)
			{
				scrollOptions.Add(GUILayout.MinWidth(biggerNameSize));
			}
			_snapshotListScroll = EditorGUILayout.BeginScrollView(_snapshotListScroll, scrollOptions.ToArray());
			
			EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
			EditorGUILayout.Space(BASE_PADDING, false);
			this.DrawSearchBar(_snapshotSearchData);
			EditorGUILayout.Space(BASE_PADDING, false);
			EditorGUILayout.EndHorizontal();
			
			var rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(snapshotListAreaWidth)));
			rectController.ReserveWidth(BASE_PADDING);
			rectController.ReserveWidthFromRight(BASE_PADDING);
			
			float buttonSize = EditorGUIUtility.singleLineHeight;
			var newLocalRect = rectController.ReserveWidthFromRight(buttonSize);
			if (GUI.Button(new Rect(newLocalRect.x, newLocalRect.center.y - buttonSize/2f, buttonSize, buttonSize), BeamGUI.iconPlus, EditorStyles.iconButton))
			{
				SetSnapshotCreationMode(ContentSnapshotType.Local);
			}
			
			_localSnapshotFoldout = EditorGUI.Foldout(rectController.rect, _localSnapshotFoldout, "Local Snapshots", true);
			if (_localSnapshotFoldout || _currentSnapshotCreationMode is ContentSnapshotType.Local)
			{
				DrawSnapshotsList(_localSnapshots.Values.ToList(), ContentSnapshotType.Local, true);
			}

			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(snapshotListAreaWidth)));
			rectController.ReserveWidth(BASE_PADDING);
			rectController.ReserveWidthFromRight(BASE_PADDING);
			
			var newSharedRect = rectController.ReserveWidthFromRight(buttonSize);
			if (GUI.Button(new Rect(newSharedRect.x, newSharedRect.center.y - buttonSize/2f, buttonSize, buttonSize), BeamGUI.iconPlus, EditorStyles.iconButton))
			{
				SetSnapshotCreationMode(ContentSnapshotType.Shared);
			}
			
			_sharedSnapshotFoldout = EditorGUI.Foldout(rectController.rect, _sharedSnapshotFoldout, "Shared Snapshots", true);
			if (_sharedSnapshotFoldout || _currentSnapshotCreationMode is ContentSnapshotType.Shared)
			{
				DrawSnapshotsList(_sharedSnapshots.Values.ToList(), ContentSnapshotType.Shared);
			}

			EditorGUILayout.EndScrollView();
		}

		private void SetSnapshotCreationMode(ContentSnapshotType snapshotType)
		{
			_mustSetControl = true;
			_currentSnapshotCreationMode = snapshotType;
			_snapshotNameFieldValue = string.Empty;
		}

		private void FinishSnapshotCreationMode(bool shouldCreate)
		{
			if (shouldCreate && _currentSnapshotCreationMode.HasValue)
			{
				_gatheringSnapshots = true;
				_contentService.TakeSnapshot(_snapshotNameFieldValue, _currentSnapshotCreationMode.Value == ContentSnapshotType.Local).Then(unit => _ = CacheSnapshots());
			}
			_currentSnapshotCreationMode = null;
		}

		private void DrawSnapshotsList(List<BeamManifestSnapshotItem> snapshotItems, ContentSnapshotType snapshotType, bool splitAutoGenerated = false)
		{
			var filteredList = snapshotItems.Where(FilterSnapshot).ToList();
			
			List<BeamManifestSnapshotItem> firstList = new();
			List<BeamManifestSnapshotItem> secondsList = new();
			if (splitAutoGenerated)
			{
				firstList.AddRange(filteredList.Where(item => !item.IsAutoSnapshot));
				secondsList.AddRange(filteredList.Where(item => item.IsAutoSnapshot));
			}
			else
			{
				firstList.AddRange(filteredList);
			}
			
			firstList.ForEach(DrawItem);

			if (secondsList.Count > 0)
			{
				BeamGUI.DrawHorizontalSeparatorLine(new RectOffset(BASE_PADDING * 4, BASE_PADDING * 4, BASE_PADDING, BASE_PADDING), new Color(0.15f, 0.15f, 0.15f));
				secondsList.ForEach(DrawItem);
			}
			
			if (_currentSnapshotCreationMode == snapshotType)
			{
				Rect fieldRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));
				fieldRect.xMin += INDENT_WIDTH;
				fieldRect.width -= INDENT_WIDTH;
				GUI.SetNextControlName("NewSnapshotField");
				_snapshotNameFieldValue = EditorGUI.TextField(fieldRect, _snapshotNameFieldValue, new GUIStyle(EditorStyles.textField)
				{
					alignment = TextAnchor.MiddleLeft
				});

				if (_mustSetControl)
				{
					GUI.FocusControl("NewSnapshotField");
					_mustSetControl = false;
				}

				if ((Event.current.type == EventType.MouseDown && !fieldRect.Contains(Event.current.mousePosition)) ||
				    Event.current.isKey && Event.current.keyCode == KeyCode.Return)
				{
					FinishSnapshotCreationMode(true);
				}

				if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
				{
					FinishSnapshotCreationMode(false);
				}
			}

			return;

			void DrawItem(BeamManifestSnapshotItem snapshotItem)
			{
				string displayName = snapshotItem.Name;

				bool isSelected = _selectedSnapshotsPaths.Contains(snapshotItem.Path);
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

				if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
				{
					if (Event.current.button == 0)
					{
						bool isAddClick = (Event.current.control || Event.current.alt || Event.current.command);
						bool isShiftClick = Event.current.shift;
						if (!isShiftClick)
						{
							if (isAddClick)
							{
								// Either adding or removing from selected
								if (_selectedSnapshotsPaths.Contains(snapshotItem.Path))
								{
									_selectedSnapshotsPaths.Remove(snapshotItem.Path);
								}
								else
								{
									_selectedSnapshotsPaths.Add(snapshotItem.Path);
								}
							}
							else
							{
								_selectedSnapshotsPaths.Clear();
								_selectedSnapshotsPaths.Add(snapshotItem.Path);
							}
						}
						else
						{
							if (_selectedSnapshotsPaths.Count == 0)
							{
								_selectedSnapshotsPaths.Add(snapshotItem.Path);
							}
							else
							{
								var allSnapshots = _allSnapshots.Values.Where(FilterSnapshot).ToList();
								int indexOfFirst = allSnapshots.FindIndex(item => item.Path == _selectedSnapshotsPaths[0]);
								int indexOfCurrent = allSnapshots.FindIndex(item => item.Path == snapshotItem.Path);
								if (indexOfFirst == -1 || indexOfCurrent == -1)
								{
									string firstSelectedName = Path.GetFileNameWithoutExtension(_selectedSnapshotsPaths[0]);
									_selectedSnapshotsPaths.Clear();
									Debug.LogError($"Beam Content Manager cannot find index map between snapshots=[{firstSelectedName}:{snapshotItem.Name}] as index=[{indexOfFirst}:{indexOfCurrent}] ");
								}
								else
								{
									_selectedSnapshotsPaths.Clear();
									int min = Math.Min(indexOfFirst, indexOfCurrent);
									int max = Math.Max(indexOfFirst, indexOfCurrent);
									for (int i = min; i <= max; i++)
									{
										_selectedSnapshotsPaths.Add(allSnapshots[i].Path);
									}
								}
							}
						}
						
					}
					else if (Event.current.button == 1)
					{
						var hasMultipleSelected = _selectedSnapshotsPaths.Count > 1;
						var snapshots = _selectedSnapshotsPaths.Count == 0 ? new List<string>() {snapshotItem.Path} : _selectedSnapshotsPaths;
						GenericMenu rightClickMenu = new GenericMenu();
						var restoreSnapshotContent = new GUIContent("Restore Snapshot");
						if (hasMultipleSelected)
						{
							rightClickMenu.AddDisabledItem(restoreSnapshotContent);
						}
						else
						{
							rightClickMenu.AddItem(restoreSnapshotContent, false, () =>
							{
								RestoreSnapshot(snapshotItem.Path);
							});
						}

						rightClickMenu.AddItem(new GUIContent("Delete Snapshot"), false, () =>
						{
							string fileNames = string.Join(", ", snapshots.Where(File.Exists).Select(Path.GetFileNameWithoutExtension));
							string snapshotText = snapshots.Count == 1 ? "snapshot" : "snapshots";
							if (EditorUtility.DisplayDialog("Delete Snapshot", $"Are you sure you want to delete your {snapshotText} {fileNames}? ",
							                                "Delete", "Cancel"))
							{
								snapshots.ForEach(File.Delete);
								_ = CacheSnapshots();
							}

						});
						rightClickMenu.AddSeparator("");
						rightClickMenu.AddItem(new GUIContent("Open File"), false, () =>
						{
							foreach (string snapshotPath in snapshots.Where(File.Exists))
							{
								InternalEditorUtility.OpenFileAtLineExternal(snapshotPath, 1);
							}
						});
						rightClickMenu.ShowAsContext();
						
					}
					Event.current.Use();
				}
			}
		}

		private bool FilterSnapshot(BeamManifestSnapshotItem item)
		{
			return _snapshotSearchData == null ||
			       string.IsNullOrEmpty(_snapshotSearchData.searchText) || 
			       item.Name.Contains(_snapshotSearchData.searchText, StringComparison.InvariantCultureIgnoreCase);
		}

		private void DrawSnapshotContents()
		{
			if (_selectedSnapshotsPaths.Count == 0)
			{
				EditorGUILayout.LabelField("No Snapshot Selected");
				return;
			}
			
			if (_selectedSnapshotsPaths.Count > 1)
			{
				EditorGUILayout.LabelField("Multiple snapshot selected, cannot show preview or neither restore multiples. Please, select only one.");
				return;
			}
			if (!_allSnapshots.TryGetValue(_selectedSnapshotsPaths[0], out var snapshot))
			{
				_selectedSnapshotsPaths.Clear();
				return;
			}
			
			GUIStyle boldLabel = new GUIStyle(EditorStyles.boldLabel)
			{
				padding = new RectOffset(BASE_PADDING, BASE_PADDING, 0,0),
				alignment = TextAnchor.MiddleLeft,
			};

			float snapshotFullNameSize = boldLabel.CalcSize(new GUIContent($"Snapshot Name: {snapshot.Name}")).x + BASE_PADDING * 3;
			var contentWithLongestName = snapshot.Contents.OrderByDescending(item => item.Name.Length).First();
			float longestNameSize = EditorStyles.label.CalcSize(new GUIContent(contentWithLongestName.Name)).x + INDENT_WIDTH + SNAPSHOT_ICON_SIZE + BASE_PADDING * 4;
			float contentMinSize = Mathf.Max(snapshotFullNameSize, longestNameSize, _snapshotSplitter.cellNormalizedSizes[1]* EditorGUIUtility.currentViewWidth);
			
			
			EditorGUILayout.BeginHorizontal();
			_snapshotInfoScroll = EditorGUILayout.BeginScrollView(_snapshotInfoScroll, GUILayout.MinWidth(contentMinSize));
			
			EditorGUILayout.BeginVertical();
			var rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			if (snapshot.IsAutoSnapshot)
			{
				rectController.ReserveWidth(BASE_PADDING);
				EditorGUI.LabelField(rectController.rect, "(Auto Generated)");
				rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			}
			
			// Snapshot Name
			var nameContent = new GUIContent("Name: ");
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(nameContent).x), nameContent, boldLabel);
			EditorGUI.LabelField(rectController.rect, snapshot.Name);
			
			// Snapshot Author
			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			var authorContent = new GUIContent("Author: ");
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(authorContent).x), authorContent, boldLabel);
			EditorGUI.LabelField(rectController.rect, $"{snapshot.Author.Email} ({snapshot.Author.AccountId})");
			
			// Snapshot Timestamp
			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			var timestampLabel = new GUIContent("Timestamp: ");
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(timestampLabel).x), timestampLabel, boldLabel);
			DateTimeOffset savedTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(snapshot.SavedTimestamp);
			string timeAgoString = GenerateTimeAgoString(savedTimestamp);
			EditorGUI.LabelField(rectController.rect, $"{savedTimestamp:g} ({timeAgoString})");
			
			// Snapshot Manifest
			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
			var manifestLabel = new GUIContent("Manifest ID: ");
			var manifestValue = new GUIContent($"{snapshot.ManifestId}");
			EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(manifestLabel).x), manifestLabel, boldLabel);
			EditorGUI.LabelField(rectController.ReserveWidth(EditorStyles.label.CalcSize(manifestValue).x), manifestValue, EditorStyles.label);
			
			// Snapshot Realm (PID)
			rectController = new EditorGUIRectController(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight));
            var realmLabel = new GUIContent("Realm: ");
            EditorGUI.LabelField(rectController.ReserveWidth(boldLabel.CalcSize(realmLabel).x), realmLabel, boldLabel);
            string currentRealmMessage = "(Could not find current realm)";
            Color messageColor = Color.red;
            if (_cli.CurrentRealm != null)
            {
	            bool isSameRealm = _cli.CurrentRealm.Pid == snapshot.ProjectData.PID;
	            currentRealmMessage = isSameRealm ? "(Current Realm)" : $"(Current Realm is `{_cli.CurrentRealm.DisplayName}`)";
	            messageColor = isSameRealm ? Color.green : new Color(0.95f, 0.69f, 0.07f);
            }
            
            string colorHex = ColorUtility.ToHtmlStringRGBA(messageColor);
            EditorGUI.LabelField(rectController.rect,$"{snapshot.ProjectData.RealmName} ({snapshot.ProjectData.PID}) - <color=#{colorHex}>{currentRealmMessage}</color>", new GUIStyle(EditorStyles.label) { richText = true});
			
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

			if (deletedContents.Count > 0 && !_isAdditiveRestore)
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
				EditorGUILayout.LabelField("Snapshot matches all your local changes, so no change will be made",
				                           new GUIStyle(EditorStyles.label)
				                           {
					                           padding = new RectOffset(BASE_PADDING, BASE_PADDING, 0, 0)
				                           });
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
			
		}

		private string GenerateTimeAgoString(DateTimeOffset savedTimestamp)
		{
			DateTimeOffset now = DateTimeOffset.Now;
			var difference = now - savedTimestamp;
			if (difference.TotalDays >= 365)
			{
				int years = CalculateYears(savedTimestamp, now);
				return years == 1 ? "1 year ago" : $"{years} years ago";
			}
    
			if (difference.TotalDays >= 30)
			{
				int months = CalculateMonths(savedTimestamp, now);
				return months == 1 ? "1 month ago" : $"{months} months ago";
			}

			int days = (int)difference.TotalDays;
			if (days > 0)
			{
				return days == 1 ? $"1 day ago" : $"{days} days ago";
			}

			int hours = (int)difference.TotalHours;
			if (hours > 0)
			{
				return hours == 1 ? $"1 hour ago" : $"{hours} hours ago";
			}
			
			int minutes = (int) difference.TotalMinutes;
			if(minutes > 0)
			{
				return minutes == 1 ? $"1 minute ago" : $"{minutes} minutes ago";
			}
			
			int seconds = (int) difference.TotalSeconds;
			return seconds == 1 ? $"1 second ago" : $"{seconds} seconds ago";
		}
		
		private int CalculateYears(DateTimeOffset start, DateTimeOffset end)
		{
			int years = end.Year - start.Year;
			if (end.Month < start.Month || (end.Month == start.Month && end.Day < start.Day))
			{
				years--;
			}
			return years;
		}

		private int CalculateMonths(DateTimeOffset start, DateTimeOffset end)
		{
			int months = (end.Year - start.Year) * 12 + (end.Month - start.Month);
			if (end.Day < start.Day)
			{
				months--;
			}
			return months;
		}

		private async Task CacheSnapshots()
		{
			_selectedSnapshotsPaths.Clear();
			_snapshotSearchData = new SearchData();
			_gatheringSnapshots = true;
			var snapshotListResult = await _contentService.GetContentSnapshots();
			_sharedSnapshots = snapshotListResult.SharedSnapshots.ToDictionary(item => item.Name, item => item);
			_localSnapshots = snapshotListResult.LocalSnapshots.ToDictionary(item => item.Name, item => item);
			_allSnapshots.Clear();
			_localSnapshots.Values.ToList().ForEach(snapshot => _allSnapshots.Add(snapshot.Path, snapshot));
			_sharedSnapshots.Values.ToList().ForEach(snapshot => _allSnapshots.Add(snapshot.Path, snapshot));
			_gatheringSnapshots = false;
		}
		
	}
}
