using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
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
			}
			
			string realmName = BeamEditorContext.Default.CurrentRealm.DisplayName;

			void PublishContents()
			{
				if (EditorUtility.DisplayDialog("Publish Content", $"Are you sure you want to publish content changes to realm [{realmName}]?", "Yes", "No"))
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
			}
			
			string realmName = BeamEditorContext.Default.CurrentRealm.DisplayName;

			void RevertContent()
			{
				if (EditorUtility.DisplayDialog("Revert Content", $"Are you sure you want to revert content changes. All changes will be reverted to match contents from realm [{realmName}]?", "Yes", "No"))
				{
					_ = _revertAction?.Invoke();
				}
			}

			DrawContentActionPanel(
				$"Clicking \"Revert Contents\" button will discard all listed below content changes to realm.",
				"Revert Contents", RevertContent, false, true);
		}
		
		private void DrawContentActionPanel(string warningMessage, string buttonText, Action onButtonClicked, bool showRevert = false, bool showConflictedAndInvalid = false)
		{
			var emptyList = new List<LocalContentManifestEntry>();
			var allModified = (_statusToDraw & ContentStatus.Modified) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Modified) : emptyList;
			var allCreated = (_statusToDraw & ContentStatus.Created) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Created) : emptyList;
			var allDeleted = (_statusToDraw & ContentStatus.Deleted) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Deleted) : emptyList;
			var allConflicted = showConflictedAndInvalid ? _contentService.GetAllConflictedContents().Where(item => _statusToDraw.HasFlag(item.StatusEnum)).ToList() : emptyList;
			var allInvalid = showConflictedAndInvalid ? _contentService.GetAllInvalidContents().Where(item => _statusToDraw.HasFlag(item.StatusEnum)).ToList() : emptyList;
			
			float minWidth = 900f;
			float screenWidth = EditorGUIUtility.currentViewWidth;
			float availableSpace = Mathf.Max(screenWidth, minWidth);

			EditorGUILayout.BeginVertical();
			
			_mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition, false, screenWidth < minWidth, GUILayout.ExpandWidth(true));
			availableSpace -= 20f;
			
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidth));
			
			var publishHelpBoxRect =
				EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace),
				                               GUILayout.Height(BeamGUI.StandardVerticalSpacing * 2));
			var helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true
			};
			EditorGUI.HelpBox(publishHelpBoxRect, $"", MessageType.Info);
			EditorGUI.LabelField(publishHelpBoxRect,
			                     new GUIContent(warningMessage),
			                     helpBoxStyle);

			DrawChangedContents(availableSpace, "Created Contents", BeamGUI.iconStatusAdded, allCreated, showRevert);
			DrawChangedContents(availableSpace, "Modified Contents", BeamGUI.iconStatusModified, allModified, showRevert);
			DrawChangedContents(availableSpace, "Deleted Contents", BeamGUI.iconStatusDeleted, allDeleted, showRevert);
			DrawChangedContents(availableSpace, "Conflicted Contents", BeamGUI.iconStatusConflicted, allConflicted, true);
			DrawChangedContents(availableSpace, "Invalid Contents", BeamGUI.iconInvalid, allInvalid, true);
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			
			var buttonsRect = EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace), GUILayout.Height(30f));
			var buttonsRectController = new EditorGUIRectController(buttonsRect);
			var publishBtnContent = new GUIContent(buttonText);
			var publishBtnSize = GUI.skin.button.CalcSize(publishBtnContent);
			
			if (BeamGUI.PrimaryButton(buttonsRectController.ReserveWidthFromRight(publishBtnSize.x), publishBtnContent))
			{
				onButtonClicked?.Invoke();
			}
			var cancelBtnContent = new GUIContent("Cancel");
			var cancelBtnSize = GUI.skin.button.CalcSize(publishBtnContent);
			if (BeamGUI.CustomButton(buttonsRectController.ReserveWidthFromRight(cancelBtnSize.x), cancelBtnContent, GUI.skin.button))
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

			
			_foldoutStates.TryAdd(headerName, true);

			var heightSize = CalculateHeight(_foldoutStates[headerName], itemList.Count);
			EditorGUILayout.BeginVertical();
			var rectController =
				new EditorGUIRectController(
					EditorGUILayout.GetControlRect(GUILayout.Width(width), GUILayout.Height(heightSize)));

			GUI.Box(rectController.rect, GUIContent.none, EditorStyles.helpBox);
			Rect headerRect = rectController.ReserveHeight(BeamGUI.StandardVerticalSpacing * 1.5f);

			
			var foldoutRect = new Rect(headerRect.x + 5, headerRect.y, 20, headerRect.height);
			_foldoutStates[headerName] = EditorGUI.Foldout(foldoutRect, _foldoutStates[headerName], GUIContent.none);

			var iconSize = 15f;
			var iconRect = new Rect(foldoutRect.xMax, headerRect.y + (headerRect.height - iconSize) / 2, iconSize,
			                        iconSize);
			GUI.DrawTexture(iconRect, icon);

			var labelRect = new Rect(iconRect.xMax + 5, headerRect.y, headerRect.width - (iconRect.xMax + 5),
			                         headerRect.height);
			var guiStyle = new GUIStyle(EditorStyles.boldLabel) {alignment = TextAnchor.MiddleLeft,};
			EditorGUI.LabelField(labelRect, headerName, guiStyle);

			if (_foldoutStates[headerName])
			{
				foreach (LocalContentManifestEntry localContentManifestEntry in itemList)
				{
					var itemRect = rectController.ReserveSingleLine();
					EditorGUILayout.BeginHorizontal();
					{
						itemRect.xMin = labelRect.xMin;
						itemRect.width -= 5;

						var itemRectController = new EditorGUIRectController(itemRect);
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
								                                "Yes", "No"))
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

		private float CalculateHeight(bool isOpen, int entriesCount)
		{
			var height = BeamGUI.StandardVerticalSpacing * 1.5f; // Header
			if (isOpen)
			{
				height += BeamGUI.StandardVerticalSpacing * entriesCount; // Items
				height += EditorGUIUtility.standardVerticalSpacing; // Bottom spacing
			}

			return height;
		}
	}
}
