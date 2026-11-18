using System;
using System.Linq;
using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Editor.BeamCli.Commands;

namespace Beamable.Editor.ContentService
{
	public partial class CliContentService
	{
		internal static bool RequiresLocalModeReview(LocalContentManifestEntry entry) =>
			entry.IsInConflict || (ContentChangeKind)entry.ChangeKind is not
				(ContentChangeKind.UpToDate or ContentChangeKind.LocalEdit);

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
				if (_contentWrites.HasWrites(key) || _contentWrites.IsDeleting(key))
					throw new InvalidOperationException("Content edits are still being saved. Wait for them to finish, then review again.");
			}
		}

		internal async Promise RefreshLocalModeExit(string reviewedScope, Func<bool> cancelled)
		{
			RequireLocalModeExitScope(reviewedScope, cancelled);
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
					await CheckLocalModeExitSnapshot(reviewedScope);
					RequireLocalModeExitScope(reviewedScope, cancelled);
					_contentConfiguration.EnableLocalContentInEditor = false;
				});
			}
			catch
			{
				// Persist the safe intent even if a failed command cannot be stopped yet.
				_contentConfiguration.EnableLocalContentInEditor = true;
				try { await ApplyLocalContentMode(); } catch { /* The watcher state exposes the failure for retry. */ }
				throw;
			}
		}

		private async Promise CheckLocalModeExitSnapshot(string reviewedScope)
		{
			var command = _cli.ContentPs(new ContentPsArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption(), noAutoSync = true, watch = false
			});
			BeamContentPsCommandEvent snapshot = null;
			Exception reportedError = null;
			command.OnStreamContentPsCommandEvent(report => snapshot = report.data);
			command.OnError(report => reportedError = new InvalidOperationException(report.data.message));
			await command.Run();
			if (reportedError != null) throw reportedError;
			var manifests = snapshot?.RelevantManifestsAgainstLatest;
			var manifest = manifests?.SingleOrDefault(item =>
				$"{item.OwnerCid}/{item.OwnerPid}/{item.ManifestId}" == reviewedScope);
			if (snapshot?.EventType != CONTENT_PS_EVENT_TYPE_FULL_REBUILD || manifest?.Entries == null)
				throw new InvalidOperationException("The CLI did not return a complete snapshot for this realm and namespace. Refresh and try again.");
			if (manifest.Entries.Any(RequiresLocalModeReview))
				throw new InvalidOperationException("Incoming changes or conflicts still need review. Refresh, then choose Keep Local or Use Realm for each item before leaving Local Mode.");
		}
	}
}
