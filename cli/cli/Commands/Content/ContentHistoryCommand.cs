using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Server;
using cli.Dotnet;
using Newtonsoft.Json;
using System.CommandLine;
using System.Text.Json.Serialization;

namespace cli.Content;

[Serializable]
public class ContentHistoryCommandArgs : CommandArgs
{
	public bool Watch;
	public int RequireProcessId;
	public string ManifestId;
	public long? FromDate;
	public long? ToDate;
	public List<string> ManifestUids;
}

[Serializable]
public class ContentHistorySyncChangelistCommandArgs : CommandArgs
{
	public string ManifestId;
	public string ManifestUid;
}

[Serializable]
public class ContentHistorySyncChangelistCommandOutput
{
	public ContentHistoryChangelist Changelist;
}

[Serializable]
public class ContentHistorySyncContentCommandArgs : CommandArgs
{
	public string ManifestId;
	public string ManifestUid;
	public List<string> ContentIds;
}

[Serializable]
public class ContentHistorySyncContentCommandOutput
{
	public ContentHistoryChangelistEntry[] ContentEntries;
}

[CliContractType, Serializable]
public class ContentHistoryCommandEvent
{
	/// <summary>
	/// Event type for when content history entries (manifest metadata) are loaded or changed.
	/// The engine integration should update its in-memory state using <see cref="EntriesPage"/> and <see cref="EntriesToRemove"/>.
	/// Relevant fields: <see cref="EntriesPage"/>, <see cref="EntriesToRemove"/>
	/// </summary>
	public const int EVT_TYPE_EntriesLoaded = 0;

	/// <summary>
	/// Event type for when changelist data (manifest diffs) are loaded or changed.
	/// The engine integration should update its in-memory state using <see cref="ChangelistsPage"/> and <see cref="ChangelistsToRemove"/>.
	/// Relevant fields: <see cref="ChangelistsPage"/>, <see cref="ChangelistsToRemove"/>
	/// </summary>
	public const int EVT_TYPE_ChangelistsLoaded = 1;

	/// <summary>
	/// One of <see cref="EVT_TYPE_EntriesLoaded"/> or <see cref="EVT_TYPE_ChangelistsLoaded"/>.
	/// The semantics of each field are defined based on the event and documented on these comments.
	/// </summary>
	public int EventType;

	/// <summary>
	/// Contains the history entries (manifest UIDs and their metadata).
	/// Only relevant when EventType is EVT_TYPE_EntriesLoaded.
	/// </summary>
	public ContentHistoryEntriesPage EntriesPage;

	/// <summary>
	/// Contains the changelist data (diff information between manifests).
	/// Only relevant when EventType is EVT_TYPE_ChangelistsLoaded.
	/// </summary>
	public ContentHistoryChangelistPage ChangelistsPage;

	/// <summary>
	/// List of entry UIDs to remove from the in-memory state.
	/// Only relevant when EventType is EVT_TYPE_EntriesLoaded.
	/// </summary>
	public List<string> EntriesToRemove = new();

	/// <summary>
	/// List of changelist UIDs to remove from the in-memory state.
	/// Only relevant when EventType is EVT_TYPE_ChangelistsLoaded.
	/// </summary>
	public List<string> ChangelistsToRemove = new();
}

public class ContentHistoryCommand : AppCommand<ContentHistoryCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, ContentHistoryCommandEvent>
	, ISkipManifest
{
	public ContentHistoryCommand() : base("history", "Fetches and caches locally the entire list of content publishes to this realm. " +
	                                                 "Can be run in watch mode for continuous updates regarding new downloaded content changelists and new content entries")
	{
	}

	public override bool IsForInternalUse => true;

	public override void Configure()
	{
		ProjectCommand.AddWatchOption(this, (args, i) => args.Watch = i);
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestId = s[0]);
		AddOption(new RequireProcessIdOption(), (args, i) => args.RequireProcessId = i);
		AddOption(new Option<long?>("--from-date", "Filter entries from this Unix timestamp (milliseconds)"), (args, l) => args.FromDate = l);
		AddOption(new Option<long?>("--to-date", "Filter entries to this Unix timestamp (milliseconds)"), (args, l) => args.ToDate = l);
		AddOption(new Option<List<string>>("--manifest-uids", "Filter by specific manifest UIDs"), (args, s) => args.ManifestUids = s);
	}

	public override async Task Handle(ContentHistoryCommandArgs args)
	{
		RequireProcessIdOption.ConfigureRequiredProcessIdWatcher(args.RequireProcessId);

		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var manifestId = args.ManifestId ?? "global";

		var contentService = args.DependencyProvider.GetService<ContentService>();

		// Non-watch mode: sync entries and then emit filtered results
		if (!args.Watch)
		{
			var report = await contentService.SyncContentHistoryEntries(pid, manifestId);

			// Get filtered entries based on provided parameters
			var locallyCachedEntries = await contentService.GetAllContentHistoryLocalEntries(
				pid,
				manifestId,
				startDate: args.FromDate,
				endDate: args.ToDate,
				manifestUids: args.ManifestUids);

			// Emit the filtered entries as a single event
			var evt = new ContentHistoryCommandEvent
			{
				EventType = ContentHistoryCommandEvent.EVT_TYPE_EntriesLoaded,
				EntriesPage = locallyCachedEntries ?? new ContentHistoryEntriesPage { Entries = new List<ContentHistoryEntry>() },
				ChangelistsPage = new ContentHistoryChangelistPage { Changelists = Array.Empty<ContentHistoryChangelist>() },
				EntriesToRemove = new(),
				ChangelistsToRemove = new()
			};
			this.SendResults(evt);

			// TODO: use AnsiConsole to render a table with the entries information 
		}
		else
		{
			// Get all locally cached entries before starting watch mode
			var locallyCachedEntries = await contentService.GetAllContentHistoryLocalEntries(pid, manifestId);

			// Emit the initial event with whatever we have locally before sync'ing
			var initialEvt = new ContentHistoryCommandEvent
			{
				EventType = ContentHistoryCommandEvent.EVT_TYPE_EntriesLoaded,
				EntriesPage = locallyCachedEntries ?? new ContentHistoryEntriesPage { Entries = new List<ContentHistoryEntry>() },
				ChangelistsPage = new ContentHistoryChangelistPage { Changelists = Array.Empty<ContentHistoryChangelist>() },
				EntriesToRemove = new(),
				ChangelistsToRemove = new()
			};
			this.SendResults(initialEvt);

			// This ensures that only one of the event streams will resolve at a time.
			var watchSemaphore = new SemaphoreSlim(1, 1);

			// Kick off task to listen for new entries
			var newEntriesTask = Task.Run(async () =>
			{
				try
				{
					await foreach (var entryFileChanges in contentService.ListenToContentHistoryEntries(pid, manifestId, args.Lifecycle.Source.Token))
					{
						try
						{
							await watchSemaphore.WaitAsync(args.Lifecycle.Source.Token);
						}
						catch (OperationCanceledException)
						{
							Log.Information("content history command was cancelled.");
						}

						try
						{
							var evt = new ContentHistoryCommandEvent
							{
								EventType = ContentHistoryCommandEvent.EVT_TYPE_EntriesLoaded,
								EntriesPage = new ContentHistoryEntriesPage { Entries = new List<ContentHistoryEntry>() },
								ChangelistsPage = new ContentHistoryChangelistPage { Changelists = Array.Empty<ContentHistoryChangelist>() },
								EntriesToRemove = new(),
								ChangelistsToRemove = new()
							};

							// Get all the uids for all the modified files
							var allModifiedFilesUids = entryFileChanges.AllFileChanges.Select(f => f.ManifestUid).Where(uid => !string.IsNullOrEmpty(uid));

							// Get all the entries that were modified.
							var allModifiedEntries = await contentService.GetAllContentHistoryLocalEntries(pid, manifestId, manifestUids: allModifiedFilesUids);
							evt.EntriesPage = allModifiedEntries ?? new ContentHistoryEntriesPage { Entries = new List<ContentHistoryEntry>() };

							// Handle deletions and renames.
							foreach (var ef in entryFileChanges.AllFileChanges.Where(ef => ef.WasDeleted() || ef.WasRenamed()))
							{
								evt.EntriesToRemove.Add(ef.OldManifestUid);
							}

							this.SendResults(evt);
						}
						finally
						{
							watchSemaphore.Release();
						}
					}
				}
				catch (Exception e)
				{
					Log.Error(e, "content history command failed unexpectedly.");
					throw;
				}
			});

			var newChangelistTask = Task.Run(async () =>
			{
				try
				{
					await foreach (var changelistFileChanges in contentService.ListenToContentHistoryChangelists(pid, manifestId, args.Lifecycle.Source.Token))
					{
						try
						{
							await watchSemaphore.WaitAsync(args.Lifecycle.Source.Token);
						}
						catch (OperationCanceledException)
						{
							Log.Information("content history command was cancelled.");
						}

						try
						{
							var evt = new ContentHistoryCommandEvent
							{
								EventType = ContentHistoryCommandEvent.EVT_TYPE_ChangelistsLoaded,
								EntriesPage = new ContentHistoryEntriesPage { Entries = new List<ContentHistoryEntry>() },
								ChangelistsPage = new ContentHistoryChangelistPage { Changelists = Array.Empty<ContentHistoryChangelist>() },
								EntriesToRemove = new(),
								ChangelistsToRemove = new()
							};

							// Get all the UIDs for all the modified/created/read changelist files
							var allModifiedChangelistUids = changelistFileChanges.AllFileChanges
								.Where(cf => cf.WasCreated() || cf.WasChanged() || cf.WasRead())
								.Select(cf => cf.ManifestUid)
								.Where(uid => !string.IsNullOrEmpty(uid))
								.Distinct()
								.ToList();

							// Read the modified changelists from disk (filtered by UIDs to avoid loading unnecessary files)
							if (allModifiedChangelistUids.Count > 0)
							{
								var filteredChangelists = await contentService.GetAllContentHistoryLocalChangelists(pid, manifestId, allModifiedChangelistUids);
								evt.ChangelistsPage = filteredChangelists;
							}

							// Handle deletions and renames - notify which changelists should be purged
							foreach (var cf in changelistFileChanges.AllFileChanges.Where(cf => cf.WasDeleted() || cf.WasRenamed()))
							{
								evt.ChangelistsToRemove.Add(cf.OldManifestUid);
							}

							// Only emit if there's actual data to send
							if (evt.ChangelistsPage.Changelists.Length > 0 || evt.ChangelistsToRemove.Count > 0)
							{
								this.SendResults(evt);
							}
						}
						finally
						{
							watchSemaphore.Release();
						}
					}
				}
				catch (Exception e)
				{
					Log.Error(e, "content history command failed unexpectedly.");
					throw;
				}
			});

			// Kick off a task that will greedily sync all the history entries until right now.
			// Once that's done, kick off a task that will listen for remote content publishes and will resync the content history
			var report = await contentService.SyncContentHistoryEntries(pid, manifestId);
			var remotePublishTask = Task.Run(async () =>
			{
				try
				{
					await foreach (var _ in contentService.ListenToRemoteContentPublishes(args, manifestId, args.Lifecycle.Source.Token))
					{
						await watchSemaphore.WaitAsync();
						try
						{
							// We re-sync only from the date of the last known local content.
							report = await contentService.SyncContentHistoryEntries(pid, manifestId);
						}
						finally
						{
							watchSemaphore.Release();
						}
					}
				}
				catch (Exception e)
				{
					Log.Error(e, "content history command failed unexpectedly.");
					throw;
				}
			});

			await newEntriesTask;
			await newChangelistTask;
			await remotePublishTask;
		}
	}
}

public class ContentHistorySyncChangelistCommand : AtomicCommand<ContentHistorySyncChangelistCommandArgs, ContentHistorySyncChangelistCommandOutput>
	, ISkipManifest
{
	public ContentHistorySyncChangelistCommand() : base("sync-changelist", "Syncs a changelist for a given manifest UID. If already cached locally, touches the file to trigger a watching history command")
	{
	}

	public override bool IsForInternalUse => true;

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestId = s[0]);
		AddOption(new Option<string>("--manifest-uid", "The manifest UID for the changelist to sync"), (args, s) => args.ManifestUid = s);
	}

	public override async Task<ContentHistorySyncChangelistCommandOutput> GetResult(ContentHistorySyncChangelistCommandArgs args)
	{
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var manifestId = args.ManifestId ?? "global";
		var manifestUid = args.ManifestUid;

		if (string.IsNullOrEmpty(manifestUid))
		{
			throw new CliException("ManifestUid is required");
		}

		var contentService = args.DependencyProvider.GetService<ContentService>();

		// Ensure the changelist folder exists
		var changelistsPath = contentService.EnsureContentHistoryChangelistsFolderExists(out _, pid, manifestId);
		var changelistFilePath = Path.Combine(changelistsPath, $"{manifestUid}.json");

		// Check if the file already exists
		if (File.Exists(changelistFilePath))
		{
			// Touch the file to update its last access time
			var now = DateTime.Now;
			File.SetLastAccessTime(changelistFilePath, now);
			Log.Information($"Touched existing changelist file for manifest UID {manifestUid}");
		}
		else
		{
			// Sync the changelist from the server
			await contentService.SyncContentHistoryChangelists(pid, manifestId, new[] { manifestUid }, args.Lifecycle.Source.Token);
			Log.Information($"Synced changelist for manifest UID {manifestUid}");
		}

		// Read and return the changelist contents
		var changelistsPage = await contentService.GetAllContentHistoryLocalChangelists(pid, manifestId);
		var changelist = changelistsPage.Changelists.FirstOrDefault(c => c.ManifestUid == manifestUid);

		return new ContentHistorySyncChangelistCommandOutput { Changelist = changelist };
	}
}

public class ContentHistorySyncContentCommand : AtomicCommand<ContentHistorySyncContentCommandArgs, ContentHistorySyncContentCommandOutput>
	, ISkipManifest
{
	public ContentHistorySyncContentCommand() : base("sync-content",
		"Syncs content files for a given manifest UID. If content IDs are not provided, syncs all content in the manifest")
	{
	}

	public override bool IsForInternalUse => true;

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestId = s[0]);
		AddOption(new Option<string>("--manifest-uid", "The manifest UID for the content to sync"), (args, s) => args.ManifestUid = s);
		AddOption(new Option<List<string>>("--content-ids", "The content IDs to sync. If not provided, syncs all content in the manifest") { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore }, (args, s) => args.ContentIds = s);
	}

	public override async Task<ContentHistorySyncContentCommandOutput> GetResult(ContentHistorySyncContentCommandArgs args)
	{
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var manifestId = args.ManifestId ?? "global";
		var manifestUid = args.ManifestUid;
		var contentIds = args.ContentIds;

		if (string.IsNullOrEmpty(manifestUid))
		{
			throw new CliException("ManifestUid is required");
		}

		var contentService = args.DependencyProvider.GetService<ContentService>();

		// Ensure the changelist is synced locally first
		await contentService.SyncContentHistoryChangelists(pid, manifestId, new[] { manifestUid }, args.Lifecycle.Source.Token);

		// Get the changelist to determine which content IDs to sync
		var changelistPage = await contentService.GetAllContentHistoryLocalChangelists(pid, manifestId, new[] { manifestUid });
		if (changelistPage.Changelists == null || changelistPage.Changelists.Length == 0)
		{
			throw new CliException($"Changelist for manifest UID {manifestUid} not found");
		}

		var changelist = changelistPage.Changelists[0];

		// If no content IDs were provided, collect all content IDs from the changelist
		if (contentIds == null || contentIds.Count == 0)
		{
			contentIds = new List<string>();
			contentIds.AddRange(changelist.Created.Keys);
			contentIds.AddRange(changelist.Modified.Keys);
			contentIds.AddRange(changelist.Removed.Keys);
		}

		// Ensure the content folder exists
		var contentPath = contentService.EnsureContentHistoryContentFolderExists(out _, pid, manifestId);
		var manifestUuidFolder = Path.Combine(contentPath, manifestUid);

		// Separate content IDs into those that exist and those that need to be synced
		var contentIdsToSync = new List<string>();
		foreach (var contentId in contentIds)
		{
			var contentFilePath = Path.Combine(manifestUuidFolder, $"{contentId}.json");
			if (!File.Exists(contentFilePath))
			{
				contentIdsToSync.Add(contentId);
			}
		}

		// Sync content files that don't exist locally
		if (contentIdsToSync.Count > 0)
		{
			await contentService.SyncContentHistoryContent(pid, manifestId, manifestUid, contentIdsToSync, args.Lifecycle.Source.Token);
			Log.Information($"Synced {contentIdsToSync.Count} content file(s) for manifest UID {manifestUid}");
		}

		// Get all locally cached content files to determine which entries to return
		var localContentPage = await contentService.GetAllContentHistoryLocalContent(pid, manifestId, manifestUid);
		var locallyCachedContentIds = new HashSet<string>(localContentPage.ContentEntries.Select(e => e.FullId));

		// Build the result from changelist entries that have locally cached content
		var resultEntries = new List<ContentHistoryChangelistEntry>();

		// Add entries from Created that are locally cached
		foreach (var kvp in changelist.Created)
		{
			if (locallyCachedContentIds.Contains(kvp.Key))
			{
				resultEntries.Add(kvp.Value);
			}
		}

		// Add entries from Modified that are locally cached
		foreach (var kvp in changelist.Modified)
		{
			if (locallyCachedContentIds.Contains(kvp.Key))
			{
				resultEntries.Add(kvp.Value);
			}
		}

		// Add entries from Removed that are locally cached
		foreach (var kvp in changelist.Removed)
		{
			if (locallyCachedContentIds.Contains(kvp.Key))
			{
				resultEntries.Add(kvp.Value);
			}
		}

		return new ContentHistorySyncContentCommandOutput { ContentEntries = resultEntries.ToArray() };
	}
}
