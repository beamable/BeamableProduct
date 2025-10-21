using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.BeamCli.UI;
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
		private Func<Promise> _revertAction; 

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
					_contentService.PublishContentsWithProgress().Then(_ =>
					{
						ChangeWindowStatus(ContentWindowStatus.Normal);
					});
				}
			}

			DrawContentActionPanel(BeamGUI.iconPublish, "Publish Content",
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
					_revertAction?.Invoke().Then(_ =>
					{
						ChangeWindowStatus(ContentWindowStatus.Normal);
					});
				}
			}

			DrawContentActionPanel( BeamGUI.iconSync,"Revert Content",
				$"Clicking \"Revert Contents\" button will discard all listed below content changes to realm.",
				"Revert Contents", RevertContent);
		}

		public int selectedValidationIndex;
		public Vector2 validationDetailScrollPosition;
		
		private void DrawValidatePanel()
		{
			var errorStyle = new GUIStyle(EditorStyles.label)
			{
				normal = {textColor = Color.red},
				wordWrap = true
			};

			var idStyle = new GUIStyle(EditorStyles.label)
			{
				wordWrap = false,
			};
			
			// render all content that are invalid in a virtual scroller....
			var entries = _contentService.GetAllInvalidContents();

			EditorGUILayout.BeginHorizontal();
			var headerTexRect = EditorGUILayout.GetControlRect(GUILayout.Width(24), GUILayout.Height(24));
			GUI.DrawTexture(headerTexRect, BeamGUI.iconCheck);
			EditorGUILayout.LabelField("Validation Results", _contentHeaderStyle);
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(12);

			if (entries.Count == 0)
			{
				EditorGUILayout.LabelField("There are no validation errors.", _contentHeaderDescriptionStyle);
				GUILayout.Space(12);
			}
			else
			{
				EditorGUILayout.LabelField("The following content items have validation issues. Select a content item to view the errors.", _contentHeaderDescriptionStyle);
				GUILayout.Space(12);
			}

			var visHeight = Mathf.Min(300, 30 * entries.Count);
			BeamCliWindow.DrawVirtualScroller(30, entries.Count, ref _mainScrollPosition, (index, rect) =>
			{
				if (index == selectedValidationIndex)
				{
					EditorGUI.DrawRect(rect, GUI.skin.settings.selectionColor);
				}
				
				var wasPressed =
					GUI.Button(rect, GUIContent.none, EditorStyles.label);

				if (wasPressed)
				{
					selectedValidationIndex = index;
				}
				
				EditorGUI.LabelField(rect, entries[index].FullId, idStyle);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
			}, visHeight);

			GUILayout.Space(12);
			BeamGUI.DrawSeparatorLine(false, color: new Color(0,0,0,.2f));


			if (selectedValidationIndex < entries.Count)
			{
				List<string> errorList = new List<string>();
				var selectedEntry = entries[selectedValidationIndex];
				if (_contentService.TryGetContentObject(selectedEntry.FullId, out var cachedContentObj))
				{
					cachedContentObj.HasValidationErrors(_contentService.GetValidationContext(), out errorList);
				}

				GUILayout.Space(6);
				
				EditorGUILayout.LabelField($"Selected: {selectedEntry.FullId}", new GUIStyle(EditorStyles.boldLabel)
				{
					wordWrap = true
				});
				SetEntryIdAsSelected(selectedEntry.FullId);

				
				validationDetailScrollPosition = EditorGUILayout.BeginScrollView(validationDetailScrollPosition);
				for (var i = 0 ; i < errorList.Count; i ++)
				{
					var error = errorList[i];
					EditorGUILayout.BeginHorizontal();
					var texRect = EditorGUILayout.GetControlRect(GUILayout.Width(16), GUILayout.Height(16));
					GUI.DrawTexture(texRect, BeamGUI.iconStatusInvalid);

					var labelRect = GUILayoutUtility.GetRect(new GUIContent(error), errorStyle);

					if (i % 2 == 1)
					{
						var backdropRect = new Rect(texRect.xMin, texRect.yMin, texRect.width + labelRect.width,
						                            labelRect.height);
						EditorGUI.DrawRect(backdropRect, new Color(0, 0, 0, .1f));
					}
					

					EditorGUI.LabelField(labelRect, error, errorStyle);
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.EndScrollView();
				
			}
			else
			{
				GUILayout.Space(6);
				EditorGUILayout.LabelField($"(no selection)", new GUIStyle(EditorStyles.boldLabel)
				{
					wordWrap = true
				});
			}

			GUILayout.FlexibleSpace();
			if (BeamGUI.CancelButton("Back", GUILayout.Width(60)))
			{
				ChangeWindowStatus(ContentWindowStatus.Normal);
			}
		}
		
		private void DrawContentActionPanel(Texture icon, string title, string warningMessage, string buttonText, Action onButtonClicked, bool showRevert = false)
		{
			var emptyList = new List<LocalContentManifestEntry>();
			var allModified = (_statusToDraw & ContentStatus.Modified) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Modified) : emptyList;
			var allCreated = (_statusToDraw & ContentStatus.Created) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Created) : emptyList;
			var allDeleted = (_statusToDraw & ContentStatus.Deleted) != 0 ? _contentService.GetAllContentFromStatus(ContentStatus.Deleted) : emptyList;

			float screenWidth = EditorGUIUtility.currentViewWidth;
			// float availableSpace = Mathf.Max(screenWidth, ACTION_PANEL_MIN_WIDTH);
			float availableSpace = Mathf.Max(screenWidth, ACTION_PANEL_MIN_WIDTH);

			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			var texRect = EditorGUILayout.GetControlRect(GUILayout.Width(24), GUILayout.Height(24));
			GUI.DrawTexture(texRect, icon);
			EditorGUILayout.LabelField(title, _contentHeaderStyle);
			EditorGUILayout.EndHorizontal();
			
			GUILayout.Space(12);
			EditorGUILayout.LabelField(warningMessage, _contentHeaderDescriptionStyle);
			GUILayout.Space(12);
		
			
			EditorGUILayout.Space(12);
			
			_mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition, GUILayout.ExpandWidth(true));
			const int widthOfAUnityScrollBar = 25;
			availableSpace -= widthOfAUnityScrollBar + NESTED_CONTENT_PADDING*2;

			EditorGUILayout.BeginVertical();


			DrawChangedContents(availableSpace, "Created Contents", BeamGUI.iconStatusAdded, allCreated, showRevert);
			DrawChangedContents(availableSpace, "Modified Contents", BeamGUI.iconStatusModified, allModified, showRevert);
			DrawChangedContents(availableSpace, "Deleted Contents", BeamGUI.iconStatusDeleted, allDeleted, showRevert);
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			
			EditorGUILayout.Space(12);
			
			var buttonsRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(30f));
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

						Rect buttonRect = default;
						if (showRevert)
						{
							buttonRect = itemRectController.ReserveWidthFromRight(80);
						}
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
