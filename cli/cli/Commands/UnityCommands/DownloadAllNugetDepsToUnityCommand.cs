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
		var info = GetUnityVersionInfoCommand.GetUnityInfo(args.unityProjectPath, "com.beamable");

		if (info.beamableNugetVersion == "0.0.123")
		{
			throw new CliException("Cannot download nuget packages for developer 0.0.123 version.");
		}

		await DownloadNugetDepToUnityCommand.DownloadPackage("Beamable.Common", info.beamableNugetVersion,
			"content/netstandard2.0/", Path.Combine(info.packageFolder, "Common"));
		
		return new DownloadAllNugetDepsToUnityCommandOutput();
	}
}
