using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor;
using Beamable.Editor.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		// Dictionary to track foldout states for each section
		private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>()
		{
			{"Created Contents", true}, {"Modified Contents", true}, {"Deleted Contents", true}
		};

		private Vector2 _mainScrollPosition;

		private void DrawPublishContent()
		{
			var allChangedContents = GetCachedManifestEntries()
			                         .Where(item => item.StatusEnum is ContentStatus.Created or ContentStatus.Deleted
				                                or ContentStatus.Modified).ToList();
			string realmName = BeamEditorContext.Default.CurrentRealm.DisplayName;
			var allModified = allChangedContents.Where(item => item.StatusEnum is ContentStatus.Modified).ToList();
			var allCreated = allChangedContents.Where(item => item.StatusEnum is ContentStatus.Created).ToList();
			var allDeleted = allChangedContents.Where(item => item.StatusEnum is ContentStatus.Deleted).ToList();
			float minWidth = 900f;
			float screenWidth = EditorGUIUtility.currentViewWidth;
			float availableSpace = Mathf.Max(screenWidth, minWidth);

			EditorGUILayout.BeginVertical();
			GUILayout.Space(40);
			_mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition, false, screenWidth < minWidth, GUILayout.ExpandWidth(true));
			availableSpace -= 20f;
			
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(minWidth));
			
			// Improved help box with larger text and centered alignment
			var publishHelpBoxRect =
				EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace),
				                               GUILayout.Height(BeamGUI.StandardVerticalSpacing * 2));
			var helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
			{
				fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true
			};
			EditorGUI.HelpBox(publishHelpBoxRect, $"", MessageType.Info);
			EditorGUI.LabelField(publishHelpBoxRect,
			                     new GUIContent(
				                     $"Clicking \"Publish Contents\" button will upload all content changes to realm {realmName}. Check the list below for the changes that will be applied."),
			                     helpBoxStyle);

			DrawChangesContents(availableSpace, "Created Contents", BeamGUI.iconStatusAdded, allCreated);
			DrawChangesContents(availableSpace, "Modified Contents", BeamGUI.iconStatusModified, allModified);
			DrawChangesContents(availableSpace, "Deleted Contents", BeamGUI.iconStatusDeleted, allDeleted);

			var buttonsRect = EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace), GUILayout.Height(30f));
			var buttonContent = new GUIContent("Publish Contents");
			var buttonSize = GUI.skin.button.CalcSize(buttonContent);
			if (BeamGUI.PrimaryButton(
				    new Rect(buttonsRect.x + 5f, buttonsRect.y, buttonSize.x, buttonsRect.height),
				    buttonContent))
			{
				if (EditorUtility.DisplayDialog("Publish Content",
				                                $"Are you sure you want to publish content changes to realm [{realmName}]?",
				                                "Yes", "No"))
				{
					_ = _contentService.PublishContentsWithProgress();
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private void DrawChangesContents(float width,
		                                 string headerName,
		                                 Texture icon,
		                                 List<LocalContentManifestEntry> itemList)
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
						
						var revertContent = new GUIContent("Revert", BeamGUI.iconRevertAction, "Revert this content to its previous state");
						if (GUI.Button(buttonRect, revertContent))
						{
							if (EditorUtility.DisplayDialog("Revert Content",
							                                $"Are you sure you want to revert {localContentManifestEntry.FullId}?",
							                                "Yes", "No"))
							{
								_ = _contentService.SyncContentsWithProgress(true, true, true, true, localContentManifestEntry.FullId, ContentFilterType.ExactIds);
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
				height += EditorGUIUtility.standardVerticalSpacing;
			}

			return height;
		}
	}
}
