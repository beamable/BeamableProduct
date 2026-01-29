using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Content;
using Beamable.Editor.Util;
using Beamable.Editor.ContentService;
using Beamable.Editor.UI.ContentWindow;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using ContentFilterType = Beamable.Common.Content.ContentFilterType;

namespace Beamable.Editor.Content.UI
{
#if !BEAMABLE_NO_CONTENT_INSPECTOR
	[CanEditMultipleObjects]
	[CustomEditor(typeof(ContentObject), true)]
	public class ContentObjectEditor : UnityEditor.Editor
	{
		private const float HEADER_HEIGHT = 60f;
		private const float BUTTONS_HEADER_HEIGHT = 90f;
		private EditorCoroutine _updateNameCoroutine;
		private EditorCoroutine _updateTagCoroutine;

		protected override void OnHeaderGUI()
		{
			var contentObject = target as ContentObject;
			if (contentObject == null) return;
			var isEditingMultiple = targets.Length > 1;

			var contentService = BeamEditorContext.Default.ServiceScope.GetService<CliContentService>();

			var boldLabelFieldStyle =
				new GUIStyle(EditorStyles.boldLabel) {fixedHeight = EditorGUIUtility.singleLineHeight};

			var isModified = contentObject.ContentStatus is ContentStatus.Modified or ContentStatus.Created or ContentStatus.Deleted;
			var isInConflict = contentObject.IsInConflict;

			var headerHeight = (isModified || isInConflict) && !isEditingMultiple ? BUTTONS_HEADER_HEIGHT : HEADER_HEIGHT;

			ContentNameValidationException.HasNameValidationErrors(contentObject, contentObject.ContentName,
			                                                       out var nameErrors);
			headerHeight += nameErrors.Count * 5;
			var topPadding = (nameErrors.Count + 1) * 5;
			
			GUIStyle headerStyle = new GUIStyle(GUI.skin.box)
			{
				padding = new RectOffset(10, 10, topPadding, 10), 
				margin = new RectOffset(0, 0, 5, 5)
			};
			GUILayout.BeginVertical(headerStyle);
			{
				var headerRect = EditorGUILayout.GetControlRect(GUILayout.Height(headerHeight));
				GUILayout.BeginHorizontal();
				Texture texture = ContentConfiguration.Instance.ContentTextureConfiguration.GetTextureForType(
					contentObject.ContentType);
				float iconSize = 40f;
				var iconRect = new Rect(headerRect.x, headerRect.y + iconSize/2f, iconSize, iconSize);
				GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit);

				Rect contentRect = new Rect(
					iconRect.xMax + 10f,
					headerRect.y + 10,
					headerRect.width - iconRect.width,
					EditorGUIUtility.singleLineHeight * 3);

				if (isEditingMultiple)
				{
					EditorGUI.LabelField(contentRect, "Editing multiple content files...", EditorStyles.miniLabel);
					GUILayout.EndHorizontal();
					GUILayout.EndVertical();
					return;
				}
				
				if (contentObject.ContentStatus is not ContentStatus.UpToDate)
				{
					var statusIconSize = 15f;
					var statusIconRect = new Rect(
						iconRect.xMin,
						iconRect.yMin,
						statusIconSize,
						statusIconSize);
					var icon = ContentWindow.GetIconForStatus(contentObject.IsInConflict, contentObject.ContentStatus);
					GUI.DrawTexture(statusIconRect, icon, ScaleMode.ScaleToFit);
					if (contentService.IsContentInvalid(contentObject.Id))
					{
						var invalidIconRect = new Rect(
							iconRect.xMax - statusIconSize/2f, 
							iconRect.yMin, 
							statusIconSize, 
							statusIconSize);
						GUI.DrawTexture(invalidIconRect, BeamGUI.iconStatusInvalid, ScaleMode.ScaleToFit);
					}
				}

				if (Event.current.type == EventType.MouseDown && iconRect.Contains(Event.current.mousePosition))
				{
					SettingsService.OpenProjectSettings("Project/Beamable/Content");
				}

				
				DrawHeaderFields(contentRect, boldLabelFieldStyle, contentObject, contentService);

				DrawNameValidator(contentObject, headerRect);

				GUILayout.EndHorizontal();
				if (isModified || isInConflict)
				{
					Rect buttonsRect = new Rect(
						headerRect.xMin + 5,
						contentRect.yMax + 5,
						headerRect.width - 10,
						headerRect.height - contentRect.height - 10);
					DrawContentButtons(contentObject, buttonsRect, contentService);
				}
			}
		GUILayout.EndVertical();
		}

		private void DrawContentButtons(ContentObject content, Rect buttonsRect, CliContentService contentService)
		{
			var modifiedButtonWidth = 80f;
			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
			{
				alignment = TextAnchor.MiddleLeft,     
				padding = new RectOffset(10, 5, 5, 5),
			};
			bool hasModifiedButton = content.ContentStatus is ContentStatus.Deleted or ContentStatus.Modified or ContentStatus.Created;
			bool hasConflictSolveButton = content.IsInConflict;
			if (hasModifiedButton)
			{
				var revertContent = new GUIContent("Revert");
				
				var revertButtonRect = new Rect(
					buttonsRect.xMin,
					buttonsRect.y,
					modifiedButtonWidth,
					buttonsRect.height);
				if (GUI.Button(revertButtonRect, revertContent, buttonStyle))
				{
					if (EditorUtility.DisplayDialog("Revert Content", 
					                                "Are you sure you want to revert this content?", "Revert", "Cancel"))
					{
						_ = contentService.SyncContentsWithProgress(true, true, true, true, content.Id, ContentFilterType.ExactIds);
					}
					
				}

				float iconSize = 16;
				var iconRect = new Rect(
					revertButtonRect.xMax - iconSize - 10,
					revertButtonRect.y + (revertButtonRect.height - iconSize) / 2,
					iconSize,
					iconSize);
				GUI.DrawTexture(iconRect, BeamGUI.iconRevertAction);
			}

			if (hasConflictSolveButton)
			{
				var conflictContent = new GUIContent("Solve Conflict");
				var extraInitPos = hasModifiedButton ? modifiedButtonWidth + 5 : 0;
				var conflictSolveButtonRect = new Rect(
					buttonsRect.xMin + extraInitPos,
					buttonsRect.y,
					120f,
					buttonsRect.height);
				if (GUI.Button(conflictSolveButtonRect, conflictContent, buttonStyle))
				{
					GenericMenu conflictResolveMenu = new GenericMenu();
					if (contentService.IsContentInvalid(content.Id))
					{
						conflictResolveMenu.AddDisabledItem(new GUIContent("Use local", "Cannot resolve using local because it is invalid"));
					}
					else
					{
						conflictResolveMenu.AddItem(new GUIContent("Use Local"), false, () =>
						{
							contentService.ResolveConflict(content.Id, true);
						});	
					}
					conflictResolveMenu.AddItem(new GUIContent("Use Local"), false, () =>
					{
						contentService.ResolveConflict(content.Id, true);
					});
					conflictResolveMenu.AddItem(new GUIContent("Use Realm"), false, () =>
					{
						contentService.ResolveConflict(content.Id, false);
					});
					conflictResolveMenu.ShowAsContext();
				}

				float iconSize = 16;
				var iconRect = new Rect(
					conflictSolveButtonRect.xMax - iconSize - 10,
					conflictSolveButtonRect.y + (conflictSolveButtonRect.height - iconSize) / 2,
					iconSize,
					iconSize);
				GUI.DrawTexture(iconRect, BeamGUI.iconCheck);
			}
		}

		private void DrawHeaderFields(Rect contentRect, GUIStyle boldLabelFieldStyle, ContentObject contentObject, CliContentService contentService)
		{
			Rect idRect = new Rect(
				contentRect.x,
				contentRect.y,
				contentRect.width,
				EditorGUIUtility.singleLineHeight);
			var idContent = new GUIContent("Id: ");
			EditorGUI.LabelField(idRect, idContent, boldLabelFieldStyle);
			var idFieldSize = boldLabelFieldStyle.CalcSize(idContent);
			idRect.x += idFieldSize.x;
			EditorGUI.LabelField(idRect, contentObject.Id);
					
			Rect nameRect = new Rect(
				contentRect.x,
				idRect.yMax,
				contentRect.width -5,
				EditorGUIUtility.singleLineHeight);
			var nameContent = new GUIContent("Name: ");
			EditorGUI.LabelField(nameRect, nameContent, boldLabelFieldStyle);
			var nameFieldSize = boldLabelFieldStyle.CalcSize(nameContent);
			nameRect.xMin += nameFieldSize.x;
			EditorGUI.BeginChangeCheck();
			if (contentObject.ContentStatus is ContentStatus.Deleted)
			{
				EditorGUI.LabelField(nameRect, contentObject.ContentName);
			}
			else
			{
				string newName = EditorGUI.DelayedTextField(nameRect, contentObject.ContentName);
				if (EditorGUI.EndChangeCheck())
				{
					if (_updateNameCoroutine != null)
					{
						EditorCoroutineUtility.StopCoroutine(_updateNameCoroutine);
					}

					_updateNameCoroutine = EditorCoroutineUtility.StartCoroutine(DelayedActionEditorRoutine(0.5d, () =>
					{
						contentService.RenameContent(contentObject.Id, newName);
						contentObject.SetContentName(newName);
						EditorUtility.SetDirty(contentObject);
					}), contentObject);


				}
			}

			Rect tagsRect = new Rect(
				contentRect.x, 
				nameRect.yMax, 
				contentRect.width - 5, 
				EditorGUIUtility.singleLineHeight);
			
			
			var tagsValue = GetTagString(contentObject.Tags);
			if (targets.Length > 1)
			{
				for (var i = 0; i < targets.Length; i++)
				{
					var otherContentObject = targets[i] as ContentObject;
					if (otherContentObject == null) continue;
					var otherValue = GetTagString(otherContentObject.Tags);
					if (otherValue != tagsValue)
					{
						tagsValue = "-";
						break;
					}
				}
			}
					
			var contentTagLabel = new GUIContent("Content Tag: ");
			EditorGUI.LabelField(tagsRect, contentTagLabel, boldLabelFieldStyle);
			var contentTagLabelSize = boldLabelFieldStyle.CalcSize(contentTagLabel);
			tagsRect.xMin += contentTagLabelSize.x;
			if (contentObject.ContentStatus is Common.BeamCli.Contracts.ContentStatus.Deleted)
			{
				EditorGUI.LabelField(tagsRect, tagsValue);
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				var newTags = EditorGUI.TextField(tagsRect, tagsValue);
				if (EditorGUI.EndChangeCheck())
				{
					if (_updateTagCoroutine != null)
					{
						EditorCoroutineUtility.StopCoroutine(_updateTagCoroutine);
					}

					_updateTagCoroutine = EditorCoroutineUtility.StartCoroutine(DelayedActionEditorRoutine(0.5d, () =>
					{
						var tags = GetTagsFromString(newTags);
						contentService.SetContentTags(contentObject.Id, tags);
						contentObject.Tags = tags.ToArray();
						EditorUtility.SetDirty(contentObject);
					}), contentObject);

				}
			}
		}

		private IEnumerator DelayedActionEditorRoutine(double delay, Action onDelayComplete)
		{
			double baseTime = EditorApplication.timeSinceStartup;
			double elapsed = 0d;
	      
			while (elapsed < delay)
			{
				elapsed = EditorApplication.timeSinceStartup - baseTime;
				yield return null;
			}
	      
			onDelayComplete?.Invoke();
		}

		private static void DrawNameValidator(ContentObject contentObject, Rect headerRect)
		{
			if (contentObject.ContentName == null)
				contentObject.SetContentName(contentObject.name);

			if (ContentNameValidationException.HasNameValidationErrors(contentObject, contentObject.ContentName, out var nameErrors))
			{
				var errorText = string.Join(",", nameErrors.Select(n => n.Message));
				var idValidationRect = new Rect(headerRect.x - 5, headerRect.y, 4, headerRect.height);
				EditorGUI.DrawRect(idValidationRect, Color.red);
					
				var redStyle = new GUIStyle(GUI.skin.label);
				redStyle.normal.textColor = Color.red;
				redStyle.fontSize = 10;
				EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y-5, headerRect.width, 12), $"({errorText})", redStyle);
					
			}
		}
		


		public override void OnInspectorGUI()
		{
			var contentObject = target as ContentObject;
			if (contentObject == null) return;

			if (contentObject.ContentStatus is Common.BeamCli.Contracts.ContentStatus.Deleted)
			{
				EditorGUILayout.HelpBox("This content has been deleted.", MessageType.Info);
				return;
			}

			if (contentObject.SerializeToConsoleRequested)
			{
				contentObject.SerializeToConsoleRequested = false;
				var serialized = ClientContentSerializer.SerializeContent(contentObject);
				Debug.Log(serialized);
			}

			if (ContentObject.ShowChecksum)
			{
				var checksumStyle = new GUIStyle(GUI.skin.label);
				checksumStyle.fontSize = 10;
				// TODO: Can we get a fixed width
				checksumStyle.alignment = TextAnchor.MiddleRight;

				float contextWidth = (float)typeof(EditorGUIUtility)
				   .GetProperty("contextWidth", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);

				var rect = EditorGUILayout.GetControlRect(false, 12, checksumStyle);

				rect = new Rect(rect.x, rect.y - 6, rect.width, rect.height);
				GUI.Box(new Rect(0, rect.y, contextWidth, rect.height), "", "In BigTitle Post");

				EditorGUI.SelectableLabel(rect, $"Checksum: {ContentUtils.ComputeChecksum(contentObject)}", checksumStyle);
			}

			base.OnInspectorGUI();
		}

		public string GetTagString(string[] tags)
		{
			return string.Join(" ", tags);
		}

		public string[] GetTagsFromString(string tagString)
		{
			return tagString?.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
		}
	}
#endif

}
