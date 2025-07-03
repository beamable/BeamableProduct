using Beamable;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor;
using Beamable.Editor.Util;
using Editor.UI.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		private void DrawPublishContent()
		{
			var allChangedContents = GetCachedManifestEntries()
				.Where(item => item.StatusEnum is ContentStatus.Created or ContentStatus.Deleted
					       or ContentStatus.Modified).ToList();
			var allModified = allChangedContents.Where(item => item.StatusEnum is ContentStatus.Modified).ToList();
			var allCreated = allChangedContents.Where(item => item.StatusEnum is ContentStatus.Created).ToList();
			var allDeleted = allChangedContents.Where(item => item.StatusEnum is ContentStatus.Deleted).ToList();
			float availableSpace = Mathf.Max(EditorGUIUtility.currentViewWidth, 900f);
			var columnSize = (availableSpace / 3f) - 10f;
			GUILayout.Space(30);

			var maxItems = Mathf.Max(allModified.Count, allCreated.Count, allDeleted.Count);
			
			var contentRect = EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace), GUILayout.Height(CalculateColumnHeight(maxItems)));
			var rectController = new EditorGUIRectController(contentRect);
			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			{
				rectController.ReserveWidth(5);
				DrawColumn(rectController, columnSize, "Created Contents", "There is no new created content.", BeamGUI.iconStatusAdded, allCreated);
				rectController.ReserveWidth(5);
				DrawColumn(rectController, columnSize, "Modified Contents", "There is no modified content.", BeamGUI.iconStatusModified, allModified);
				rectController.ReserveWidth(5);
				DrawColumn(rectController, columnSize, "Deleted Contents", "There is no deleted content.", BeamGUI.iconStatusDeleted, allDeleted);
			}
			EditorGUILayout.EndHorizontal();
			
			var buttonsRect = EditorGUILayout.GetControlRect(GUILayout.Width(availableSpace), GUILayout.Height(30f));
			var buttonContent = new GUIContent("Publish Contents");
			var buttonSize = GUI.skin.button.CalcSize(buttonContent);
			if (GUI.Button(new Rect(buttonsRect.center.x - buttonSize.x/2f, buttonsRect.y, buttonSize.x, buttonsRect.height), buttonContent))
			{
				string realmName = BeamEditorContext.Default.CurrentRealm.DisplayName;
				if (EditorUtility.DisplayDialog("Publish Content",
				                                $"Are you sure you want to publish content changes to realm [{realmName}]?",
				                                "Yes", "No"))
				{
					_ = _contentService.PublishContentsWithProgress();
				}
			}
			
			EditorGUILayout.EndVertical();
		}

		private void DrawColumn(EditorGUIRectController rectController, float columnSize, string headerName, string noItemMessage, Texture icon, List<LocalContentManifestEntry> itemList)
		{
			EditorGUILayout.BeginVertical();
			var columnRect = rectController.ReserveWidth(columnSize);
			var columnRectController = new EditorGUIRectController(columnRect);
				
			EditorGUI.DrawRect(columnRect, new Color(0.15f, 0.15f, 0.15f));
			Rect headerRect = columnRectController.ReserveHeight(PropertyDrawerUtils.StandardVerticalSpacing);
			var itemContent = new GUIContent(headerName);
			var guiStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				alignment = TextAnchor.MiddleCenter,
			};
			var contentSize = guiStyle.CalcSize(itemContent);
			var iconSize = 15f;
			
			GUI.DrawTexture(new Rect(headerRect.center.x - contentSize.x/2f - iconSize, headerRect.center.y - iconSize/2f, iconSize, iconSize), icon);
			EditorGUI.LabelField(new Rect(headerRect.center.x - contentSize.x/2f + iconSize/2f, headerRect.y, contentSize.x, headerRect.height), itemContent, guiStyle);

			if (itemList.Count == 0)
			{
				Rect infoBoxRect = columnRectController.ReserveHeight(PropertyDrawerUtils.StandardVerticalSpacing * 2f);
				infoBoxRect.xMin += 5;
				infoBoxRect.width -= 5;
				EditorGUI.HelpBox(infoBoxRect, noItemMessage, MessageType.Info);
			}
			foreach (LocalContentManifestEntry localContentManifestEntry in itemList)
			{
				EditorGUILayout.BeginHorizontal();
				{
					var itemRect = columnRectController.ReserveHeight(PropertyDrawerUtils.StandardVerticalSpacing);
					itemRect.xMin += 5;
					itemRect.width -= 5;
					var entryContent = new GUIContent(localContentManifestEntry.FullId, localContentManifestEntry.FullId);
					EditorGUI.LabelField(itemRect, entryContent);
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
		}

		private float CalculateColumnHeight(int entriesCount)
		{
			var minSize = Mathf.Max(entriesCount, 2);
			return (minSize + 2) * PropertyDrawerUtils.StandardVerticalSpacing;
		}
	}
}
