using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private const float ACTION_PANEL_MIN_WIDTH = 900f;

		private Dictionary<string, bool> _foldoutStates = new()
		{
			{"Created Contents", true}, {"Modified Contents", true}, {"Deleted Contents", true}
		};

		private Vector2 _mainScrollPosition;

		private ContentStatus _statusToDraw = ContentStatus.Invalid;
		private Func<Task> _revertAction; 

		private void DrawPublishPanel()
		{
			if (_contentService.HasConflictedContent || _contentService.HasInvalidContent || !_contentService.HasChangedContents)
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);
				return;
			}
			
			string realmName = BeamEditorContext.Default.BeamCli.CurrentRealm.DisplayName;

			void PublishContents()
			{
				if (EditorUtility.DisplayDialog("Publish Content", $"Are you sure you want to publish content changes to realm [{realmName}]?", "Publish", "Cancel"))
				{
					_ = _contentService.PublishContentsWithProgress();
				}
			}

			DrawContentActionPanel(
				$"Clicking \"Publish Contents\" button will upload all content changes to realm {realmName}. Check the list below for the changes that will be applied.",
				"Publish Contents", PublishContents, true);
		}
		
		private void DrawRevertPanel()
		{
			if (!_contentService.HasChangedContents)
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);
				return;
			}
			
			string realmName = BeamEditorContext.Default.BeamCli.CurrentRealm.DisplayName;

			void RevertContent()
			{
				if (EditorUtility.DisplayDialog("Revert Content", $"Are you sure you want to revert content changes. All changes will be reverted to match contents from realm [{realmName}]?", "Revert", "Cancel"))
				{
					_ = _revertAction?.Invoke();
				}
			}

			DrawContentActionPanel(
				$"Clicking \"Revert Contents\" button will discard all listed below content changes to realm.",
				"Revert Contents", RevertContent);
		}
		
		private void DrawValidatePanel()
		{
			if (!_contentService.HasChangedContents)
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);
				return;
			}
			
			var redStyle = new GUIStyle(EditorStyles.label)
			{
				normal = {textColor = Color.red}, 
				wordWrap = true
			};
			
			float screenWidth = EditorGUIUtility.currentViewWidth;
			float availableSpace = Mathf.Max(screenWidth, ACTION_PANEL_MIN_WIDTH);
			
			EditorGUILayout.BeginVertical();
			_mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition, false, screenWidth < ACTION_PANEL_MIN_WIDTH, GUILayout.ExpandWidth(true));
			availableSpace -= 20f;
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(ACTION_PANEL_MIN_WIDTH));

			var contents = _contentService.GetAllContentFromStatus(ContentStatus.Modified);
			contents.AddRange(_contentService.GetAllContentFromStatus(ContentStatus.Created));

			var sortedItems = SortItems("contentActionCache", contents, ContentSortOptionType.ValidStatus);
			for (int index = sortedItems.Count - 1; index >= 0; index--)
			{
				LocalContentManifestEntry entry = sortedItems[index];
				bool hasError = false;
				List<string> errorList = new List<string>();
				string entryId = entry.FullId;
				if (_contentService.TryGetContentObject(entryId, out var cachedContentObj))
				{
					hasError = cachedContentObj.HasValidationErrors(_contentService.GetValidationContext(),
					                                                out errorList);
				}

				_foldoutStates.TryAdd(entryId, true);

				var heightSize = CalculateItemValidationHeight(_foldoutStates[entryId], errorList, availableSpace);
				EditorGUILayout.BeginVertical();
				var rectController = new EditorGUIRectController(
					EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace), GUILayout.Height(heightSize)));
				
				var headerRect =
					new EditorGUIRectController(rectController.ReserveHeight(BeamGUI.StandardVerticalSpacing));

				headerRect.ReserveWidth(BASE_PADDING);
				var foldoutRect = headerRect.ReserveWidth(FOLDOUT_WIDTH);
				if (hasError)
				{
					_foldoutStates[entryId] = EditorGUI.Foldout(foldoutRect, _foldoutStates[entryId], GUIContent.none);
				}

				var iconSize = 15f;
				var iconRect = headerRect.ReserveWidth(iconSize);
				Texture iconForStatus = hasError ? BeamGUI.iconStatusInvalid : GetIconForStatus(entry.IsInConflict, entry.StatusEnum);
				GUI.DrawTexture(new Rect(iconRect.x, iconRect.center.y - iconSize / 2f, iconSize, iconSize),
				                iconForStatus,
				                ScaleMode.ScaleToFit);

				headerRect.ReserveWidth(BASE_PADDING);

				var labelRect = headerRect.rect;
				var guiStyle = new GUIStyle(EditorStyles.boldLabel) {alignment = TextAnchor.MiddleLeft,};
				EditorGUI.LabelField(labelRect, entryId, guiStyle);

				if (_foldoutStates[entryId])
				{
					foreach (string error in errorList)
					{
						var errorContent = new GUIContent(error);
						float itemHeight = redStyle.CalcHeight(errorContent, rectController.rect.width - BASE_PADDING);
						var itemRect = rectController.ReserveHeight(itemHeight);
						EditorGUILayout.BeginHorizontal();
						{
							itemRect.xMin = iconRect.xMax;
							itemRect.width -= BASE_PADDING;
							var itemRectController = new EditorGUIRectController(itemRect);
							var idValidationRect = itemRectController.ReserveWidth(4);
							EditorGUI.DrawRect(idValidationRect, Color.red);
							itemRectController.ReserveWidth(BASE_PADDING);
							
							EditorGUI.LabelField(itemRectController.rect, errorContent, redStyle);
						}
						EditorGUILayout.EndHorizontal();
						rectController.ReserveHeight(EditorGUIUtility.standardVerticalSpacing);
					}
				}

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			bool hasConflictOrInvalid = _contentService.HasInvalidContent || _contentService.HasConflictedContent;
			
			var buttonsRect = EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace), GUILayout.Height(30f));
			
			var buttonsRectController = new EditorGUIRectController(buttonsRect);
			var publishBtnContent = new GUIContent("Go to Publish");
			var publishBtnSize = GUI.skin.button.CalcSize(publishBtnContent);
			Rect publicBtnRect = buttonsRectController.ReserveWidthFromRight(publishBtnSize.x + BASE_PADDING * 2);
			if (BeamGUI.ShowDisabled(!hasConflictOrInvalid, () => BeamGUI.PrimaryButton(publicBtnRect, publishBtnContent)))
			{
				ChangeToPublishMode();
			}
			
			var revertSyncBtnContent = new GUIContent("Go to Revert/Sync");
			var revertSyncBtnSize = GUI.skin.button.CalcSize(revertSyncBtnContent);
			Rect revertSyncBtnRect = buttonsRectController.ReserveWidthFromRight(revertSyncBtnSize.x + BASE_PADDING * 2);
			if (BeamGUI.PrimaryButton(revertSyncBtnRect, revertSyncBtnContent))
			{
				ChangeToRevertAll();
			}
			
			var cancelBtnContent = new GUIContent("Cancel");
			var cancelBtnSize = GUI.skin.button.CalcSize(cancelBtnContent);
			if (BeamGUI.CustomButton(buttonsRectController.ReserveWidthFromRight(cancelBtnSize.x + BASE_PADDING * 2), cancelBtnContent, BeamGUI.ColorizeButton(Color.gray)))
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);
			}
			
			EditorGUILayout.EndVertical();
		}
		
		private void DrawContentActionPanel(string warningMessage, string buttonText, Action onButtonClicked, bool showRevert = false)
		{
			var emptyList = new List<LocalContentManifestEntry>();
			var allModified = (_statusToDraw & ContentStatus.Modified) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Modified) : emptyList;
			var allCreated = (_statusToDraw & ContentStatus.Created) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Created) : emptyList;
			var allDeleted = (_statusToDraw & ContentStatus.Deleted) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Deleted) : emptyList;

			float screenWidth = EditorGUIUtility.currentViewWidth;
			float availableSpace = Mathf.Max(screenWidth, ACTION_PANEL_MIN_WIDTH);

			EditorGUILayout.BeginVertical();
			
			_mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition, false, screenWidth < ACTION_PANEL_MIN_WIDTH, GUILayout.ExpandWidth(true));
			availableSpace -= 20f;

			EditorGUILayout.BeginVertical(GUILayout.MinWidth(ACTION_PANEL_MIN_WIDTH));

			Rect publishHelpBoxRect =
				EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace),
				                               GUILayout.Height(BeamGUI.StandardVerticalSpacing * 2));
			var helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true
			};
			EditorGUI.HelpBox(publishHelpBoxRect, "", MessageType.Info);
			EditorGUI.LabelField(publishHelpBoxRect,
			                     new GUIContent(warningMessage),
			                     helpBoxStyle);

			DrawChangedContents(availableSpace, "Created Contents", BeamGUI.iconStatusAdded, allCreated, showRevert);
			DrawChangedContents(availableSpace, "Modified Contents", BeamGUI.iconStatusModified, allModified, showRevert);
			DrawChangedContents(availableSpace, "Deleted Contents", BeamGUI.iconStatusDeleted, allDeleted, showRevert);
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			
			var buttonsRect = EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace), GUILayout.Height(30f));
			var buttonsRectController = new EditorGUIRectController(buttonsRect);
			var primaryBtnContent = new GUIContent(buttonText);
			var primaryBtnSize = GUI.skin.button.CalcSize(primaryBtnContent);
			
			if (BeamGUI.PrimaryButton(buttonsRectController.ReserveWidthFromRight(primaryBtnSize.x + BASE_PADDING * 2), primaryBtnContent))
			{
				onButtonClicked?.Invoke();
			}
			var cancelBtnContent = new GUIContent("Cancel");
			var cancelBtnSize = GUI.skin.button.CalcSize(cancelBtnContent);
			if (BeamGUI.CustomButton(buttonsRectController.ReserveWidthFromRight(cancelBtnSize.x + BASE_PADDING * 2), cancelBtnContent, BeamGUI.ColorizeButton(Color.gray)))
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawChangedContents(float width,
		                                 string headerName,
		                                 Texture icon,
		                                 List<LocalContentManifestEntry> itemList, bool showRevert = false)
		{
			if (itemList.Count == 0)
				return;

			var sortedList = SortItems(headerName, itemList, ContentSortOptionType.IdAscending);
			
			_foldoutStates.TryAdd(headerName, true);

			var heightSize = CalculateContentsHeight(_foldoutStates[headerName], sortedList.Count);
			EditorGUILayout.BeginVertical();
			var rectController =
				new EditorGUIRectController(
					EditorGUILayout.GetControlRect(GUILayout.Width(width), GUILayout.Height(heightSize)));

			GUI.Box(rectController.rect, GUIContent.none, EditorStyles.helpBox);
			var headerRect = new EditorGUIRectController(rectController.ReserveHeight(BeamGUI.StandardVerticalSpacing * 1.5f));

			headerRect.ReserveWidth(BASE_PADDING);
			var foldoutRect = headerRect.ReserveWidth(FOLDOUT_WIDTH);
			_foldoutStates[headerName] = EditorGUI.Foldout(foldoutRect, _foldoutStates[headerName], GUIContent.none);

			var iconSize = 15f;
			var iconRect = headerRect.ReserveWidth(iconSize);
			GUI.DrawTexture(new Rect(iconRect.x, iconRect.center.y - iconSize/2f, iconSize, iconSize), icon, ScaleMode.ScaleToFit);
			
			headerRect.ReserveWidth(BASE_PADDING);

			var labelRect = headerRect.rect;
			var guiStyle = new GUIStyle(EditorStyles.boldLabel) {alignment = TextAnchor.MiddleLeft,};
			EditorGUI.LabelField(labelRect, headerName, guiStyle);

			if (_foldoutStates[headerName])
			{
				foreach (LocalContentManifestEntry localContentManifestEntry in sortedList)
				{
					var itemRect = rectController.ReserveSingleLine();
					EditorGUILayout.BeginHorizontal();
					{
						itemRect.xMin = iconRect.xMax;
						itemRect.width -= BASE_PADDING;
						var itemRectController = new EditorGUIRectController(itemRect);

						var itemIconRect = itemRectController.ReserveWidth(iconSize);
						itemRectController.ReserveWidth(BASE_PADDING);
						
						bool isContentInvalid = _contentService.IsContentInvalid(localContentManifestEntry.FullId);
						bool isInConflict = localContentManifestEntry.IsInConflict;
						if (isContentInvalid || isInConflict)
						{
							GUI.DrawTexture(itemIconRect, isContentInvalid ? BeamGUI.iconStatusInvalid : BeamGUI.iconStatusConflicted, ScaleMode.ScaleToFit);
						}
						
						var buttonRect = itemRectController.ReserveWidthFromRight(80);
						var contentRect = itemRectController.rect;
						var entryContent = new GUIContent(localContentManifestEntry.FullId, localContentManifestEntry.FullId);
						EditorGUI.LabelField(contentRect, entryContent);
						if (showRevert)
						{
							var revertContent = new GUIContent("Revert", BeamGUI.iconRevertAction,
							                                   "Revert this content to its previous state");
							if (GUI.Button(buttonRect, revertContent))
							{
								if (EditorUtility.DisplayDialog("Revert Content",
								                                $"Are you sure you want to revert {localContentManifestEntry.FullId}?",
								                                "Revert", "Cancel"))
								{
									_ = _contentService.SyncContentsWithProgress(
										true, true, true, true, localContentManifestEntry.FullId,
										ContentFilterType.ExactIds);
								}
							}
						}
					}
					EditorGUILayout.EndHorizontal();
					rectController.ReserveHeight(EditorGUIUtility.standardVerticalSpacing);
				}
			}

			EditorGUILayout.EndVertical();
		}

		private float CalculateContentsHeight(bool isOpen, int entriesCount)
		{
			var height = BeamGUI.StandardVerticalSpacing * 1.5f; // Header
			if (isOpen)
			{
				height += BeamGUI.StandardVerticalSpacing * entriesCount; // Items
				height += EditorGUIUtility.standardVerticalSpacing; // Bottom spacing
			}

			return height;
		}

		private float CalculateItemValidationHeight(bool isOpen, List<string> errors, float availableWidth)
		{
			GUIStyle style = EditorStyles.label;
			style.wordWrap = true;
			var height = BeamGUI.StandardVerticalSpacing; // Entry Label
			if (isOpen)
			{
				foreach (string error in errors) // Calc Errors Size
				{
					height += style.CalcHeight(new GUIContent(error), availableWidth);
					height += EditorGUIUtility.standardVerticalSpacing;
				}
				height += EditorGUIUtility.standardVerticalSpacing; // Bottom spacing
			}
			return height;
		}
	}
}
