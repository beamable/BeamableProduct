using cli.Services.Unity;
using System.CommandLine;

namespace cli.UnityCommands;

public class DownloadNugetDepToUnityCommandArgs : CommandArgs
{
	public string packageId;
	public string packageVersion;
	public string packageSrcPath;
	public string outputPath;
}

public class DownloadNugetDepToUnityCommandOutput
{

}
public class DownloadNugetDepToUnityCommand : AtomicCommand<DownloadNugetDepToUnityCommandArgs, DownloadNugetDepToUnityCommandOutput>
{
	public override bool IsForInternalUse => true;

	public DownloadNugetDepToUnityCommand() : base("download-nuget-package", "Download a beamable nuget package dep into Unity ")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("packageId", "the nuget id of the package dep"), (args, i) => args.packageId = i);
		AddArgument(new Argument<string>("packageVersion", "the version of the package"), (args, i) => args.packageVersion = i);
		AddArgument(new Argument<string>("src", "the file path inside the package to copy"), (args, i) => args.packageSrcPath = i);
		AddArgument(new Argument<string>("dst", "the target location to place the copied files"), (args, i) => args.outputPath = i);
	}

	public override async Task<DownloadNugetDepToUnityCommandOutput> GetResult(DownloadNugetDepToUnityCommandArgs args)
	{
		await UnityProjectUtil.DownloadPackage(args.packageId, args.packageVersion, args.packageSrcPath, args.outputPath);
		return new DownloadNugetDepToUnityCommandOutput();
	}
}
