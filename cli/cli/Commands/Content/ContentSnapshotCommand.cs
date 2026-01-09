using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using System.CommandLine;

namespace cli.Content;

public class ContentSnapshotCommand : AtomicCommand<ContentSnapshotCommandArgs, ContentSnapshotResult>, ISkipManifest
{
	private ContentService _contentService;
	
	public ContentSnapshotCommand() : base("snapshot", "Save a manifest as a contents snapshot")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--manifest-id", () => "global", "Defines the name of the manifest that the snapshot will be created from. The default value is `global`"), (args, s) => args.ManifestIdToSnapshot = s);
		AddOption(new Option<string>("--name", () => "", "Defines the name for the snapshot to be created"), (args, s) => args.SnapshotName = s, new[] { "-n" });
		AddOption(new Option<ContentSnapshotType>("--snapshot-type", () => ContentSnapshotType.Local,
				"Defines where the snapshot will be stored to." +
				$"\n{nameof(ContentSnapshotType.Local)} => Will save the snapshot under `.beamable/temp/content-snapshots/[PID]` folder" +
				$"\n{nameof(ContentSnapshotType.Shared)} => Will save the snapshot under `.beamable/content-snapshots/[PID]` folder"),
			(args, type) => args.ContentSnapshotType = type, new[] { "-t" });
	}

	public override async Task<ContentSnapshotResult> GetResult(ContentSnapshotCommandArgs args)
	{
		_contentService = args.ContentService;
		
		string snapshotPath = await _contentService.SnapshotLocalContent(args.SnapshotName, args.ContentSnapshotType ==  ContentSnapshotType.Local, args.ManifestIdToSnapshot);

		return new()
		{
			SnapshotFullPath = snapshotPath
		};
	}
}

[CliContractType, Serializable]
public class ContentSnapshotCommandArgs : ContentCommandArgs
{
	public string ManifestIdToSnapshot;
	public string SnapshotName;
	public ContentSnapshotType ContentSnapshotType;
}

[CliContractType, Serializable]
public class ContentSnapshotResult
{
	public string SnapshotFullPath;
}
