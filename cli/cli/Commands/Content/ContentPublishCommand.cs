using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using System.CommandLine;

namespace cli.Content;

public class ContentPublishCommand : AtomicCommand<ContentPublishCommandArgs, ContentPublishResult>, ISkipManifest, IResultSteam<ProgressStreamResultChannel, ContentProgressUpdateData>
{
	private ContentService _contentService;

	public ContentPublishCommand() : base("publish", "Publish content and manifest")
	{
	}

	public override void Configure()
	{
		AddOption(ContentCommand.MANIFESTS_FILTER_OPTION, (args, s) => args.ManifestIdsToPublish = s);
		AddOption(new Option<AutoSnapshotType>("--auto-snapshot-type", () => AutoSnapshotType.None,
			"Defines if after publish the Content System should take snapshots of the content." + 
			$"\n{nameof(AutoSnapshotType.None)} => Will not save any snapshot after publishing" +
		    $"\n{nameof(AutoSnapshotType.LocalOnly)} => Will save the snapshot under `.beamable/temp/content-snapshots/[PID]` folder" +
		    $"\n{nameof(AutoSnapshotType.SharedOnly)} => Will save the snapshot under `.beamable/content-snapshots/[PID]` folder" +
		    $"\n{nameof(AutoSnapshotType.Both)} => Will save two snapshots, under local and shared folders"), 
			(args, t) => args.SnapshotType = t);
		AddOption(new Option<int>("--max-local-snapshots", () => 20, 
			"Defines the max stored local snapshots taken by the auto snapshot generation by this command. When the number hits, the older one will be deletd and replaced by the new snapshot"),
			(args, value) => args.MaxLocalSnapshot = value);
	}

	public override async Task<ContentPublishResult> GetResult(ContentPublishCommandArgs args)
	{
		_contentService = args.ContentService;

		// Prepare the filters --- makes sure all the given manifests exist.
		var publishPromises = new List<Task>();
		foreach (string manifestId in args.ManifestIdsToPublish)
		{
			publishPromises.Add(_contentService.PublishContent(args.SnapshotType, args.MaxLocalSnapshot, manifestId, this.SendResults<ProgressStreamResultChannel, ContentProgressUpdateData>));
		}
		await Task.WhenAll(publishPromises);

		return new();
	}
	
}

[Serializable]
public class ContentPublishCommandArgs : ContentCommandArgs
{
	public string[] ManifestIdsToPublish;
	public AutoSnapshotType SnapshotType;
	public int MaxLocalSnapshot;
}

public class ContentPublishResult
{
}

