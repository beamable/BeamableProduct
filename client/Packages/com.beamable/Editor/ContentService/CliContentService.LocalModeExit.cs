using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor.BeamCli.Commands;
using UnityEditor;

namespace Beamable.Editor.ContentService
{
	public partial class CliContentService
	{
		// Keep ownership across failed cancellation acknowledgements until a watcher delivers its replacement snapshot.
		private string _exitReviewReloadScope;
		private IBeamCommand _localModeExitCheck;
		private Task _localModeExitCheckCompletion;
		private Task _localModeExitCheckStop;
		private bool _localModeExitStopFailed;
		private int _localModeExitTimeoutMs = 30000;

		internal static bool RequiresLocalModeReview(LocalContentManifestEntry entry) =>
			entry.IsInConflict || (ContentChangeKind)entry.ChangeKind is not
				(ContentChangeKind.UpToDate or ContentChangeKind.LocalEdit);

		private struct ExitReviewEdit
		{
			public LoadedContent Baseline;
			public string LoadError;
		}

		private Dictionary<string, ExitReviewEdit> GetExitReviewEdits(LocalContentManifest manifest)
		{
			if (_displayedLoads == null || (_loadedContent.Count == 0 && _loadErrors.Count == 0)) return null;
			Dictionary<string, ExitReviewEdit> edits = null;
			var prefix = $"{manifest.OwnerCid}/{manifest.OwnerPid}/{manifest.ManifestId}/";
			foreach (var entry in manifest.Entries)
			{
				if (!_contentScriptableCache.TryGetValue(entry.FullId, out var content) || content == null ||
				    !_displayedLoads.TryGetValue(content, out var displayed)) continue;
				var key = displayed.Key;
				if (!content.HasPendingEditorChangeNotification && !HasPendingWrites(key) &&
				    !(_saveFailures.TryGetValue(key, out var failure) && IsDisplayingRefusedEdit(entry.FullId, key, failure, out _)))
					continue;
				if (entry.StatusEnum == ContentStatus.Deleted || _contentWrites.IsDeleting(key) ||
				    !key.StartsWith(prefix, StringComparison.Ordinal)) continue;

				var edit = new ExitReviewEdit();
				if (_loadedContent.TryGetValue(key, out edit.Baseline))
				{
					if (edit.Baseline.Path != entry.JsonFilePath || edit.Baseline.Content != content) continue;
				}
				else if (!_loadErrors.TryGetValue(key, out edit.LoadError) ||
				         !EntriesCache.TryGetValue(entry.FullId, out var previous) || previous.JsonFilePath != entry.JsonFilePath)
					continue;
				(edits ??= new Dictionary<string, ExitReviewEdit>()).Add(entry.FullId, edit);
			}
			return edits;
		}

		private void RequireLocalModeExitScope(string scope, Func<bool> cancelled)
		{
			if (_watchersDisposed || (cancelled?.Invoke() ?? false) || scope != GetContentScopeKey() ||
			    !_contentConfiguration.EnableLocalContentInEditor)
				throw new InvalidOperationException("The exit review was cancelled or its realm, namespace, or mode changed. Local Mode has not been turned off.");
			if (CurrentProgress.IsActive || isReloading)
				throw new InvalidOperationException("Wait for the current content operation to finish, then review again.");
			var inspectors = HasPendingInspectorEdits?.GetInvocationList();
			var hasTrackedEdits = _contentWrites.HasActivity || _queuedPropertyWrites.Count > 0 ||
			                      _pendingTagSaves.Count > 0 || _saveFailures.Count > 0;
			if (inspectors == null && !hasTrackedEdits)
			{
				foreach (var pair in _contentScriptableCache)
					if (pair.Value != null && pair.Value.HasPendingEditorChangeNotification && EntriesCache.ContainsKey(pair.Key))
						throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then review again.");
				return;
			}

			foreach (var id in EntriesCache.Keys)
			{
				RequireInspectorNotificationsSettled(id, inspectors);
				if (!hasTrackedEdits) continue;
				var key = GetContentWriteKey(scope, id);
				if (_pendingTagSaves.ContainsKey(key) || HasPendingWrites(key) || _contentWrites.IsDeleting(key))
					throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then review again.");
				RequireNoRefusedInspectorEdit(id, key);
			}
		}

		internal async Promise RefreshLocalModeExit(string reviewedScope, Func<bool> cancelled)
		{
			RequireLocalModeExitScope(reviewedScope, cancelled);
			await ReloadAfterReconciliation(reviewedScope);
			RequireLocalModeExitScope(reviewedScope, cancelled);
		}

		internal async Promise ExitLocalMode(string reviewedScope, Func<bool> cancelled = null)
		{
			RequireLocalModeExitScope(reviewedScope, cancelled);
			if (AutoSyncState != ContentAutoSyncState.Paused)
				throw new InvalidOperationException("Wait for Local Mode watchers to be ready, or refresh them after a failure.");
			try
			{
				await WatcherGate.WaitAsync();
				try
				{
					RequireLocalModeExitScope(reviewedScope, cancelled);
					_localContentModeError = null;
					await StopLocalModeExitCheck();
					await Task.WhenAll(StopContentWatcher(preserveRequest: true), StopHistoryWatcher());
					RequireLocalModeExitScope(reviewedScope, cancelled);
					_exitReviewReloadScope = reviewedScope;
					var ready = await StartContentWatcher(reviewedScope, cancelled);
					await WaitForLocalModeActivation(ready, reviewedScope, cancelled);
					if (!_contentWatchRequested)
					{
						// Validate through the same guarded subscription, then retire it if no window owns it.
						DetachLocalModeExitWatcher();
						await StopLocalModeExitCheck();
					}
					else ClearLocalModeExitCheck(); // The active content watcher now owns this command.
					_contentConfiguration.EnableLocalContentInEditor = _observedLocalContentMode = false;
					if (_historyWatchRequested) StartContentHistoryCore();
				}
				catch
				{
					DetachLocalModeExitWatcher();
					await StopLocalModeExitCheck();
					throw;
				}
				finally { WatcherGate.Release(); }
			}
			catch
			{
				_contentConfiguration.EnableLocalContentInEditor = true;
				_exitReviewReloadScope = reviewedScope;
				try { await ApplyLocalContentMode(); } catch { /* The watcher state exposes the failure for retry. */ }
				throw;
			}
		}

		private void DetachLocalModeExitWatcher()
		{
			if (_localModeExitCheck == null || !ReferenceEquals(_contentWatcher?.Command, _localModeExitCheck)) return;
			_contentWatcher = null;
			isReloading = false;
		}

		private async Task WaitForLocalModeActivation(Task ready, string scope, Func<bool> cancelled)
		{
			var abort = new TaskCompletionSource<bool>();
			var deadline = EditorApplication.timeSinceStartup + (_localModeExitTimeoutMs > 0 ? _localModeExitTimeoutMs : 30000) / 1000d;
			void CheckAbort()
			{
				try
				{
					if (_watchersDisposed || (cancelled?.Invoke() ?? false) || scope != GetContentScopeKey() ||
					    !_contentConfiguration.EnableLocalContentInEditor)
						abort.TrySetException(new OperationCanceledException("The Local Mode exit check was cancelled. Local Mode stays on."));
					else if (EditorApplication.timeSinceStartup >= deadline)
						abort.TrySetException(new TimeoutException("The Local Mode exit check timed out. Local Mode stays on; refresh to retry."));
				}
				catch (Exception error) { abort.TrySetException(error); }
			}
			EditorApplication.update += CheckAbort;
			try
			{
				CheckAbort();
				if (abort.Task.IsCompleted) await abort.Task;
				// A later acknowledged stop may finish this task after cancellation has already won.
				_ = ready.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
				await (await Task.WhenAny(ready, abort.Task));
			}
			finally { EditorApplication.update -= CheckAbort; }
		}

		private void ValidateLocalModeActivation(BeamContentPsCommandEvent snapshot, string scope, Func<bool> cancelled)
		{
			if (_watchersDisposed || (cancelled?.Invoke() ?? false) || scope != GetContentScopeKey())
				throw new OperationCanceledException("The Local Mode exit check was cancelled.");
			var manifest = snapshot?.RelevantManifestsAgainstLatest?.SingleOrDefault(item =>
				$"{item.OwnerCid}/{item.OwnerPid}/{item.ManifestId}" == scope);
			if (snapshot?.EventType != CONTENT_PS_EVENT_TYPE_FULL_REBUILD || manifest?.Entries == null)
				throw new InvalidOperationException("The CLI did not return a complete snapshot for this realm and namespace.");
			if (manifest.Entries.Any(RequiresLocalModeReview))
				throw new InvalidOperationException("Incoming changes or conflicts still need review before leaving Local Mode.");
			if (!snapshot.LocalModeExitActivated)
				throw new InvalidOperationException("The CLI did not confirm guarded auto-sync activation. Update the CLI and review again.");
		}

		private void ClearLocalModeExitCheck()
		{
			_localModeExitCheck = null;
			_localModeExitCheckCompletion = _localModeExitCheckStop = null;
			_localModeExitStopFailed = false;
		}

		private async Task StopLocalModeExitCheck()
		{
			if (_localModeExitCheck == null) return;
			try
			{
				if (_localModeExitCheckStop == null || _localModeExitCheckStop.IsFaulted || _localModeExitCheckStop.IsCanceled)
					_localModeExitCheckStop = StopWatcher(_localModeExitCheck, _localModeExitCheckCompletion);
				await WaitForWatcherStop(_localModeExitCheckStop, "Could not confirm exit-check cleanup. Local Mode stays on. Refresh to retry before restarting watchers.");
				ClearLocalModeExitCheck();
			}
			catch { _localModeExitStopFailed = true; throw; }
		}
	}
}
