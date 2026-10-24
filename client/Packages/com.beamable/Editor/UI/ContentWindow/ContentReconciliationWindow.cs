using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Content;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.ContentService;
using Beamable.Editor.Util;
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
		private Vector2 _propertyScroll, _jsonScroll, _reviewScroll;
		private float _localScroll, _realmScroll;
		private bool _simplifiedJson;
		private GUIContent[][] _rows;
		private string[] _localLines, _realmLines;
		private bool[] _jsonChangedLines;
		private float _localWidth, _realmWidth;
		private GUIStyle _body, _heading, _cell, _json;
		private GUIStyle _localHeading, _realmHeading;
		private GUIStyle _idLabel, _scopeLabel;
		private GUIStyle _presence, _dangerHeading, _summaryHeading, _primaryButton;
		private GUIContent _missingTypeMessage, _keepLocalLabel, _useRealmLabel;
		private static readonly Color AdditionColor = new Color(.12f, .78f, .52f);
		private static readonly Color DeletionColor = new Color(.95f, .25f, .30f);
		internal static readonly GUILayoutOption[] HeaderLabelOptions = { GUILayout.MinWidth(0), GUILayout.ExpandWidth(true) };
		private static readonly string[] DiffTabs = { "Properties", "JSON" };
		private static readonly string[] SummaryTabs = { "Summary", "JSON" };
		private const float RowHeight = 24;

		internal static void Open(CliContentService service, LocalContentManifestEntry entry)
		{
			var owner = focusedWindow;
			var window = GetWindow<ContentReconciliationWindow>(true, "Beam Content — Reconcile", true);
			if (window._applying) { window.Focus(); return; }
			var kind = ContentWindow_TooltipsHelper.GetChangeKind(entry);
			var summary = kind == ContentChangeKind.IncomingAddition || kind == ContentChangeKind.IncomingDeletion;
			window.minSize = new Vector2(560, summary ? 380 : 500);
			var size = new Vector2(760, summary ? 430 : 600);
			if (owner != null && owner != window)
				window.position = new Rect(owner.position.center - size / 2, size);
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
			_propertyScroll = _jsonScroll = _reviewScroll = Vector2.zero;
			_localScroll = _realmScroll = 0;
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
				_missingTypeMessage = null;
				var differences = preview.Differences ?? Array.Empty<ContentReconciliationDifference>();
				_rows = new GUIContent[differences.Length][];
				for (var i = 0; i < differences.Length; i++)
					_rows[i] = new[] { Cell(differences[i].Property), Cell(differences[i].LocalValue), Cell(differences[i].RealmValue) };
				(_localLines, _realmLines, _jsonChangedLines, _simplifiedJson) = CompareJson(preview.LocalJson, preview.RealmJson);
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
			    string.IsNullOrEmpty(preview.LocalRevision) || preview.LocalExists != !string.IsNullOrEmpty(preview.LocalJson) ||
			    preview.RealmExists != !string.IsNullOrEmpty(preview.RealmJson) || preview.Differences == null)
				throw new InvalidOperationException("The CLI did not return a complete review. Update the CLI, then refresh this review.");
			// JsonUtility reads null string fields as empty strings. Keep absent sides consistent
			// for JSON placeholders and Copy JSON, after rejecting contradictory presence/payloads.
			if (!preview.LocalExists) preview.LocalJson = null;
			if (!preview.RealmExists) preview.RealmJson = null;
		}

		internal static string ReviewErrorMessage(string error)
		{
			// Match the parser diagnostic, not an unrelated failure mentioning a preview option.
			var unsupported = error.IndexOf("Unrecognized command or argument '--preview=", StringComparison.Ordinal) >= 0 ||
			                  error.IndexOf("Unrecognized command or argument '--preview'", StringComparison.Ordinal) >= 0 ||
			                  error.IndexOf("Unrecognized command or argument '--expected-local-revision=", StringComparison.Ordinal) >= 0 ||
			                  error.IndexOf("Unrecognized command or argument '--expected-local-revision'", StringComparison.Ordinal) >= 0;
			return unsupported
				? "The installed Beam CLI does not support this reconciliation review. Update the CLI used by this Unity project, restart Unity, then reopen this review.\n\n" + error
				: error + "\nRefresh and review before choosing again.";
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

		internal static string SummaryTitle(bool localExists, bool realmExists, ContentChangeKind kind, bool missingType = false) =>
			!localExists && realmExists ? kind == ContentChangeKind.IncomingAddition ? missingType ? "New custom content in the realm" : "New item in the realm" : "Only in the published realm"
			: localExists && !realmExists ? kind == ContentChangeKind.IncomingDeletion ? "Deleted from the realm" : "Only present locally"
			: "Absent in both versions";

		internal static string PresenceLabel(bool exists, bool realm, ContentChangeKind kind) => exists ? "Present"
			: realm && kind == ContentChangeKind.IncomingDeletion ? "Deleted" : "Not present";

		// Presentation only: originals remain untouched for copying and reconciliation revision checks.
		internal static (string[] Local, string[] Realm, bool[] Changed, bool Simplified) CompareJson(string local, string realm)
		{
			var left = local?.Replace("\r\n", "\n").Split('\n') ?? Array.Empty<string>();
			var right = realm?.Replace("\r\n", "\n").Split('\n') ?? Array.Empty<string>();
			var localRows = new List<string>();
			var realmRows = new List<string>();
			int prefix = 0, suffix = 0;
			while (prefix < Math.Min(left.Length, right.Length) && left[prefix] == right[prefix])
			{
				localRows.Add(left[prefix]); realmRows.Add(right[prefix++]);
			}
			while (suffix < Math.Min(left.Length, right.Length) - prefix &&
			       left[left.Length - suffix - 1] == right[right.Length - suffix - 1]) suffix++;
			int n = left.Length - prefix - suffix, m = right.Length - prefix - suffix;
			int a = prefix, b = prefix;
			void AppendBlock(int endA, int endB)
			{
				while (a < endA || b < endB)
				{
					localRows.Add(a < endA ? left[a++] : null);
					realmRows.Add(b < endB ? right[b++] : null);
				}
			}
			// Bound both CPU and the alignment table (at most 4 MB). Large changed regions use
			// positional alignment, explicitly labelled in the UI; no source lines are discarded.
			var simplified = n > 0 && m > 0 && (long)(n + 1) * (m + 1) > 1000000;
			if (n > 0 && m > 0 && !simplified)
			{
				// Compare IDs in the quadratic loop so long JSON strings are hashed outside it.
				var ids = new Dictionary<string, int>(StringComparer.Ordinal);
				int Id(string line)
				{
					if (!ids.TryGetValue(line, out var id)) ids.Add(line, id = ids.Count);
					return id;
				}
				var leftIds = new int[n]; var rightIds = new int[m];
				for (int i = 0; i < n; i++) leftIds[i] = Id(left[prefix + i]);
				for (int j = 0; j < m; j++) rightIds[j] = Id(right[prefix + j]);
				int stride = m + 1;
				var matches = new int[(n + 1) * stride];
				for (int i = n - 1; i >= 0; i--)
					for (int j = m - 1; j >= 0; j--)
						matches[i * stride + j] = leftIds[i] == rightIds[j] ? 1 + matches[(i + 1) * stride + j + 1]
							: Math.Max(matches[(i + 1) * stride + j], matches[i * stride + j + 1]);
				int x = 0, y = 0;
				while (x < n && y < m)
				{
					if (leftIds[x] == rightIds[y])
					{
						AppendBlock(prefix + x, prefix + y);
						localRows.Add(left[a++]); realmRows.Add(right[b++]);
						x++; y++;
					}
					else if (matches[(x + 1) * stride + y] >= matches[x * stride + y + 1]) x++;
					else y++;
				}
			}
			AppendBlock(left.Length - suffix, right.Length - suffix);
			while (suffix-- > 0) { localRows.Add(left[a++]); realmRows.Add(right[b++]); }
			var changed = new bool[localRows.Count];
			for (int i = 0; i < changed.Length; i++) changed[i] = localRows[i] != realmRows[i];
			return (localRows.ToArray(), realmRows.ToArray(), changed, simplified);
		}

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
			_presence = new GUIStyle(EditorStyles.label) { richText = true, clipping = TextClipping.Clip };
			_dangerHeading = new GUIStyle(EditorStyles.boldLabel);
			_dangerHeading.normal.textColor = EditorGUIUtility.isProSkin ? DeletionColor : new Color(.65f, .08f, .12f);
			_summaryHeading = new GUIStyle(_heading) { wordWrap = true };
			_primaryButton = new GUIStyle(GUI.skin.button);
			_primaryButton.normal.background = Texture2D.whiteTexture;
			foreach (var state in new[] { _primaryButton.normal, _primaryButton.hover, _primaryButton.active, _primaryButton.focused })
			{
				state.textColor = Color.white;
			}
			_keepLocalLabel = new GUIContent(" Keep Local", EditorGUIUtility.IconContent("BuildSettings.Standalone.Small").image);
			_useRealmLabel = new GUIContent(" Use Realm", BeamGUI.iconStatusIncoming);
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
						Badge(ContentWindow_TooltipsHelper.GetStatusLabel(_entry), _realmHeading);
						GUILayout.FlexibleSpace();
						if (ContentConfiguration.Instance.EnableLocalContentInEditor) Badge("Local Mode On · Auto-sync paused", _localHeading);
						else GUILayout.Label("Auto-sync enabled", EditorStyles.miniLabel);
					}
					GUILayout.Space(8);
					if (_preview != null)
					{
						var tab = GUILayout.Toolbar(_tab, _preview.LocalExists && _preview.RealmExists ? DiffTabs : SummaryTabs, GUILayout.Height(26));
						if (tab != _tab) { _tab = tab; _reviewScroll = Vector2.zero; }
						GUILayout.Space(8);
					}
					// Only the review body scrolls: long warnings/errors cannot displace the tabs or action footer.
					_reviewScroll = EditorGUILayout.BeginScrollView(_reviewScroll, GUILayout.ExpandHeight(true));
					if (_loading) { GUILayout.Label("Loading local and published versions…", _body); GUILayout.FlexibleSpace(); }
					else if (_preview == null) GUILayout.FlexibleSpace();
					else DrawPreview();
					if (_error != null)
					{
						EditorGUILayout.HelpBox(ReviewErrorMessage(_error), MessageType.Warning);
						using (new EditorGUI.DisabledScope(_applying || _service.CurrentProgress.IsActive))
							if (GUILayout.Button("Refresh review", GUILayout.Height(26))) _ = LoadPreview();
					}
					if (_preview != null && (_tab == 1 || (_preview.LocalExists && _preview.RealmExists))) DrawChoiceCards();
					EditorGUILayout.EndScrollView();
					DrawActions();
				}
				GUILayout.Space(14);
			}
			GUILayout.Space(12);
		}

		private void DrawPreview()
		{
			var both = _preview.LocalExists && _preview.RealmExists;
			if (_tab == 1)
			{
				DrawJsonComparison();
			}
			else if (both) DrawProperties();
			else DrawSummary();
			if (_missingType && (_tab == 1 || both)) DrawMissingType();
		}

		private void DrawSummary()
		{
			var kind = ContentWindow_TooltipsHelper.GetChangeKind(_entry);
			var addition = !_preview.LocalExists && _preview.RealmExists && kind == ContentChangeKind.IncomingAddition;
			var deletion = _preview.LocalExists && !_preview.RealmExists && kind == ContentChangeKind.IncomingDeletion;
			using (new EditorGUILayout.HorizontalScope())
			{
				if (addition || deletion)
				{
					var icon = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28));
					// Reuse the Content Window's status artwork; labels carry the meaning without color.
					var texture = addition ? BeamGUI.iconStatusAdded : BeamGUI.iconStatusDeleted;
					if (texture != null) GUI.DrawTexture(icon, texture, ScaleMode.ScaleToFit);
					else { EditorGUI.DrawRect(icon, addition ? AdditionColor : DeletionColor); GUI.Label(icon, addition ? "+" : "−", _heading); }
				}
				using (new EditorGUILayout.VerticalScope())
				{
					GUILayout.Label(SummaryTitle(_preview.LocalExists, _preview.RealmExists, kind, _missingType), _summaryHeading);
					GUILayout.Label(addition ? "This item does not exist locally. Choose whether to import it."
						: deletion ? "Your local copy still exists. Choose whether to keep it or accept the deletion."
						: "This decision applies to the whole item. Open JSON to inspect its contents.", _body);
				}
			}
			GUILayout.Space(8);
			var presence = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
			EditorGUI.DrawRect(presence, new Color(.5f, .5f, .5f, .10f));
			Presence(new Rect(presence.x + 8, presence.y + 6, presence.width / 2 - 16, 20), "Local", _preview.LocalExists, false, kind);
			Presence(new Rect(presence.center.x + 8, presence.y + 6, presence.width / 2 - 16, 20), "Published realm", _preview.RealmExists, true, kind);
			EditorGUI.DrawRect(new Rect(presence.center.x, presence.y + 7, 1, 18), new Color(.5f, .5f, .5f, .25f));
			if (_missingType) { GUILayout.Space(8); DrawMissingType(); }
			GUILayout.Space(8);
			DrawChoiceCards();
		}

		private void Presence(Rect rect, string title, bool exists, bool realm, ContentChangeKind kind)
		{
			var color = ColorUtility.ToHtmlStringRGB((exists ? _realmHeading : _dangerHeading).normal.textColor);
			GUI.Label(rect, $"{title}: <color=#{color}><b>{PresenceLabel(exists, realm, kind)}</b></color>", _presence);
		}

		private void DrawMissingType()
		{
			if (_missingTypeMessage == null)
			{
				var typeName = _entry.TypeName ?? "unknown";
				// Bound the inline identifier so a long custom type cannot push JSON/actions out of the window.
				var displayName = typeName.Length > 60 ? typeName.Substring(0, 57) + "…" : typeName;
				_missingTypeMessage = new GUIContent($"Use Realm imports the JSON. The full Inspector requires the matching '{displayName}' content script to be added and compiled.", typeName);
			}
			using (var panel = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				DrawPanel(panel.rect, _localHeading.normal.textColor);
				GUILayout.Label("⚠ Matching Unity script is missing", _localHeading);
				GUILayout.Label(_missingTypeMessage, _body);
			}
		}

		private static void DrawPanel(Rect rect, Color color)
		{
			if (Event.current.type != EventType.Repaint) return;
			EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, .45f));
			var inner = new Rect(rect.x + 1, rect.y + 1, Math.Max(0, rect.width - 2), Math.Max(0, rect.height - 2));
			EditorGUI.DrawRect(inner, EditorGUIUtility.isProSkin ? new Color(.15f, .16f, .17f) : new Color(.82f, .83f, .84f));
			EditorGUI.DrawRect(inner, new Color(color.r, color.g, color.b, .08f));
		}

		private static void Badge(string text, GUIStyle style)
		{
			using (var panel = new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(false)))
			{
				DrawPanel(panel.rect, style.normal.textColor);
				GUILayout.Label(text, style);
			}
		}

		private void DrawProperties()
		{
			GUILayout.Label($"{_rows.Length} changed properties · unchanged properties hidden", EditorStyles.miniLabel);
			var header = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));
			var width = (header.width - 16) / 3;
			var localColor = _localHeading.normal.textColor;
			var realmColor = _realmHeading.normal.textColor;
			localColor.a = realmColor.a = .14f;
			EditorGUI.DrawRect(new Rect(header.x + width + 2, header.y, width - 4, RowHeight), localColor);
			EditorGUI.DrawRect(new Rect(header.x + width * 2 + 2, header.y, width - 4, RowHeight), realmColor);
			EditorGUI.DrawRect(new Rect(header.x + width + 2, header.yMax - 2, width - 4, 2), _localHeading.normal.textColor);
			EditorGUI.DrawRect(new Rect(header.x + width * 2 + 2, header.yMax - 2, width - 4, 2), _realmHeading.normal.textColor);
			GUI.Label(new Rect(header.x + 8, header.y, width - 8, RowHeight), "Property", EditorStyles.boldLabel);
			GUI.Label(new Rect(header.x + width + 8, header.y, width - 8, RowHeight), "Local", _localHeading);
			GUI.Label(new Rect(header.x + width * 2 + 8, header.y, width - 8, RowHeight), "Published realm", _realmHeading);
			var viewport = GUILayoutUtility.GetRect(0, 0, GUILayout.MinHeight(RowHeight * 4), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var view = new Rect(0, 0, Math.Max(1, viewport.width - 16), _rows.Length * RowHeight);
			_propertyScroll = GUI.BeginScrollView(viewport, _propertyScroll, view);
			VisibleRows(_propertyScroll.y, viewport.height, _rows.Length, out var first, out var end);
			for (var i = first; i < end; i++)
			{
				var row = new Rect(0, i * RowHeight, view.width, RowHeight);
				localColor.a = realmColor.a = i % 2 == 0 ? .10f : .06f;
				EditorGUI.DrawRect(new Rect(0, row.y, width - 2, RowHeight - 1), new Color(.5f, .5f, .5f, i % 2 == 0 ? .10f : .05f));
				EditorGUI.DrawRect(new Rect(width + 2, row.y, width - 4, RowHeight - 1), localColor);
				EditorGUI.DrawRect(new Rect(width * 2 + 2, row.y, width - 4, RowHeight - 1), realmColor);
				for (var column = 0; column < 3; column++)
					GUI.Label(new Rect(column * width, row.y, width, RowHeight), _rows[i][column], _cell);
			}
			GUI.EndScrollView();
			if (_rows.Length == 0) GUILayout.Label("No property or tag differences.", _body);
		}

		private void DrawJsonComparison()
		{
			var explanation = !_preview.LocalExists && !_preview.RealmExists ? "Item is absent in both versions."
				: !_preview.LocalExists ? "Not present locally · published lines shown in blue"
				: !_preview.RealmExists ? "Not present in the published realm · local lines shown in amber"
				: _simplifiedJson ? "Large diff: simplified line alignment · highlighted lines differ"
				: "Highlighted lines differ · blank cells have no matching line";
			GUILayout.Label(explanation, EditorStyles.miniLabel);
			var header = GUILayoutUtility.GetRect(0, RowHeight, GUILayout.ExpandWidth(true));
			var gutter = GUI.skin.verticalScrollbar.fixedWidth + GUI.skin.verticalScrollbar.margin.left;
			var width = Math.Max(1, (header.width - gutter - 8) / 2);
			DrawJsonHeader(new Rect(header.x, header.y, width, RowHeight), "Local", _preview.LocalJson, _localHeading);
			DrawJsonHeader(new Rect(header.x + width + 8, header.y, width, RowHeight), "Published realm", _preview.RealmJson, _realmHeading);
			var viewport = GUILayoutUtility.GetRect(0, 0, GUILayout.MinHeight(RowHeight * 4), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var view = new Rect(0, 0, Math.Max(1, viewport.width - gutter), _localLines.Length * RowHeight);
			// One vertical scroll owner prevents wheel/drag events or differing line widths from desynchronizing panes.
			_jsonScroll = GUI.BeginScrollView(viewport, _jsonScroll, view, false, true);
			VisibleRows(_jsonScroll.y, viewport.height, _localLines.Length, out var first, out var end);
			DrawJsonLines(new Rect(0, 0, width, view.height), _localLines, _localScroll, _localHeading.normal.textColor, first, end);
			DrawJsonLines(new Rect(width + 8, 0, width, view.height), _realmLines, _realmScroll, _realmHeading.normal.textColor, first, end);
			GUI.EndScrollView();
			var bars = GUILayoutUtility.GetRect(0, GUI.skin.horizontalScrollbar.fixedHeight, GUILayout.ExpandWidth(true));
			DrawJsonHorizontal(new Rect(bars.x, bars.y, width, bars.height), _localLines, ref _localScroll, ref _localWidth);
			DrawJsonHorizontal(new Rect(bars.x + width + 8, bars.y, width, bars.height), _realmLines, ref _realmScroll, ref _realmWidth);
		}

		private static void DrawJsonHeader(Rect rect, string title, string json, GUIStyle style)
		{
			GUI.Label(new Rect(rect.x, rect.y, rect.width - 84, rect.height), new GUIContent(title, json == null ? "Not present" : title), style);
			using (new EditorGUI.DisabledScope(json == null))
				if (GUI.Button(new Rect(rect.xMax - 80, rect.y, 80, 20), "Copy JSON", EditorStyles.miniButton)) EditorGUIUtility.systemCopyBuffer = json;
		}

		private void DrawJsonLines(Rect rect, string[] lines, float horizontalScroll, Color color, int first, int end)
		{
			GUI.BeginGroup(rect);
			for (int i = first; i < end; i++)
			{
				var row = new Rect(0, i * RowHeight, rect.width, RowHeight);
				if (_jsonChangedLines[i] && lines[i] != null)
				{
					EditorGUI.DrawRect(new Rect(0, row.y, 3, RowHeight - 1), color);
					color.a = .16f;
					EditorGUI.DrawRect(row, color);
					color.a = 1;
				}
				GUI.Label(new Rect(-horizontalScroll, row.y, rect.width + horizontalScroll, RowHeight), lines[i] ?? string.Empty, _json);
			}
			GUI.EndGroup();
		}

		private void DrawJsonHorizontal(Rect rect, string[] lines, ref float scroll, ref float contentWidth)
		{
			if (contentWidth == 0)
			{
				contentWidth = 1;
				var content = new GUIContent();
				foreach (var line in lines)
				{
					content.text = line;
					contentWidth = Math.Max(contentWidth, _json.CalcSize(content).x);
				}
			}
			using (new EditorGUI.DisabledScope(contentWidth <= rect.width))
				scroll = GUI.HorizontalScrollbar(rect, scroll, Math.Min(rect.width, contentWidth), 0, contentWidth);
		}

		private void DrawActions()
		{
			GUILayout.Space(10);
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
					GUI.backgroundColor = _preview != null && _preview.LocalExists && !_preview.RealmExists ? DeletionColor : new Color(0, .46f, .68f);
					if (GUILayout.Button("Use Realm", _primaryButton, GUILayout.Width(130), GUILayout.Height(30))) _ = Apply(false);
					GUI.backgroundColor = color;
				}
			}
		}

		private void ActionSummary(GUIContent title, bool keepLocal, GUIStyle heading)
		{
			using (var panel = new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(0), GUILayout.ExpandWidth(true)))
			{
				DrawPanel(panel.rect, heading.normal.textColor);
				GUILayout.Label(title, heading, GUILayout.Height(20));
				GUILayout.Label(ActionExplanation(_preview.LocalExists, _preview.RealmExists, keepLocal), _body);
				if (!keepLocal && _missingType) GUILayout.Label("Inspector support is limited.", _body);
			}
		}

		private void DrawChoiceCards()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				ActionSummary(_keepLocalLabel, true, _localHeading);
				ActionSummary(_useRealmLabel, false, _realmHeading);
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
