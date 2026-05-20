using Beamable.Common.BeamCli;
using Beamable.Common.Content;
using Beamable.Server;
using System.CommandLine;

namespace cli.Content;

public class ContentSyncCommand : AtomicCommand<ContentSyncCommandArgs, ContentSyncResult>, ISkipManifest, IResultSteam<ProgressStreamResultChannel, ContentProgressUpdateData>
{
	private static ConfigurableOptionFlag SYNC_CREATED_OPTION = new("sync-created",
		"Deletes any created content that is not present in the latest manifest. If filters are provided, will only delete the created content that matches the filter");

	private static ConfigurableOptionFlag SYNC_MODIFIED_OPTION = new("sync-modified",
		"This will discard your local changes ONLY on files that are NOT conflicted. If filters are provided, will only do this for content that matches the filter");

	private static ConfigurableOptionFlag SYNC_CONFLICTS_OPTION = new("sync-conflicts",
		"This will discard your local changes ONLY on files that ARE conflicted. If filters are provided, will only do this for content that matches the filter");
	
	private static ConfigurableOptionFlag SYNC_DELETED_OPTION = new("sync-deleted",
		"This will revert all your deleted files. If filters are provided, will only do this for content that matches the filter");

	private static ConfigurableOption TARGET_MANIFEST_UID_OPTION =
		new("target", "If you pass in a Manifest's UID, we'll sync with that as the target. If filters are provided, will only do this for content that matches the filter");

	private static Option<int> DOWNLOAD_MAX_PARALLEL_COUNT_OPTION = new(
		"--download-max-parallel-count",
		() => 64,
		"Maximum number of content files to download in parallel while syncing. Use 0 for unbounded parallelism.");

	private ContentService _contentService;

	public ContentSyncCommand() : base("sync", "Synchronizes the local content matching the filters to the latest content stored in the realm")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToReset = s);
		AddOption(ContentCommand.FILTER_TYPE_OPTION, (args, s) => args.FilterType = s);
		AddOption(ContentCommand.FILTER_OPTION, (args, s) => args.Filter = string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Split(','));
		AddOption(SYNC_CREATED_OPTION, (args, b) => args.SyncCreated = b);
		AddOption(SYNC_MODIFIED_OPTION, (args, b) => args.SyncModified = b);
		AddOption(SYNC_CONFLICTS_OPTION, (args, b) => args.SyncConflicts = b);
		AddOption(SYNC_DELETED_OPTION, (args, b) => args.SyncDeleted = b);
		AddOption(TARGET_MANIFEST_UID_OPTION, (args, b) => args.TargetManifestUid = b);
		AddOption(DOWNLOAD_MAX_PARALLEL_COUNT_OPTION, (args, i) => args.DownloadMaxParallelCount = i);
	}

	public override async Task<ContentSyncResult> GetResult(ContentSyncCommandArgs args)
	{
		_contentService = args.ContentService;

		// Resets the content for all the given manifests
		var resetPromises = new List<Task<ContentSyncReport>>();
		foreach (var manifestId in args.ManifestIdsToReset)
		{
			var task = _contentService.SyncLocalContent(args.Lifecycle, manifestId, args.FilterType, args.Filter, args.SyncCreated,
				args.SyncModified, args.SyncConflicts, args.SyncDeleted, args.TargetManifestUid, this.SendResults<ProgressStreamResultChannel, ContentProgressUpdateData>,
				args.DownloadMaxParallelCount);
				
			resetPromises.Add(task);
		}

		var res = await Task.WhenAll(resetPromises);		
		return new() { Reports = res };
	}
}

public class ContentSyncCommandArgs : ContentCommandArgs
{
	public string[] ManifestIdsToReset;

	public ContentFilterType FilterType;
	public string[] Filter;

	public bool SyncCreated;
	public bool SyncModified;
	public bool SyncConflicts;
	public bool SyncDeleted;
	public string TargetManifestUid;
	public int DownloadMaxParallelCount;
}

[CliContractType]
public class ContentSyncResult
{
	public ContentSyncReport[] Reports;
}
