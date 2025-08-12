using Beamable.Common.BeamCli;
using System.CommandLine;
using System.Text;

namespace cli.Content;

public class ContentRestoreCommand : AtomicCommand<ContentRestoreCommandArgs, ContentRestoreResult>, ISkipManifest, IReportException<ContentRestoreErrorReport>
{
	private ContentService _contentService;
	
	public ContentRestoreCommand() : base("restore", "Restore content data based on a local snapshot, remote manifest ID or manifest file.")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--manifest-id", () => "global", "Defines the name of the manifest on which the snapshot will be restored. The default value is `global`"), (args, s) => args.ManifestId = s);
		AddOption(
			new Option<string>("--name", () => "",
				"Defines the name or path for the snapshot to be restored. If passed a name, it will first get the snapshot from shared folder '.beamable/content-snapshots' than from the local only under '.beamable/temp/content-snapshots'. If a path is passed, it is going to try get the json file from the path"),
			(args, s) => args.SnapshotNameOrPath = s, new[] { "-n" });
		AddOption(new Option<bool>("--delete-after-restore", () => false, "Defines if the snapshot file should be deleted after restoring."), (args, b) => args.DeleteSnapshotAfterRestore = b, new[] { "-d" });
	}

	public override async Task<ContentRestoreResult> GetResult(ContentRestoreCommandArgs args)
	{
		_contentService = args.ContentService;
		
		string snapshotFileName = Path.HasExtension(args.SnapshotNameOrPath) ? args.SnapshotNameOrPath : $"{args.SnapshotNameOrPath}.json";
		if (Path.GetExtension(snapshotFileName) != ".json")
		{
			throw new CliException($"{args.SnapshotNameOrPath} is not a json file.");
		}
		string beamPath = args.ConfigService.ConfigDirectoryPath;
		string fullPath;
		if (Path.IsPathFullyQualified(snapshotFileName))
		{
			if (!File.Exists(snapshotFileName))
			{
				throw new CliException<ContentRestoreErrorReport>($"Snapshot file doesn't exist on path {snapshotFileName}")
				{
					payload = new ContentRestoreErrorReport() { AvailableSnapshots = GetLocalAndSharedSnapshots(_contentService)}
				};
			}
			fullPath = snapshotFileName;
		}
		else
		{
			string sharedFolder = ContentService.GetContentSnapshotDirectoryPath(beamPath, false);
			string tempFolder = ContentService.GetContentSnapshotDirectoryPath(beamPath, true);
			
			// Try to find it on shared or temp folder
			string sharedFolderFile = Path.Combine(sharedFolder, snapshotFileName);
			string tempFolderFile = Path.Combine(tempFolder, snapshotFileName);
			
			if (File.Exists(sharedFolderFile))
			{
				fullPath = sharedFolderFile;
			} else if (File.Exists(tempFolderFile))
			{
				fullPath = tempFolderFile;
			}
			else
			{
				throw new CliException<ContentRestoreErrorReport>(
					$"Snapshot with name '{args.SnapshotNameOrPath}' not found on shared or temp folder, please check if the folder is under '{sharedFolder}' or '{tempFolder}'.")
					{
						payload = new ContentRestoreErrorReport() { AvailableSnapshots = GetLocalAndSharedSnapshots(_contentService)}
					};
			}
		}
		
		var restoredContents = await _contentService.RestoreSnapshot(fullPath, args.DeleteSnapshotAfterRestore, args.ManifestId);
		return new ContentRestoreResult() { RestoredContents = restoredContents };
	}

	private string[] GetLocalAndSharedSnapshots(ContentService content)
	{
		List<string> snapshotsPaths = content.GetContentSnapshots(false).ToList();
		snapshotsPaths.AddRange(content.GetContentSnapshots(true));
		return snapshotsPaths.ToArray();	
	}
}

[Serializable]
public class ContentRestoreErrorReport : ErrorOutput
{
	public string[] AvailableSnapshots;
}

public class ContentRestoreCommandArgs : ContentCommandArgs
{
	public string ManifestId;
	public string SnapshotNameOrPath;
	public bool DeleteSnapshotAfterRestore;
}

[CliContractType, Serializable]
public class ContentRestoreResult
{
	public string[] RestoredContents;
}
