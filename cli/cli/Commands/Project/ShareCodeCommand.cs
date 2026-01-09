using Beamable.Common;
using System.CommandLine;
using System.Reflection;
using System.Runtime.Loader;
using Beamable.Server;
using static Beamable.Common.Constants.Directories;

namespace cli.Dotnet;

public class ShareCodeCommandArgs : CommandArgs
{
	public string dllPath;
	public string[] dependencyPrefixBlackList;

}
public class ShareCodeCommand : AppCommand<ShareCodeCommandArgs>
{
	public ShareCodeCommand() : base("share-code", "Given a dll, copy the dll to the associated unity projects")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("source", "The .dll filepath for the built code"), (arg, i) => arg.dllPath = i);
		AddOption(new Option<string>("--dep-prefix-blacklist", () =>

			 "System"
		, "A list of namespace prefixes to ignore when copying dependencies"), (arg, i) =>
		{
			var entries = i.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			arg.dependencyPrefixBlackList = entries;
		});
	}

	public override Task Handle(ShareCodeCommandArgs args)
	{
		var absolutePath = Path.GetFullPath(args.dllPath);
		var fileName = Path.GetFileName(absolutePath);

		var absoluteDir = Path.GetDirectoryName(absolutePath);

		var dlls = Directory.GetFiles(absoluteDir, "*.dll");



		// take the dll, and copy it into Unity...
		var linkedUnityProjects = args.ProjectService.GetLinkedUnityProjects();
		Log.Debug($"linked unity projects=[{string.Join(",", linkedUnityProjects)}]");
		foreach (var unityProjectPath in linkedUnityProjects)
		{
			var unityAssetPath = Path.Combine(args.ConfigService.BeamableWorkspace, unityProjectPath, "Assets");
			if (!Directory.Exists(unityAssetPath))
			{
				BeamableLogger.LogError($"Could not copy shared project [{fileName}] because linked unity project because directory doesn't exist [{unityAssetPath}]");
				continue;
			}

			var outputDirectory = Path.Combine(args.ConfigService.BeamableWorkspace, unityProjectPath, SAMS_COMMON_DLL_DIR);
			Directory.CreateDirectory(outputDirectory);
			
			Log.Debug($"-unity project=[{unityAssetPath}] src-dll-folder=[{outputDirectory}]");
			for (var i = 0; i < dlls.Length; i++)
			{

				var dllPath = dlls[i];
				var dllName = Path.GetFileName(dllPath);

				Log.Verbose($"--considering dll=[{dllPath}]");
				if (args.dependencyPrefixBlackList.Any(dllName.StartsWith)) continue;
				if (dllName.StartsWith("System")) continue;

				var outputPath = Path.GetRelativePath(absoluteDir, dllPath);
				outputPath = Path.Combine(outputDirectory, outputPath);

				Log.Debug($"--copying dll=[{dllPath}] output=[{outputPath}]");
				File.Copy(dllPath, outputPath, true);
			}
		}

		return Task.CompletedTask;
	}
}
