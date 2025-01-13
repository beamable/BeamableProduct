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

		UnityProjectUtil.DeleteAllFilesWithExtensions(Path.Combine(info.packageFolder, "Common"), new string[]{".cs", ".cs.meta"});
		UnityProjectUtil.DeleteAllFilesWithExtensions(Path.Combine(infoServer.packageFolder, "SharedRuntime"), new string[]{".cs", ".cs.meta"});
		UnityProjectUtil.DeleteAllFilesWithExtensions(Path.Combine(infoServer.packageFolder, "Runtime/Common"), new string[]{".cs", ".cs.meta"});

		await UnityProjectUtil.DownloadPackage("Beamable.Common", info.beamableNugetVersion,
			"content/netstandard2.0/", Path.Combine(info.packageFolder, "Common"));
		
		await UnityProjectUtil.DownloadPackage("Beamable.Server.Common", info.beamableNugetVersion,
			"content/netstandard2.0/SharedRuntime/", Path.Combine(infoServer.packageFolder, "SharedRuntime"));
		
		await UnityProjectUtil.DownloadPackage("Beamable.Server.Common", info.beamableNugetVersion,
			"content/netstandard2.0/Runtime/Common/", Path.Combine(infoServer.packageFolder, "Runtime/Common"));

	}
	
}
