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
		private int _localModeExitStopTimeoutMs = 10000;

		internal static bool RequiresLocalModeReview(LocalContentManifestEntry entry) =>
			entry.IsInConflict || (ContentChangeKind)entry.ChangeKind is not
				(ContentChangeKind.UpToDate or ContentChangeKind.LocalEdit);

		private Dictionary<string, LoadedContent> GetExitReviewEdits(LocalContentManifest manifest)
		{
			var edits = new Dictionary<string, LoadedContent>();
			foreach (var entry in manifest.Entries)
			{
				var key = GetContentWriteKey(entry.FullId);
				if (entry.StatusEnum == ContentStatus.Deleted || _contentWrites.IsDeleting(key) ||
				    !_loadedContent.TryGetValue(key, out var loaded) || loaded.Path != entry.JsonFilePath ||
				    !_contentScriptableCache.TryGetValue(entry.FullId, out var content) || content == null || loaded.Content != content)
					continue;
				if (content.HasPendingEditorChangeNotification || HasPendingWrites(key) ||
				    (TryGetSaveFailure(entry.FullId, out var failure) && CanDiscardEdit(entry.FullId, failure)))
					edits.Add(entry.FullId, loaded);
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
			foreach (var id in EntriesCache.Keys)
			{
				RequireSavedInspectorEdits(id);
				var key = GetContentWriteKey(id);
				if (HasPendingWrites(key) || _contentWrites.IsDeleting(key))
					throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then review again.");
				if (TryGetSaveFailure(id, out var failure) && CanDiscardEdit(id, failure))
					throw new InvalidOperationException($"The Inspector edit for {id} was not saved. Save or discard it in the Inspector before leaving Local Mode.");
			}
		}

		internal async Promise RefreshLocalModeExit(string reviewedScope, Func<bool> cancelled)
		{
			RequireLocalModeExitScope(reviewedScope, cancelled);
			_exitReviewReloadScope = reviewedScope;
			await Reload();
			RequireLocalModeExitScope(reviewedScope, cancelled);
		}

		internal async Promise ExitLocalMode(string reviewedScope, Func<bool> cancelled = null)
		{
			RequireLocalModeExitScope(reviewedScope, cancelled);
			if (AutoSyncState != ContentAutoSyncState.Paused)
				throw new InvalidOperationException("Wait for Local Mode watchers to be ready, or refresh them after a failure.");
			try
			{
				// Keep the preference on while both watchers stop. Recheck only after shutdown,
				// under the transition gate, so pending startup/reload work cannot bypass review.
				await ApplyLocalContentMode(async () =>
				{
					RequireLocalModeExitScope(reviewedScope, cancelled);
					await CheckLocalModeExitSnapshot(reviewedScope, cancelled);
					RequireLocalModeExitScope(reviewedScope, cancelled);
					_contentConfiguration.EnableLocalContentInEditor = false;
				});
			}
			catch
			{
				// Persist the safe intent even if a failed command cannot be stopped yet.
				_contentConfiguration.EnableLocalContentInEditor = true;
				_exitReviewReloadScope = reviewedScope;
				try { await ApplyLocalContentMode(); } catch { /* The watcher state exposes the failure for retry. */ }
				throw;
			}
		}

		private async Promise CheckLocalModeExitSnapshot(string reviewedScope, Func<bool> cancelled)
		{
			var command = _cli.ContentPs(new ContentPsArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption(), noAutoSync = true, watch = false
			});
			BeamContentPsCommandEvent snapshot = null;
			Exception reportedError = null;
			command.OnStreamContentPsCommandEvent(report => snapshot = report.data);
			command.OnError(report => reportedError = new InvalidOperationException(report.data.message));
			var abort = new TaskCompletionSource<Exception>();
			var deadline = EditorApplication.timeSinceStartup + (_localModeExitTimeoutMs > 0 ? _localModeExitTimeoutMs : 30000) / 1000d;
			void CheckAbort()
			{
				try
				{
					if (_watchersDisposed || (cancelled?.Invoke() ?? false) || reviewedScope != GetContentScopeKey() ||
					    !_contentConfiguration.EnableLocalContentInEditor)
						abort.TrySetResult(new OperationCanceledException("The Local Mode exit check was cancelled. Local Mode stays on."));
					else if (EditorApplication.timeSinceStartup >= deadline)
						abort.TrySetResult(new TimeoutException("The Local Mode exit check timed out. Local Mode stays on; refresh to retry."));
				}
				catch (Exception error) { abort.TrySetResult(error); }
			}
			_localModeExitCheck = command.Command;
			_localModeExitCheckStop = null;
			_localModeExitStopFailed = false;
			EditorApplication.update += CheckAbort;
			try
			{
				var completion = _localModeExitCheckCompletion = AwaitCommand(command.Run());
				// Observe the original run even if cancellation wins the race below.
				_ = completion.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
				if (await Task.WhenAny(completion, abort.Task) == abort.Task)
				{
					var reason = await abort.Task;
					await StopLocalModeExitCheck();
					throw reason;
				}
				await completion;
			}
			finally
			{
				EditorApplication.update -= CheckAbort;
				// A failed stop retains ownership so a later refresh/dispose can retry cleanup.
				if (!_localModeExitStopFailed && _localModeExitCheckCompletion?.IsCompleted == true)
					ClearLocalModeExitCheck();
			}
			if (reportedError != null) throw reportedError;
			var manifests = snapshot?.RelevantManifestsAgainstLatest;
			var manifest = manifests?.SingleOrDefault(item =>
				$"{item.OwnerCid}/{item.OwnerPid}/{item.ManifestId}" == reviewedScope);
			if (snapshot?.EventType != CONTENT_PS_EVENT_TYPE_FULL_REBUILD || manifest?.Entries == null)
				throw new InvalidOperationException("The CLI did not return a complete snapshot for this realm and namespace. Refresh and try again.");
			if (manifest.Entries.Any(RequiresLocalModeReview))
				throw new InvalidOperationException("Incoming changes or conflicts still need review. Refresh, then choose Keep Local or Use Realm for each item before leaving Local Mode.");
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
				var stop = _localModeExitCheckStop;
				_ = stop.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
				if (await Task.WhenAny(stop, Task.Delay(_localModeExitStopTimeoutMs > 0 ? _localModeExitStopTimeoutMs : 10000)) != stop)
					throw new TimeoutException("Could not confirm exit-check cleanup. Local Mode stays on. Refresh to retry before restarting watchers.");
				await stop;
				ClearLocalModeExitCheck();
			}
			catch { _localModeExitStopFailed = true; throw; }
		}
	}
}
