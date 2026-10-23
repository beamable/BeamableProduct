using System;
using System.Threading.Tasks;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.ContentService;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	// A review owns one read-only request. Applying uses the existing service operation and watcher lifecycle.
	internal sealed class ContentReconciliationWindow : EditorWindow
	{
		private CliContentService _service;
		private string _contentId, _scope, _error;
		private LocalContentManifestEntry _entry;
		private ContentReconciliationPreview _preview;
		private ContentResolveWrapper _request;
		private int _generation, _tab;
		private bool _loading, _applying, _missingType;
		private Vector2 _propertyScroll, _localScroll, _realmScroll, _summaryScroll;
		private GUIContent[][] _rows;
		private string[] _localLines, _realmLines;
		private float _localWidth, _realmWidth;
		private GUIStyle _body, _heading, _cell, _json;
		private GUIStyle _localHeading, _realmHeading;
		private GUIStyle _idLabel, _scopeLabel;
		internal static readonly GUILayoutOption[] HeaderLabelOptions = { GUILayout.MinWidth(0), GUILayout.ExpandWidth(true) };
		private static readonly string[] DiffTabs = { "Properties", "JSON" };
		private static readonly string[] SummaryTabs = { "Summary", "JSON" };
		private const float RowHeight = 24;

		internal static void Open(CliContentService service, LocalContentManifestEntry entry)
		{
			var owner = focusedWindow;
			var window = GetWindow<ContentReconciliationWindow>(true, "Beam Content — Reconcile", true);
			if (window._applying) { window.Focus(); return; }
			window.minSize = new Vector2(560, 500);
			if (owner != null && owner != window)
				window.position = new Rect(owner.position.center - new Vector2(380, 300), new Vector2(760, 600));
			window._service = service;
			window._contentId = entry.FullId;
			window._entry = entry;
			window._scope = service.GetContentScopeKey();
			window._tab = 0;
			_ = window.LoadPreview();
		}

		private void OnDisable()
		{
			_generation++;
			_request?.Command.Cancel();
			_request = null;
		}

		private async Task LoadPreview()
		{
			var generation = ++_generation;
			_request?.Command.Cancel();
			_request = null;
			_preview = null;
			_error = null;
			_loading = true;
			_propertyScroll = _localScroll = _realmScroll = _summaryScroll = Vector2.zero;
			try
			{
				// A refresh may accept a newer publication, but never silently move to another realm/namespace.
				if (_scope != _service.GetContentScopeKey() || !_service.EntriesCache.TryGetValue(_contentId, out _entry))
					throw new InvalidOperationException("The content scope changed or the item is unavailable. Close this review and reopen the item.");
				var command = _service.CreateReconciliationPreview(_contentId, _scope, _entry.TargetManifestUid);
				_request = command;
				ContentReconciliationPreview preview = null;
				Exception reportedError = null;
				command.OnStreamContentResolveConflictResult(report => preview = report.data.Preview);
				command.OnError(report => reportedError ??= new InvalidOperationException(report.data.message));
				await command.Run();
				if (this == null || generation != _generation) return;
				if (reportedError != null) throw reportedError;
				ValidatePreview(preview, _contentId, _entry.TargetManifestUid);
				_missingType = preview.RealmExists && !_service.HasContentType(_entry.TypeName);
				var differences = preview.Differences ?? Array.Empty<ContentReconciliationDifference>();
				_rows = new GUIContent[differences.Length][];
				for (var i = 0; i < differences.Length; i++)
					_rows[i] = new[] { Cell(differences[i].Property), Cell(differences[i].LocalValue), Cell(differences[i].RealmValue) };
				_localLines = (preview.LocalJson ?? "Not present locally.").Split('\n');
				_realmLines = (preview.RealmJson ?? "Not present in the published realm.").Split('\n');
				_localWidth = _realmWidth = 0;
				_preview = preview;
			}
			catch (Exception error)
			{
				if (this != null && generation == _generation) _error = error.Message;
			}
			finally
			{
				if (this != null && generation == _generation) { _request = null; _loading = false; Repaint(); }
			}
		}

		internal static void ValidatePreview(ContentReconciliationPreview preview, string contentId, string manifestUid)
		{
			if (preview == null || preview.ContentId != contentId || preview.ManifestUid != manifestUid ||
			    string.IsNullOrEmpty(preview.LocalRevision) || preview.LocalExists != (preview.LocalJson != null) ||
			    preview.RealmExists != (preview.RealmJson != null) || preview.Differences == null)
				throw new InvalidOperationException("The CLI did not return a complete review. Update the CLI, then refresh this review.");
		}

		private static GUIContent Cell(string value)
		{
			value ??= string.Empty;
			var text = value.Replace('\n', ' ').Replace('\r', ' ');
			return new GUIContent(text.Length > 200 ? text.Substring(0, 200) + "…" : text,
				value.Length > 1000 ? value.Substring(0, 1000) + "…\nSee JSON for the complete value." : value);
		}

		internal static string ActionExplanation(bool localExists, bool realmExists, bool keepLocal) => keepLocal
			? localExists ? "Keep your local copy for a later publish." : realmExists ? "Keep it absent locally; a later publish will delete it from the realm." : "Keep this item absent locally."
			: realmExists ? localExists ? "Replace your local copy with the published version." : "Add the published item to your local content."
			: localExists ? "Delete this item from your local content." : "Keep this item absent locally.";

		internal static string SummaryTitle(bool localExists, bool realmExists, ContentChangeKind kind) =>
			!localExists && realmExists ? kind == ContentChangeKind.IncomingAddition ? "Added in the realm" : "Only in the published realm"
			: localExists && !realmExists ? kind == ContentChangeKind.IncomingDeletion ? "Deleted from the realm" : "Only present locally"
			: "Absent in both versions";

		// Shared by the property and JSON views: render only visible fixed-height rows, regardless of item size.
		internal static void VisibleRows(float scroll, float height, int count, out int first, out int end)
		{
			first = Math.Min(count, Math.Max(0, (int)(scroll / RowHeight)));
			end = Math.Min(count, first + Math.Max(0, (int)Math.Ceiling(height / RowHeight)) + 1);
		}

		private void EnsureStyles()
		{
			if (_body != null) return;
			_body = new GUIStyle(EditorStyles.label) { wordWrap = true };
			_heading = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
			_cell = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip, padding = new RectOffset(8, 8, 3, 3) };
			_json = new GUIStyle(EditorStyles.label) { richText = false, wordWrap = false, padding = new RectOffset(6, 6, 3, 3) };
			_localHeading = new GUIStyle(EditorStyles.boldLabel);
			_realmHeading = new GUIStyle(EditorStyles.boldLabel);
			_idLabel = new GUIStyle(EditorStyles.boldLabel) { clipping = TextClipping.Clip };
			_scopeLabel = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
			_localHeading.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1, .77f, .24f) : new Color(.48f, .31f, 0);
			_realmHeading.normal.textColor = EditorGUIUtility.isProSkin ? new Color(.18f, .75f, 1) : new Color(0, .34f, .55f);
		}

		private void OnGUI()
		{
			EnsureStyles();
			EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), EditorGUIUtility.isProSkin
				? new Color(.15f, .16f, .17f) : new Color(.82f, .83f, .84f));
			if (_service == null)
			{
				EditorGUILayout.HelpBox("Scripts reloaded. Reopen this item from Beam Content to review its latest versions.", MessageType.Info);
				if (GUILayout.Button("Close")) Close();
				return;
			}
			GUILayout.Space(12);
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(14);
				using (new EditorGUILayout.VerticalScope())
				{
					GUILayout.Label("Reconcile Content", _heading);
					GUILayout.Label(new GUIContent(_contentId, _contentId), _idLabel, HeaderLabelOptions);
					var scopeLabel = "Realm / namespace: " + _scope;
					GUILayout.Label(new GUIContent(scopeLabel, scopeLabel), _scopeLabel, HeaderLabelOptions);
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Label(ContentWindow_TooltipsHelper.GetStatusLabel(_entry), _realmHeading);
						GUILayout.FlexibleSpace();
						GUILayout.Label(ContentConfiguration.Instance.EnableLocalContentInEditor ? "Local Mode On · Auto-sync paused" : "Auto-sync enabled", EditorStyles.miniLabel);
					}
					GUILayout.Space(8);
					if (_loading) { GUILayout.Label("Loading local and published versions…", _body); GUILayout.FlexibleSpace(); }
					else if (_preview == null) GUILayout.FlexibleSpace();
					else DrawPreview();
					if (_error != null)
					{
						EditorGUILayout.HelpBox(_error + "\nRefresh and review before choosing again.", MessageType.Warning);
						using (new EditorGUI.DisabledScope(_applying || _service.CurrentProgress.IsActive))
							if (GUILayout.Button("Refresh review", GUILayout.Height(26))) _ = LoadPreview();
					}
					DrawActions();
				}
				GUILayout.Space(14);
			}
			GUILayout.Space(12);
		}

		private void DrawPreview()
		{
			var both = _preview.LocalExists && _preview.RealmExists;
			_tab = GUILayout.Toolbar(_tab, both ? DiffTabs : SummaryTabs, GUILayout.Height(26));
			GUILayout.Space(8);
			if (_tab == 1)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					DrawJson("Local", _preview.LocalJson, _localLines, ref _localScroll, ref _localWidth);
					DrawJson("Published realm", _preview.RealmJson, _realmLines, ref _realmScroll, ref _realmWidth);
				}
			}
			else if (both) DrawProperties();
			else
			{
				_summaryScroll = EditorGUILayout.BeginScrollView(_summaryScroll, GUILayout.ExpandHeight(true));
				GUILayout.Label(SummaryTitle(_preview.LocalExists, _preview.RealmExists, ContentWindow_TooltipsHelper.GetChangeKind(_entry)), _heading);
				GUILayout.Space(12);
				using (new EditorGUILayout.HorizontalScope())
				{
					Presence("Local", _preview.LocalExists);
					Presence("Published realm", _preview.RealmExists);
				}
				GUILayout.Space(12);
				GUILayout.Label("This decision applies to the whole item. Open JSON to inspect its contents.", _body);
				EditorGUILayout.EndScrollView();
			}
			if (_missingType)
				EditorGUILayout.HelpBox($"Content script not found locally for '{_entry.TypeName}'. Use Realm imports the JSON, but the Inspector cannot show all properties until the matching content script is added and compiled.", MessageType.Warning);
		}

		private void Presence(string title, bool exists)
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
			{
				GUILayout.Label(title, EditorStyles.boldLabel);
				GUILayout.Label(exists ? "Present" : "Not present");
			}
		}

		private void DrawProperties()
		{
			GUILayout.Label($"{_rows.Length} changed properties · unchanged properties hidden", EditorStyles.miniLabel);
			var header = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));
			var width = (header.width - 16) / 3;
			GUI.Label(new Rect(header.x, header.y, width, RowHeight), "Property", EditorStyles.boldLabel);
			GUI.Label(new Rect(header.x + width, header.y, width, RowHeight), "Local", _localHeading);
			GUI.Label(new Rect(header.x + width * 2, header.y, width, RowHeight), "Published realm", _realmHeading);
			var viewport = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var view = new Rect(0, 0, Math.Max(1, viewport.width - 16), _rows.Length * RowHeight);
			_propertyScroll = GUI.BeginScrollView(viewport, _propertyScroll, view);
			VisibleRows(_propertyScroll.y, viewport.height, _rows.Length, out var first, out var end);
			for (var i = first; i < end; i++)
			{
				var row = new Rect(0, i * RowHeight, view.width, RowHeight);
				if (i % 2 == 0) EditorGUI.DrawRect(row, new Color(0, 0, 0, .08f));
				for (var column = 0; column < 3; column++)
					GUI.Label(new Rect(column * width, row.y, width, RowHeight), _rows[i][column], _cell);
			}
			GUI.EndScrollView();
			if (_rows.Length == 0) GUILayout.Label("No property or tag differences.", _body);
		}

		private void DrawJson(string title, string json, string[] lines, ref Vector2 scroll, ref float contentWidth)
		{
			using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Label(title, EditorStyles.boldLabel);
					using (new EditorGUI.DisabledScope(json == null))
						if (GUILayout.Button("Copy JSON", EditorStyles.miniButton, GUILayout.Width(80))) EditorGUIUtility.systemCopyBuffer = json;
				}
				if (contentWidth == 0)
				{
					contentWidth = 1;
					foreach (var line in lines) contentWidth = Math.Max(contentWidth, _json.CalcSize(new GUIContent(line)).x);
				}
				var viewport = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
				var view = new Rect(0, 0, Math.Max(contentWidth, viewport.width - 16), lines.Length * RowHeight);
				scroll = GUI.BeginScrollView(viewport, scroll, view);
				VisibleRows(scroll.y, viewport.height, lines.Length, out var first, out var end);
				for (var i = first; i < end; i++) GUI.Label(new Rect(0, i * RowHeight, view.width, RowHeight), lines[i], _json);
				GUI.EndScrollView();
			}
		}

		private void DrawActions()
		{
			GUILayout.Space(10);
			if (_preview != null)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					ActionSummary("Keep Local", true, _localHeading);
					ActionSummary("Use Realm", false, _realmHeading);
				}
			}
			GUILayout.Label(_applying ? "Reconciling… Please wait." : "Only this item is affected. Nothing is published.", EditorStyles.miniLabel);
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledScope(_applying))
					if (GUILayout.Button("Cancel", GUILayout.Width(90), GUILayout.Height(30))) Close();
				GUILayout.FlexibleSpace();
				using (new EditorGUI.DisabledScope(_preview == null || _loading || _applying || _error != null || _service.CurrentProgress.IsActive))
				{
					var invalid = _entry.IsInConflict && _service.IsContentInvalid(_contentId);
					using (new EditorGUI.DisabledScope(invalid))
						if (GUILayout.Button(new GUIContent("Keep Local", invalid ? "Fix local validation errors before keeping this conflict." : ""), GUILayout.Width(130), GUILayout.Height(30))) _ = Apply(true);
					var color = GUI.backgroundColor;
					GUI.backgroundColor = _preview != null && !_preview.RealmExists ? new Color(1, .65f, .65f) : new Color(.45f, .78f, 1);
					if (GUILayout.Button("Use Realm", GUILayout.Width(130), GUILayout.Height(30))) _ = Apply(false);
					GUI.backgroundColor = color;
				}
			}
		}

		private void ActionSummary(string title, bool keepLocal, GUIStyle heading)
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
			{
				GUILayout.Label(title, heading);
				GUILayout.Label(ActionExplanation(_preview.LocalExists, _preview.RealmExists, keepLocal), _body);
			}
		}

		private async Task Apply(bool keepLocal)
		{
			if (_applying || _preview == null || _error != null) return;
			_applying = true;
			try
			{
				await _service.ReconcileContentWithProgress(_contentId, keepLocal, _scope, _preview.ManifestUid, _preview.LocalRevision);
				if (this != null) Close();
			}
			catch (Exception error) { if (this != null) _error = error.Message; }
			finally { if (this != null) { _applying = false; Repaint(); } }
		}
	}
}
