using cli.Services.Unity;
using System.CommandLine;

namespace cli.UnityCommands;

public class GetUnityVersionInfoCommandArgs : CommandArgs
{
	public string unityProjectPath;
}

public class GetUnityVersionInfoCommandOutput
{
	public string beamableNugetVersion;
	public string sdkVersion;
	public string packageFolder;
}

public class GetUnityVersionInfoCommand : AtomicCommand<GetUnityVersionInfoCommandArgs, GetUnityVersionInfoCommandOutput>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public GetUnityVersionInfoCommand() : base("get-version-info", "get information about a beamable unity sdk project's version dependencies")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("unityPath", "the path to the root of the unity project"), (args, unityPath) =>
		{
			if (Path.HasExtension(unityPath))
			{
				throw new CliException($"given unityPath must be a directory, unityPath=[${unityPath}]");
			}
			args.unityProjectPath = unityPath;
		});
	}

	public override Task<GetUnityVersionInfoCommandOutput> GetResult(GetUnityVersionInfoCommandArgs args)
	{
		return Task.FromResult(UnityProjectUtil.GetUnityInfo(args.unityProjectPath, "com.beamable"));
	}
}
