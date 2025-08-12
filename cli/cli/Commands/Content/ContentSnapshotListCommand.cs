using Beamable.Common.BeamCli;

namespace cli.Content;

public class ContentSnapshotListCommand : AtomicCommand<ContentSnapshotListCommandArgs, ContentSnapshotListResult>, ISkipManifest
{
	private ContentService _contentService;

	public ContentSnapshotListCommand() : base("snapshot-list", "Find and list all shared (.beamable/content-snapshots) and local (.beamable/temp/content-snapshots) snapshots")
	{
	}

	public override void Configure()
	{
	}

	public override Task<ContentSnapshotListResult> GetResult(ContentSnapshotListCommandArgs args)
	{
		_contentService = args.ContentService;
		return Task.FromResult(new ContentSnapshotListResult()
		{
			LocalSnapshots = _contentService.GetContentSnapshots(true), SharedSnapshots = _contentService.GetContentSnapshots(false)
		});
	}
	
}

public class ContentSnapshotListCommandArgs : ContentCommandArgs
{
}

[CliContractType, Serializable]
public class ContentSnapshotListResult
{
	public string[] SharedSnapshots;
	public string[] LocalSnapshots;
}
