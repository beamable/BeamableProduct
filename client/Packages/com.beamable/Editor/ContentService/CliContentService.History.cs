using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Beamable.Editor.ContentService
{
	public partial class CliContentService
	{
		private ContentHistoryWrapper _contentHistoryWatcher;
		private readonly ContentHistoryEntryCache _contentHistoryEntryCache = new();

		/// <summary>
		/// Gets the publish metadata received by the active history watcher, ordered newest first.
		/// This is an in-memory view over the CLI's local history cache.
		/// </summary>
		public IReadOnlyList<BeamContentHistoryEntry> ContentHistoryEntries => _contentHistoryEntryCache.Entries;
		/// <summary>
		/// Changes whenever the history cache is cleared or updated so editor windows can repaint.
		/// </summary>
		public int ContentHistoryVersion { get; private set; }
		/// <summary>
		/// True while the long-running <c>content history --watch</c> command is active.
		/// </summary>
		public bool IsContentHistoryWatching => _contentHistoryWatcher != null;
		/// <summary>
		/// True after the watcher has delivered its first valid history event, including an empty history.
		/// </summary>
		public bool HasReceivedInitialContentHistory { get; private set; }

		/// <summary>
		/// Starts a process-bound CLI history watcher for the selected manifests and clears the previous in-memory entries.
		/// The CLI maintains its own on-disk cache; this method only resets the editor-side view of that cache.
		/// </summary>
		public void StartContentHistory()
		{
			if (_contentHistoryWatcher != null)
			{
				return;
			}

			_contentHistoryEntryCache.Clear();
			HasReceivedInitialContentHistory = false;
			ContentHistoryVersion++;
			_contentHistoryWatcher = _cli.ContentHistory(new ContentHistoryArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				watch = true,
				requireProcessId = Process.GetCurrentProcess().Id
			});
			_contentHistoryWatcher.Command.On(report => OnContentHistoryEvent(report.json));
			_ = _contentHistoryWatcher.Run();
		}

		/// <summary>
		/// Cancels the active history watcher. It does not clear the CLI's on-disk history cache.
		/// </summary>
		public void StopContentHistory()
		{
			if (_contentHistoryWatcher == null)
			{
				return;
			}

			_contentHistoryWatcher.Cancel();
			_contentHistoryWatcher = null;
		}

		/// <summary>
		/// Synchronizes and returns the changelist for one historical publish.
		/// The result is delivered by the CLI stream and parsed through the history-specific wire adapter.
		/// </summary>
		/// <param name="manifestUid">The historical manifest UID to inspect.</param>
		public async Task<BeamContentHistoryChangelistEntry[]> GetContentHistoryChanges(string manifestUid)
		{
			var changesWaiter = new TaskCompletionSource<BeamContentHistoryChangelistEntry[]>();
			var wrapper = _cli.ContentHistorySyncContent(new ContentHistoryArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption()
			}, new ContentHistorySyncContentArgs
			{
				manifestUid = manifestUid
			});
			wrapper.Command.On(report =>
			{
				if (ContentHistoryStreamParser.TryParseChanges(report.json, out var entries))
				{
					changesWaiter.TrySetResult(entries);
				}
			});

			await wrapper.Run();
			return await changesWaiter.Task;
		}

		/// <summary>
		/// Returns the locally cached historical JSON for one content ID, synchronizing the selected changelist first.
		/// </summary>
		/// <param name="manifestUid">The historical manifest UID that owns the content version.</param>
		/// <param name="contentId">The full content ID to preview.</param>
		public async Task<string> GetContentHistoryJson(string manifestUid, string contentId)
		{
			var entries = await GetContentHistoryChanges(manifestUid);
			foreach (var entry in entries)
			{
				if (entry.FullId == contentId && !string.IsNullOrEmpty(entry.JsonFilePath) && File.Exists(entry.JsonFilePath))
				{
					return await File.ReadAllTextAsync(entry.JsonFilePath);
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Restores every changed content file from a historical publish into the local workspace, then reloads Content Manager.
		/// This does not publish the restored files to the realm.
		/// </summary>
		/// <param name="manifestUid">The historical manifest UID to restore.</param>
		public async Task RestoreContentHistory(string manifestUid)
		{
			var wrapper = _cli.ContentHistoryRestoreContent(new ContentHistoryArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption()
			}, new ContentHistoryRestoreContentArgs
			{
				manifestUid = manifestUid
			});

			await wrapper.Run();
			await Reload();
		}

		private void OnContentHistoryEvent(string streamJson)
		{
			var historyUpdate = ContentHistoryStreamParser.Parse(streamJson);
			if (historyUpdate == null)
			{
				return;
			}

			_contentHistoryEntryCache.Apply(historyUpdate.Entries, historyUpdate.EntriesToRemove);
			HasReceivedInitialContentHistory = true;
			ContentHistoryVersion++;
		}
	}
}
