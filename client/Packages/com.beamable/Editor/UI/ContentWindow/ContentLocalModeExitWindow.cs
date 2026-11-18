using System;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Content;
using Beamable.Editor.ContentService;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.ContentWindow
{
	internal sealed class ContentLocalModeExitWindow : EditorWindow
	{
		private CliContentService _service;
		private ContentConfiguration _configuration;
		private string _scope;
		private string _error;
		private bool _busy;
		private bool _closed;
		private Vector2 _scroll;

		internal static void Open(CliContentService service, ContentConfiguration configuration)
		{
			if (!configuration.EnableLocalContentInEditor) return;
			var window = GetWindow<ContentLocalModeExitWindow>(true, "Leave Local Mode?", true);
			if (window._busy) { window.Focus(); return; }
			window._service = service;
			window._configuration = configuration;
			window._scope = service.GetContentScopeKey();
			window._closed = false;
			window.minSize = new Vector2(560, 340);
			_ = window.Refresh();
		}

		private void OnDestroy() => _closed = true;
		private void OnInspectorUpdate() => Repaint();

		private async Task Refresh()
		{
			_busy = true;
			_error = null;
			try
			{
				if (_scope != _service.GetContentScopeKey() || !_configuration.EnableLocalContentInEditor)
					throw new InvalidOperationException("The realm, namespace, or mode changed. Close this review and start again.");
				if (_service.CurrentProgress.IsActive) throw new InvalidOperationException("Wait for the current content operation to finish.");
				await _service.RefreshLocalModeExit(_scope, () => _closed);
			}
			catch (Exception error) { _error = error.Message; }
			finally { _busy = false; }
		}

		private async Task Leave()
		{
			_busy = true;
			_error = null;
			try
			{
				await _service.ExitLocalMode(_scope, () => _closed);
				if (!_closed) Close();
			}
			catch (Exception error) { _error = error.Message; }
			finally { _busy = false; }
		}

		private void OnGUI()
		{
			if (_service == null || _configuration == null) { Close(); return; }
			GUILayout.Label("Review before leaving Local Mode", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Realm / namespace", _scope);
			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			EditorGUILayout.HelpBox("Local Mode stays on while you review. Choose Keep Local or Use Realm for incoming changes and conflicts. Unrelated local edits stay local. Nothing is published.", MessageType.Info);
			var scopeChanged = _scope != _service.GetContentScopeKey() || !_configuration.EnableLocalContentInEditor;
			if (scopeChanged) EditorGUILayout.HelpBox("The realm, namespace, or mode changed. Close this review and start again.", MessageType.Warning);
			if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
			var pending = _service.EntriesCache.Values.Where(CliContentService.RequiresLocalModeReview).OrderBy(entry => entry.FullId).ToArray();
			using (new EditorGUI.DisabledScope(_busy || scopeChanged || _service.CurrentProgress.IsActive))
			{
				foreach (var entry in pending)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(new GUIContent(entry.FullId, entry.FullId));
					if (GUILayout.Button("Review…", GUILayout.Width(90))) ContentReconciliationWindow.Open(_service, entry);
					EditorGUILayout.EndHorizontal();
				}
				if (pending.Length == 0) GUILayout.Label("No incoming changes or conflicts in the current view.", EditorStyles.wordWrappedLabel);
				EditorGUILayout.EndScrollView();
				GUILayout.Label("Leaving resumes automatic updates from the realm. Restart Play Mode to use published content.", EditorStyles.wordWrappedLabel);
			}
			EditorGUILayout.BeginHorizontal();
			using (new EditorGUI.DisabledScope(_busy || scopeChanged || _service.CurrentProgress.IsActive))
				if (GUILayout.Button("Refresh")) _ = Refresh();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(_configuration.EnableLocalContentInEditor ? "Stay in Local Mode" : "Close")) Close();
			using (new EditorGUI.DisabledScope(_busy || scopeChanged || _service.CurrentProgress.IsActive || pending.Length != 0))
				if (GUILayout.Button("Leave Local Mode")) _ = Leave();
			EditorGUILayout.EndHorizontal();
			if (_busy) GUILayout.Label("Checking content… Local Mode stays on until the check succeeds.");
		}
	}
}
