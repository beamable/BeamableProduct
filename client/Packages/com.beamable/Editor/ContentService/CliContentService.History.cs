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
		/// Gets the last watcher failure so the History window can display a recoverable error state.
		/// </summary>
		public ContentHistoryOperationException ContentHistoryWatcherError { get; private set; }
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
			ContentHistoryWatcherError = null;
			ContentHistoryVersion++;
			_contentHistoryWatcher = _cli.ContentHistory(new ContentHistoryArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption(),
				watch = true,
				requireProcessId = Process.GetCurrentProcess().Id
			});
			_contentHistoryWatcher.Command.On(report => OnContentHistoryEvent(report.json));
			_ = RunContentHistoryWatcher(_contentHistoryWatcher);
		}

		/// <summary>
		/// Stops a failed watcher, if any, and starts a new watcher with a fresh editor-side cache.
		/// </summary>
		public void RestartContentHistory()
		{
			StopContentHistory();
			StartContentHistory();
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
			var wrapper = _cli.ContentHistorySyncContent(new ContentHistoryArgs
			{
				manifestIds = GetSelectedManifestIdsCliOption()
			}, new ContentHistorySyncContentArgs
			{
				manifestUid = manifestUid
			});
			BeamContentHistoryChangelistEntry[] changes = null;
			try
			{
				wrapper.Command.On(report =>
				{
					if (ContentHistoryStreamParser.TryParseChanges(report.json, out var entries))
					{
						changes = entries;
					}
				});

				await wrapper.Run();
				if (changes == null)
				{
					throw new InvalidOperationException("The CLI completed without delivering a usable content-history changelist stream response.");
				}

				return changes;
			}
			catch (Exception exception)
			{
				throw new ContentHistoryOperationException(
					"Load publish changes",
					"Unable to load changes for this publish. Check your connection and try again.",
					manifestUid,
					exception);
			}
		}

		/// <summary>
		/// Returns the locally cached historical JSON for one content ID, synchronizing the selected changelist first.
		/// </summary>
		/// <param name="manifestUid">The historical manifest UID that owns the content version.</param>
		/// <param name="contentId">The full content ID to preview.</param>
		public async Task<string> GetContentHistoryJson(string manifestUid, string contentId)
		{
			try
			{
				var entries = await GetContentHistoryChanges(manifestUid);
				foreach (var entry in entries)
				{
					if (entry.FullId != contentId)
					{
						continue;
					}

					if (string.IsNullOrEmpty(entry.JsonFilePath) || !File.Exists(entry.JsonFilePath))
					{
						throw new FileNotFoundException("The CLI did not provide a readable cached historical content file.", entry.JsonFilePath);
					}

					return await File.ReadAllTextAsync(entry.JsonFilePath);
				}

				throw new FileNotFoundException($"The selected content entry '{contentId}' was not included in the historical changelist.");
			}
			catch (Exception exception)
			{
				throw new ContentHistoryOperationException(
					"Preview historical content",
					"Unable to load this historical content file. Check your connection and try again.",
					manifestUid,
					exception);
			}
		}

		/// <summary>
		/// Restores every changed content file from a historical publish into the local workspace, then reloads Content Manager.
		/// This does not publish the restored files to the realm.
		/// </summary>
		/// <param name="manifestUid">The historical manifest UID to restore.</param>
		public async Task RestoreContentHistory(string manifestUid)
		{
			try
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
			catch (ContentHistoryOperationException)
			{
				throw;
			}
			catch (Exception exception)
			{
				throw new ContentHistoryOperationException(
					"Restore changed files",
					"Unable to restore changed files. Check your connection and try again.",
					manifestUid,
					exception);
			}
		}

		private async Task RunContentHistoryWatcher(ContentHistoryWrapper watcher)
		{
			try
			{
				await watcher.Run();
				if (ReferenceEquals(_contentHistoryWatcher, watcher))
				{
					SetContentHistoryWatcherError(new ContentHistoryOperationException(
						"Watch publish history",
						HasReceivedInitialContentHistory
							? "History updates stopped. Showing the history already loaded. Retry to resume updates."
							: "Unable to load published history. Check your connection and try again.",
						null,
						new InvalidOperationException("The CLI history watcher ended unexpectedly.")));
				}
			}
			catch (Exception exception)
			{
				if (ReferenceEquals(_contentHistoryWatcher, watcher))
				{
					SetContentHistoryWatcherError(new ContentHistoryOperationException(
						"Watch publish history",
						HasReceivedInitialContentHistory
							? "History updates stopped. Showing the history already loaded. Retry to resume updates."
							: "Unable to load published history. Check your connection and try again.",
						null,
						exception));
				}
			}
		}

		private void SetContentHistoryWatcherError(ContentHistoryOperationException error)
		{
			_contentHistoryWatcher = null;
			ContentHistoryWatcherError = error;
			ContentHistoryVersion++;
			UnityEngine.Debug.LogError(error.Diagnostic);
		}

		private void OnContentHistoryEvent(string streamJson)
		{
			try
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
			catch (Exception exception)
			{
				if (_contentHistoryWatcher == null)
				{
					return;
				}

				_contentHistoryWatcher.Cancel();
				SetContentHistoryWatcherError(new ContentHistoryOperationException(
					"Watch publish history",
					"Unable to load published history. Check your connection and try again.",
					null,
					exception));
			}
		}
	}

	public class ContentHistoryOperationException : Exception
	{
		public string Operation { get; }
		public string UserMessage { get; }
		public string ManifestUid { get; }
		public DateTimeOffset TimestampUtc { get; }

		public string Diagnostic =>
			$"Content History operation failed\n" +
			$"Timestamp (UTC): {TimestampUtc:O}\n" +
			$"Operation: {Operation}\n" +
			$"Manifest UID: {ManifestUid ?? "(not applicable)"}\n" +
			$"Exception:\n{InnerException}";

		public ContentHistoryOperationException(string operation, string userMessage, string manifestUid, Exception innerException)
			: base(userMessage, innerException)
		{
			Operation = operation;
			UserMessage = userMessage;
			ManifestUid = manifestUid;
			TimestampUtc = DateTimeOffset.UtcNow;
		}

		public static ContentHistoryOperationException FromException(string operation, string userMessage, string manifestUid,
			Exception exception)
		{
			return exception as ContentHistoryOperationException ??
			       new ContentHistoryOperationException(operation, userMessage, manifestUid, exception);
		}
	}
}
