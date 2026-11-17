using System;
using System.Linq;
using Beamable.Editor.ContentService;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
    internal sealed class ContentSchemaNotice : PopupWindowContent
    {
        internal const string Title = "Content differs from your local C# model";
        internal const string Description = "Normal edits preserve unsupported fields. If your scripts may be out of date, update them and let Unity compile before reviewing JSON changes.";
        private readonly CliContentService _service;
        private readonly string _id, _scope;
        internal ContentSchemaNotice(CliContentService service, string id)
        { _service = service; _id = id; _scope = service.GetContentScopeKey(); }
        public override Vector2 GetWindowSize() => new Vector2(460, 145);
        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginArea(new Rect(10, 8, rect.width - 20, rect.height - 16));
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(Title, EditorStyles.boldLabel);
                if (GUILayout.Button("\u00d7", EditorStyles.miniButton, GUILayout.Width(22))) editorWindow.Close();
            }
            GUILayout.Label(Description, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_scope != _service.GetContentScopeKey() ||
                       _service.CurrentProgress.IsActive || EditorApplication.isCompiling))
            {
                if (GUILayout.Button("Review schema changes\u2026"))
                {
                    var service = _service;
                    var id = _id;
                    var scope = _scope;
                    editorWindow.Close();
                    EditorApplication.delayCall += () =>
                    {
                        if (scope == service.GetContentScopeKey()) ContentSchemaWindow.Open(service, id);
                    };
                }
            }
            GUILayout.EndArea();
        }
    }

    internal sealed class ContentSchemaWindow : EditorWindow
    {
        private CliContentService _service;
        private CliContentService.SchemaMigrationPreview _preview;
        private ContentSchemaPreview _comparison;
        private string _id, _error, _scope;
        private ContentSchemaPreview.Row[][] _groups;
        private string _summary;
        private bool _applying;
        private int _tab;
        private Vector2 _scroll, _leftScroll, _rightScroll;
        private GUIStyle _cell;
        private static readonly string[] Tabs = { "Properties", "JSON" };
        private static readonly string[] Groups = { "Fields removed", "Defaults added", "Values changed", "Unchanged" };

        internal static ContentSchemaWindow Open(CliContentService service, string id)
        {
            var window = CreateInstance<ContentSchemaWindow>();
            window.titleContent = new GUIContent("Match JSON to the local C# model");
            window.minSize = new Vector2(720, 500);
            window.position = new Rect(100, 100, 860, 600);
            window._service = service;
            window._id = id;
            window._scope = service.GetContentScopeKey();
            window.Review();
            window.ShowUtility();
            return window;
        }

        private void Review()
        {
            _error = null;
            _preview = null;
            _comparison = null;
            try
            {
                if (_scope != _service.GetContentScopeKey())
                    throw new InvalidOperationException("Realm or namespace changed. Close this window and review the item in the current scope.");
                var preview = _service.PreviewSchemaMigration(_id);
                var comparison = new ContentSchemaPreview(preview.OriginalJson, preview.ProposedProperties);
                _preview = preview;
                _comparison = comparison;
                _groups = Enumerable.Range(0, Groups.Length).Select(i => comparison.Rows.Where(r => (int)r.Kind == i).ToArray()).ToArray();
                _summary = $"{_groups[0].Length} removed \u00b7 {_groups[1].Length} added \u00b7 {_groups[2].Length} changed \u00b7 {_groups[3].Length} unchanged";
            }
            catch (Exception ex) { _error = ex.Message; }
        }

        private async void Apply()
        {
            if (_applying || _preview == null) return;
            _applying = true;
            try
            {
                await _service.ApplySchemaMigration(_preview);
                if (this != null) Close();
            }
            catch (Exception ex)
            {
                if (this == null) return;
                _error = ex.Message;
                // Never reuse a failed/stale preview. A fresh review requires an explicit user action.
                _preview = null;
            }
            finally { _applying = false; if (this != null) Repaint(); }
        }

        private void OnGUI()
        {
            if (_service == null) { EditorGUILayout.HelpBox("Content was reloaded. Close this window and review the item again.", MessageType.Info); return; }
            _cell ??= new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip, padding = new RectOffset(6, 6, 2, 2) };
            using (new EditorGUILayout.VerticalScope(new GUIStyle { padding = new RectOffset(12, 12, 10, 10) }))
            {
                GUILayout.Label(_id, EditorStyles.boldLabel);
                GUILayout.Label("Scope: " + _scope, EditorStyles.miniLabel);
                GUILayout.Label("Reference: currently loaded local C# model", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox("Applying can remove fields introduced by newer versions. If your scripts may be out of date, update them and let Unity compile first.", MessageType.Warning);
                GUILayout.Label("No check against main or a published model.", EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
                if (_comparison != null)
                {
                    GUILayout.Label(_summary, EditorStyles.miniLabel);
                    _tab = GUILayout.Toolbar(_tab, Tabs, GUILayout.Width(220));
                    if (_tab == 0) DrawProperties(); else DrawJson();
                }
                else GUILayout.FlexibleSpace();
                GUILayout.Label("Removed fields and values are deleted. Missing fields get defaults.", EditorStyles.wordWrappedLabel);
                GUILayout.Label("C# scripts stay unchanged.", EditorStyles.wordWrappedLabel);
                GUILayout.Space(8);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Only this item. Saves locally. Does not publish.", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_applying))
                        if (GUILayout.Button("Cancel", GUILayout.Width(75))) Close();
                    var unavailable = _applying || EditorApplication.isCompiling || _service.CurrentProgress.IsActive;
                    using (new EditorGUI.DisabledScope(unavailable))
                    {
                        if (_preview == null)
                        {
                            if (GUILayout.Button("Review again", GUILayout.Width(165))) Review();
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(!_comparison.Rows.Any(r => r.Kind != ContentSchemaPreview.Change.Unchanged)))
                                if (GUILayout.Button(_applying ? "Saving\u2026" : "Apply changes to JSON", GUILayout.Width(165))) Apply();
                        }
                    }
                }
            }
        }

        private void DrawProperties()
        {
            var header = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            DrawCells(header, "Property", "Current JSON", "After changes", EditorStyles.boldLabel);
            var viewport = GUILayoutUtility.GetRect(0, 10000, 80, 10000, GUILayout.ExpandHeight(true));
            const float rowHeight = 24;
            var count = _groups.Sum(rows => rows.Length + (rows.Length > 0 ? 1 : 0));
            var view = new Rect(0, 0, Mathf.Max(0, viewport.width - 16), count * rowHeight);
            _scroll = GUI.BeginScrollView(viewport, _scroll, view);
            ContentReconciliationWindow.VisibleRows(_scroll.y, viewport.height, count, out var first, out var end);
            var index = 0;
            for (var group = 0; group < Groups.Length; group++)
            {
                var rows = _groups[group];
                if (rows.Length == 0) continue;
                if (index >= first && index < end)
                    GUI.Label(new Rect(0, index * rowHeight, view.width, rowHeight), $"{Groups[group]} ({rows.Length})", EditorStyles.boldLabel);
                index++;
                for (var i = Math.Max(0, first - index); i < rows.Length && index + i < end; i++)
                {
                    var row = rows[i];
                    var rect = new Rect(0, (index + i) * rowHeight, view.width, rowHeight);
                    if (row.Kind != ContentSchemaPreview.Change.Unchanged)
                        EditorGUI.DrawRect(rect, row.Kind == ContentSchemaPreview.Change.Removed ? new Color(.7f, .2f, .2f, .22f) :
                            row.Kind == ContentSchemaPreview.Change.Added ? new Color(.2f, .65f, .2f, .22f) : new Color(.7f, .55f, .1f, .22f));
                    DrawCells(rect, row.Path, row.Before, row.After, _cell);
                }
                index += rows.Length;
            }
            GUI.EndScrollView();
        }

        private static void DrawCells(Rect rect, string name, string before, string after, GUIStyle style)
        {
            rect.width /= 3;
            foreach (var value in new[] { name, before, after })
            {
                var text = value.Length > 1000 ? value.Substring(0, 1000) + "\u2026 (see JSON)" : value;
                GUI.Label(rect, new GUIContent(text, text), style);
                rect.x += rect.width;
            }
        }

        private void DrawJson()
        {
            GUILayout.Label("Content properties \u00b7 read-only", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawJsonSide("Current JSON", _comparison.CurrentJson, ref _leftScroll);
                DrawJsonSide("After changes", _comparison.ProposedJson, ref _rightScroll);
            }
        }

        private static void DrawJsonSide(string title, string json, ref Vector2 scroll)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width((EditorGUIUtility.currentViewWidth - 40) / 2)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(title, EditorStyles.boldLabel);
                    if (GUILayout.Button("Copy", GUILayout.Width(45))) GUIUtility.systemCopyBuffer = json;
                }
                scroll = EditorGUILayout.BeginScrollView(scroll);
                // SelectableLabel is read-only; edits must never change the reviewed payload.
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };
                var size = style.CalcSize(new GUIContent(json));
                EditorGUILayout.SelectableLabel(json, style, GUILayout.Width(Mathf.Max(300, size.x)), GUILayout.Height(Mathf.Max(200, size.y)));
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
