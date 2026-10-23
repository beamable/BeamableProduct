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
using System.Collections.Generic;
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
		private Action _pendingNameEdit;
		private Action _pendingTagEdit;
		private CliContentService _editingService;
		private string _editingContentId;
		private bool _editorDisabled;

		private void ObservePendingEdits(CliContentService service)
		{
			_editorDisabled = false;
			_editingContentId = (target as ContentObject)?.Id;
			if (ReferenceEquals(_editingService, service)) return;
			if (_editingService != null) _editingService.HasPendingInspectorEdits -= HasPendingEdits;
			_editingService = service;
			service.HasPendingInspectorEdits += HasPendingEdits;
		}

		private bool HasPendingEdits(string contentId) => _editingContentId == contentId &&
			(_pendingNameEdit != null || _pendingTagEdit != null);

		private void CompleteHeaderEdit(bool name)
		{
			var pending = name ? _pendingNameEdit : _pendingTagEdit;
			if (name) { _pendingNameEdit = null; _updateNameCoroutine = null; }
			else { _pendingTagEdit = null; _updateTagCoroutine = null; }
			try { pending?.Invoke(); }
			finally { ReleasePendingEditsObserver(); }
		}

		private void OnDisable()
		{
			// Keep the guard until already-scheduled callbacks finish; don't reorder their writes on deselection.
			_editorDisabled = true;
			ReleasePendingEditsObserver();
		}

		private void ReleasePendingEditsObserver()
		{
			if (!_editorDisabled || _pendingNameEdit != null || _pendingTagEdit != null) return;
			if (_editingService != null) _editingService.HasPendingInspectorEdits -= HasPendingEdits;
			_editingService = null;
		}
		private readonly Dictionary<string, int> _listValidationErrors = new();
		private bool _hasListValidationCache;
		private Guid _listValidationRevision;
		private int _listManifestRevision;
		private int _listDirtyCount;

		protected override void OnHeaderGUI()
		{
			var contentObject = target as ContentObject;
			if (contentObject == null) return;
			var contentService = BeamEditorContext.Default.ServiceScope.GetService<CliContentService>();
			ObservePendingEdits(contentService);
			using (new EditorGUI.DisabledScope(ContentHistoryInspectorPreview.IsReadOnly(contentObject) || contentService.CurrentProgress.IsActive))
			{
			var isEditingMultiple = targets.Length > 1;

			var boldLabelFieldStyle =
				new GUIStyle(EditorStyles.boldLabel) {fixedHeight = EditorGUIUtility.singleLineHeight};

			var isModified = contentObject.ContentStatus is ContentStatus.Modified or ContentStatus.Created or ContentStatus.Deleted ||
				(contentService.EntriesCache.TryGetValue(contentObject.Id, out var headerEntry) && ContentWindow_TooltipsHelper.NeedsReconciliation(headerEntry));
			var isInConflict = contentObject.IsInConflict;

			var headerHeight = (isModified || isInConflict) && !isEditingMultiple ? BUTTONS_HEADER_HEIGHT : HEADER_HEIGHT;

			ContentNameValidationException.HasNameValidationErrors(contentObject, contentObject.ContentName,
			                                                       out var nameErrors);
			headerHeight += (nameErrors?.Count ?? 0) * 5;
			var topPadding = ((nameErrors?.Count ?? 0) + 1) * 5;
			
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
				
				const float statusIconSize = 15f;
				if (!ContentHistoryInspectorPreview.IsReadOnly(contentObject) && contentObject.ContentStatus is not ContentStatus.UpToDate)
				{
					var statusIconRect = new Rect(
						iconRect.xMin,
						iconRect.yMin,
						statusIconSize,
						statusIconSize);
					var icon = ContentWindow_TooltipsHelper.GetStatusIcon(GetCachedEntry(contentObject));
					GUI.DrawTexture(statusIconRect, icon, ScaleMode.ScaleToFit);
				}
				// A dependency can become invalid without changing this content's own file/status.
				if (contentService.IsContentInvalid(contentObject.Id))
				{
					var invalidIconRect = new Rect(
						iconRect.xMax - statusIconSize / 2f,
						iconRect.yMin,
						statusIconSize,
						statusIconSize);
					GUI.DrawTexture(invalidIconRect, BeamGUI.iconStatusInvalid, ScaleMode.ScaleToFit);
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
		}

		private GUIStyle _contentActionButtonStyle;
		private void DrawContentButtons(ContentObject content, Rect buttonsRect, CliContentService contentService)
		{
			if (contentService.CurrentProgress.IsActive)
			{
				EditorGUI.ProgressBar(buttonsRect, contentService.CurrentProgress.Progress, contentService.CurrentProgress.Description);
				return;
			}
			_contentActionButtonStyle ??= new GUIStyle(GUI.skin.button)
			{
				alignment = TextAnchor.MiddleLeft,     
				padding = new RectOffset(10, 36, 5, 5),
			};
			var buttonStyle = _contentActionButtonStyle;
			float modifiedButtonWidth = 0;
			bool incoming = contentService.EntriesCache.TryGetValue(content.Id, out var entry) &&
				ContentWindow_TooltipsHelper.NeedsReconciliation(entry);
			bool hasModifiedButton = incoming || content.ContentStatus is ContentStatus.Deleted or ContentStatus.Modified or ContentStatus.Created;
			bool hasConflictSolveButton = content.IsInConflict;
			if (hasModifiedButton)
			{
				var revertContent = new GUIContent(incoming ? "Reconcile" : "Revert");
				modifiedButtonWidth = buttonStyle.CalcSize(revertContent).x;
				
				var revertButtonRect = new Rect(
					buttonsRect.xMin,
					buttonsRect.y,
					modifiedButtonWidth,
					buttonsRect.height);
				if (GUI.Button(revertButtonRect, revertContent, buttonStyle))
				{
					if (incoming)
					{
						ContentWindow_TooltipsHelper.ShowReconciliationDialog(contentService, entry);
					}
					else if (EditorUtility.DisplayDialog("Revert Content",
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
					buttonStyle.CalcSize(conflictContent).x,
					buttonsRect.height);
				if (GUI.Button(conflictSolveButtonRect, conflictContent, buttonStyle))
				{
					if (contentService.EntriesCache.TryGetValue(content.Id, out var reviewed))
						ContentWindow_TooltipsHelper.ShowReconciliationDialog(contentService, reviewed);
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

					var scope = contentService.GetContentScopeKey();
					_pendingNameEdit = () =>
					{
						if (contentObject == null || contentObject.ContentStatus == ContentStatus.Deleted ||
						    scope != contentService.GetContentScopeKey()) return;
						contentService.RenameContent(contentObject.Id, newName);
						contentObject.SetContentName(newName);
						EditorUtility.SetDirty(contentObject);
					};
					_updateNameCoroutine = EditorCoroutineUtility.StartCoroutine(DelayedActionEditorRoutine(0.5d, () =>
					{
						CompleteHeaderEdit(true);
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

					var scope = contentService.GetContentScopeKey();
					_pendingTagEdit = () =>
					{
						// This Inspector debounce is separate from ContentObject's pending autosave.
						if (contentObject == null || contentObject.ContentStatus == Common.BeamCli.Contracts.ContentStatus.Deleted ||
						    scope != contentService.GetContentScopeKey()) return;
						var tags = GetTagsFromString(newTags);
						contentService.SetContentTags(contentObject.Id, tags);
						contentObject.Tags = tags.ToArray();
						EditorUtility.SetDirty(contentObject);
					};
					_updateTagCoroutine = EditorCoroutineUtility.StartCoroutine(DelayedActionEditorRoutine(0.5d, () =>
					{
						CompleteHeaderEdit(false);
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
				redStyle.normal.textColor = BeamGUI.ContentErrorColor;
				redStyle.fontSize = 10;
				EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y-5, headerRect.width, 12), $"({errorText})", redStyle);
					
			}
		}
		


		public override void OnInspectorGUI()
		{
			var contentObject = target as ContentObject;
			if (contentObject == null) return;
			var busy = BeamEditorContext.Default.CliContentService.CurrentProgress.IsActive;
			using (new EditorGUI.DisabledScope(ContentHistoryInspectorPreview.IsReadOnly(contentObject) || busy))
			{

			if (contentObject.ContentStatus is Common.BeamCli.Contracts.ContentStatus.Deleted)
			{
				// Deleted also represents incoming additions with no local file; use the authoritative presentation.
				DrawContentStatus();
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

				EditorGUI.SelectableLabel(rect, $"Checksum: {contentObject.GetEditorDataChecksum()}", checksumStyle);
			}
			
			DrawContentProperties(contentObject);
			if (!busy) contentObject.CheckForNonDetectedChanges();
			}
			DrawContentStatus();
		}

		private GUIStyle _statusMessageStyle;
		private int _statusManifestRevision = -1;
		private int _statusProgressRevision = -1;

		public override bool RequiresConstantRepaint()
		{
			if (!BeamEditor.IsInitialized) return false;
			// Unity already asks Inspectors whether to repaint. Read only the existing cache revision.
			var service = BeamEditorContext.Default.CliContentService;
			var revision = service.ManifestChangedCount;
			if (_statusManifestRevision == revision && _statusProgressRevision == service.ProgressUpdateVersion) return false;
			_statusManifestRevision = revision;
			_statusProgressRevision = service.ProgressUpdateVersion;
			return true;
		}

		private static LocalContentManifestEntry GetCachedEntry(ContentObject content)
		{
			if (content != null && !string.IsNullOrEmpty(content.Id) && BeamEditor.IsInitialized &&
				BeamEditorContext.Default.CliContentService.EntriesCache.TryGetValue(content.Id, out var entry)) return entry;
			return default;
		}

		private void DrawContentStatus()
		{
			var entries = new List<LocalContentManifestEntry>(targets.Length);
			var historical = 0;
			var hasChanges = false;
			var onlyIncoming = targets.Length > 0;
			foreach (var selected in targets)
			{
				var content = selected as ContentObject;
				if (ContentHistoryInspectorPreview.IsReadOnly(content))
				{
					historical++;
					onlyIncoming = false;
				}
				else
				{
					var entry = GetCachedEntry(content);
					entries.Add(entry);
					var kind = ContentWindow_TooltipsHelper.GetChangeKind(entry);
					onlyIncoming &= ContentWindow_TooltipsHelper.IsIncoming(kind);
					hasChanges |= kind == ContentChangeKind.LocalEdit || ContentWindow_TooltipsHelper.IsIncoming(kind) ||
						(kind == ContentChangeKind.Unknown && entry.StatusEnum is ContentStatus.Modified or ContentStatus.Created or ContentStatus.Deleted);
				}
			}
			var message = ContentWindow_TooltipsHelper.GetSelectionDescription(entries, historical,
				ContentConfiguration.Instance.EnableLocalContentInEditor, out var conflict);
			_statusMessageStyle ??= new GUIStyle(EditorStyles.wordWrappedLabel) { padding = new RectOffset(8, 8, 8, 8) };
			GUILayout.Space(8);
			var text = new GUIContent(message);
			var width = Mathf.Max(60, EditorGUIUtility.currentViewWidth - 60);
			var height = Mathf.Max(42, _statusMessageStyle.CalcHeight(text, width));
			var rect = EditorGUILayout.GetControlRect(false, height);
			EditorGUI.DrawRect(rect, conflict ? new Color(.7f, .15f, .15f, .24f) : new Color(.15f, .4f, .75f, .24f));
			var icon = conflict ? "console.erroricon.sml" : hasChanges ? "console.warnicon.sml" : "console.infoicon.sml";
			GUI.Label(new Rect(rect.x + 6, rect.y + 8, 20, 20), onlyIncoming
				? new GUIContent(ContentWindow_TooltipsHelper.GetStatusIcon(entries[0]))
				: EditorGUIUtility.IconContent(icon));
			rect.xMin += 28;
			GUI.Label(rect, text, _statusMessageStyle);
		}

		private void DrawContentProperties(ContentObject contentObject)
		{
			serializedObject.UpdateIfRequiredOrScript();
			RefreshListValidation(contentObject);
			var property = serializedObject.GetIterator();
			bool enterChildren = true;
			while (property.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (_listValidationErrors.TryGetValue(property.propertyPath, out var count))
				{
					var errors = count == 1 ? "1 validation error" : $"{count} validation errors";
					EditorGUILayout.HelpBox($"{property.displayName} contains {errors}. Expand the list to inspect the affected fields.", MessageType.Error);
				}
				// Retain Unity's default list controls and all existing nested property drawers.
				using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
				{
					EditorGUILayout.PropertyField(property, true);
				}
			}
			if (serializedObject.ApplyModifiedProperties()) _hasListValidationCache = false;
		}

		private void RefreshListValidation(ContentObject contentObject)
		{
			if (serializedObject.isEditingMultipleObjects || !BeamEditor.IsInitialized)
			{
				_listValidationErrors.Clear();
				_hasListValidationCache = false;
				return;
			}
			var service = BeamEditorContext.Default.CliContentService;
			var context = service.GetValidationContext();
			if (!context.Initialized)
			{
				_listValidationErrors.Clear();
				_hasListValidationCache = false;
				return;
			}
			int dirtyCount = EditorUtility.GetDirtyCount(contentObject);
			if (_hasListValidationCache && _listValidationRevision == contentObject.ValidationGuid &&
			    _listManifestRevision == service.ManifestChangedCount && _listDirtyCount == dirtyCount) return;

			// Validate once per edit/manifest change, never once per list on every repaint.
			_listValidationErrors.Clear();
			var errors = contentObject.GetMemberValidationErrors(context);
			var property = serializedObject.GetIterator();
			bool enterChildren = true;
			while (property.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (!property.isArray || property.propertyType == SerializedPropertyType.String) continue;
				int count = CountListValidationErrors(property, errors);
				if (count > 0) _listValidationErrors[property.propertyPath] = count;
			}
			_listValidationRevision = contentObject.ValidationGuid;
			_listManifestRevision = service.ManifestChangedCount;
			_listDirtyCount = dirtyCount;
			_hasListValidationCache = true;
		}

		internal static int CountListValidationErrors(SerializedProperty list, List<ContentValidationException> errors)
		{
			var matches = new HashSet<ContentValidationException>();
			var property = list.Copy();
			var end = list.GetEndProperty();
			do
			{
				object parent = null;
				bool parentResolved = false;
				foreach (var error in errors)
				{
					if (error.Info.Field.Name != property.name) continue;
					// Resolving a list element walks its parent path; do it once, even if many errors share this field name.
					if (!parentResolved)
					{
						parent = ContentRefPropertyDrawer.GetTargetParentObjectOfProperty(property);
						parentResolved = true;
					}
					if (Equals(error.Info.Target, parent)) matches.Add(error);
				}
				// Next (not NextVisible) includes fields hidden inside collapsed list elements.
			} while (property.Next(true) && !SerializedProperty.EqualContents(property, end));
			return matches.Count;
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
