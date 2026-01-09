using Beamable.Common.BeamCli;
using System.CommandLine;
using System.Text;

namespace cli.Content;

public class ContentRestoreCommand : AtomicCommand<ContentRestoreCommandArgs, ContentRestoreResult>, ISkipManifest, IReportException<ContentRestoreErrorReport>
{
	private ContentService _contentService;
	
	public ContentRestoreCommand() : base("restore", "Restore content data based on a local snapshot, remote manifest ID or manifest file")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--manifest-id", () => "global", "Defines the name of the manifest on which the snapshot will be restored. The default value is `global`"), (args, s) => args.ManifestId = s);
		AddOption(new Option<string>("--name", () => "",
				"Defines the name or path for the snapshot to be restored. If passed a name, it will first get the snapshot from shared folder '.beamable/content-snapshots/[PID]' than from the local only under '.beamable/temp/content-snapshots/[PID]'. If a path is passed, it is going to try get the json file from the path"),
			(args, s) => args.SnapshotNameOrPath = s, new[] { "-n" });
		AddOption(new Option<string>("--pid", () => string.Empty, "An optional field to set the PID from where you would like to get the snapshot to be restored. The default will be the current PID the user are in"), (args, s) => args.Pid = s);
		AddOption(new Option<bool>("--delete-after-restore", () => false, "Defines if the snapshot file should be deleted after restoring"), (args, b) => args.DeleteSnapshotAfterRestore = b, new[] { "-d" });
		AddOption(
			new Option<bool>("--additive-restore", () => false,
				"Defines if the restore will additionally adds the contents without deleting current local contents"),
			(args, b) => args.AdditiveRestore = b);
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
		string pid = args.Requester.Pid;
		if (!string.IsNullOrEmpty(args.Pid))
		{
			pid = args.Pid;
		}
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
			string sharedFolder = ContentService.GetContentSnapshotDirectoryPath(beamPath, pid, false);
			string tempFolder = ContentService.GetContentSnapshotDirectoryPath(beamPath, pid, true);
			
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
		
		var restoredContents = await _contentService.RestoreSnapshot(fullPath, args.DeleteSnapshotAfterRestore, args.AdditiveRestore, args.ManifestId);
		return new ContentRestoreResult() { RestoredContents = restoredContents };
	}

	private string[] GetLocalAndSharedSnapshots(ContentService content)
	{
		List<string> snapshotsPaths = content.GetContentSnapshots(false).ToList();
		snapshotsPaths.AddRange(content.GetContentSnapshots(true));
		return snapshotsPaths.ToArray();	
	}
}

[CliContractType, Serializable]
public class ContentRestoreErrorReport : ErrorOutput
{
	public string[] AvailableSnapshots;
}

[CliContractType, Serializable]
public class ContentRestoreCommandArgs : ContentCommandArgs
{
	public string ManifestId;
	public string Pid;
	public string SnapshotNameOrPath;
	public bool DeleteSnapshotAfterRestore;
	public bool AdditiveRestore;
}

[CliContractType, Serializable]
public class ContentRestoreResult
{
	public string[] RestoredContents;
}
