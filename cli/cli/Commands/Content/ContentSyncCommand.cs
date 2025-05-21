using System.CommandLine;

namespace cli.Content;

public class ContentSyncCommand : AtomicCommand<ContentSyncCommandArgs, ContentSyncResult>, ISkipManifest
{
	private static ConfigurableOptionFlag SYNC_CREATED_OPTION = new("sync-created",
		"Deletes any created content that is not present in the latest manifest. If filters are provided, will only delete the created content that matches the filter");

	private static ConfigurableOptionFlag SYNC_MODIFIED_OPTION = new("sync-modified",
		"This will discard your local changes ONLY on files that are NOT conflicted. If filters are provided, will only do this for content that matches the filter");

	private static ConfigurableOptionFlag SYNC_CONFLICTS_OPTION = new("sync-conflicts",
		"This will discard your local changes ONLY on files that ARE conflicted. If filters are provided, will only do this for content that matches the filter");

	private static ConfigurableOption TARGET_MANIFEST_UID_OPTION =
		new("target", "If you pass in a Manifest's UID, we'll sync with that as the target. If filters are provided, will only do this for content that matches the filter");

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
		AddOption(TARGET_MANIFEST_UID_OPTION, (args, b) => args.TargetManifestUid = b);
	}

	public override async Task<ContentSyncResult> GetResult(ContentSyncCommandArgs args)
	{
		_contentService = args.ContentService;

		// Resets the content for all the given manifests
		var resetPromises = new List<Task>();
		foreach (var manifestId in args.ManifestIdsToReset)
		{
			resetPromises.Add(_contentService.SyncLocalContent(manifestId, args.FilterType, args.Filter, args.SyncCreated, args.SyncModified, args.SyncConflicts, args.TargetManifestUid));
		}

		await Task.WhenAll(resetPromises);

		return new();
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
	public string TargetManifestUid;
}

public class ContentSyncResult
{
}
