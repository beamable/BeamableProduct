using Beamable.Server;
using cli.Services.Unity;
using System.CommandLine;

namespace cli.UnityCommands;

public class DownloadAllNugetDepsToUnityCommandArgs : CommandArgs
{
	public string unityProjectPath;
	// public string nugetVersion;
}

public class DownloadAllNugetDepsToUnityCommandOutput
{
	
}
public class DownloadAllNugetDepsToUnityCommand : AtomicCommand<DownloadAllNugetDepsToUnityCommandArgs, DownloadAllNugetDepsToUnityCommandOutput>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public DownloadAllNugetDepsToUnityCommand() : base("download-all-nuget-packages", "Download all known beamable nuget deps for the Beamable SDK")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("unityProjectPath", "the path to the Unity project"), (args, i) => args.unityProjectPath = i);

	}	

	public override async Task<DownloadAllNugetDepsToUnityCommandOutput> GetResult(DownloadAllNugetDepsToUnityCommandArgs args)
	{
		await DownloadAllPackages(args);
		return new DownloadAllNugetDepsToUnityCommandOutput();
	}

	public static async Task DownloadAllPackages(DownloadAllNugetDepsToUnityCommandArgs args)
	{
		var info = UnityProjectUtil.GetUnityInfo(args.unityProjectPath, "com.beamable");
		var infoServer = UnityProjectUtil.GetUnityInfo(args.unityProjectPath, "com.beamable.server");

		if (info.beamableNugetVersion.StartsWith("0.0.123"))
		{
			throw new CliException("Cannot download nuget packages for developer 0.0.123 version.");
		}

		var generatedExtensions = new string[] { ".cs", ".cs.meta" };
		var commonFolder = Path.Combine(info.packageFolder, "Common");

		// capture the meta files before anything is deleted, so that folder guids and file guids that
		// already exist survive the delete-and-replace cycle below.
		var commonSnapshot = UnityProjectUtil.CaptureMetaSnapshot(commonFolder);

		UnityProjectUtil.DeleteGeneratedFiles(commonFolder, generatedExtensions);

		// nothing repopulates these two, so they only ever need the file cleanup and a prune.
		var serverFolders = new[]
		{
			Path.Combine(infoServer.packageFolder, "SharedRuntime"),
			Path.Combine(infoServer.packageFolder, "Runtime/Common")
		};
		foreach (var serverFolder in serverFolders)
		{
			UnityProjectUtil.DeleteGeneratedFiles(serverFolder, generatedExtensions);
		}

		await UnityProjectUtil.DownloadPackage("Beamable.Common", info.beamableNugetVersion,
			"content/sourceCode/", commonFolder, commonSnapshot);

		// only prune once the replacement source is on disk. A folder that the new source still uses has
		// been repopulated by now, so it is no longer empty and keeps its meta file. A folder that the new
		// source dropped is still empty, so it is removed along with its orphaned meta file.
		// If the download throws, neither of the steps below runs, so the folder meta files are left
		// untouched and a retry is safe.
		UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(commonFolder);

		foreach (var serverFolder in serverFolders)
		{
			UnityProjectUtil.PruneEmptyDirectoriesAndMetaFiles(serverFolder);
		}

		// finally, backfill a meta for any folder that survived the prune without one, which covers both
		// a folder whose meta this run deleted and a folder the new source introduced.
		var writtenFolderMetaFiles = UnityProjectUtil.EnsureFolderMetaFiles(commonFolder, commonSnapshot);
		if (writtenFolderMetaFiles.Count > 0)
		{
			Log.Information(
				$"Wrote {writtenFolderMetaFiles.Count} folder meta files. Folders=[{string.Join(", ", writtenFolderMetaFiles)}]");
		}

		var missingMetaFiles = UnityProjectUtil.FindDirectoriesMissingMetaFiles(commonFolder);
		if (missingMetaFiles.Count > 0)
		{
			throw new CliException(
				$"Unity would ignore {missingMetaFiles.Count} directories in [{commonFolder}] because they have no meta file. " +
				$"Missing=[{string.Join(", ", missingMetaFiles)}]");
		}
	}
	
}
