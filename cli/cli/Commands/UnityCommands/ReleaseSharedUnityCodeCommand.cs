using Serilog;
using System.CommandLine;
using System.IO.Compression;

namespace cli.UnityCommands;

public class ReleaseSharedUnityCodeCommandArgs : CommandArgs
{
	public string csProjPath;
	public string unityProjectPath;
	public string packageRelativeTarget;
	public string packageId;
}

public class ReleaseSharedUnityCodeCommandOutput
{
	public string message;
}

public class ReleaseSharedUnityCodeCommand : AtomicCommand<ReleaseSharedUnityCodeCommandArgs, ReleaseSharedUnityCodeCommandOutput>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public ReleaseSharedUnityCodeCommand() : base("release-shared-code", "Copy the various shared code projects into the Beamable Unity SDK")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("csprojPath", "path to csproj project"), (args, csProjPath) =>
		{
			if (!File.Exists(csProjPath))
			{
				throw new CliException($"no file exists at given csProjPath=[{Path.GetFullPath(csProjPath)}]");
			}

			if (!csProjPath.EndsWith(".csproj"))
			{
				throw new CliException($"given csproj path must be a .csproj file. csProjPath=[{Path.GetFullPath(csProjPath)}]");
			}
			
			args.csProjPath = csProjPath;
		});

		AddArgument(new Argument<string>("unityPath", "relative path to Unity destination for src files"),
			(args, unityPath) =>
			{
				if (Path.HasExtension(unityPath))
				{
					throw new CliException($"given unityPath must be a directory, unityPath=[${unityPath}]");
				}
				args.unityProjectPath = unityPath;
			});

		AddArgument(new Argument<string>("packageId", "the name of the package to copy into"),
			(args, i) => args.packageId = i);
		
		AddArgument(new Argument<string>("packageRelativePath", "relative path to Unity destination for src files"),
			(args, unityPath) =>
			{
				if (Path.HasExtension(unityPath))
				{
					throw new CliException($"given packageRelativeTarget must be a directory, unityPath=[${unityPath}]");
				}
				args.packageRelativeTarget = unityPath;
			});
	}

	public override Task<ReleaseSharedUnityCodeCommandOutput> GetResult(ReleaseSharedUnityCodeCommandArgs args)
	{
		
		// we know that the CLI csproj is relative to the unity client path...
		
		
		
		var info = GetUnityVersionInfoCommand.GetUnityInfo(args.unityProjectPath, args.packageId);
		if (info.beamableNugetVersion != "0.0.123")
		{
			return Task.FromResult(new ReleaseSharedUnityCodeCommandOutput
			{
				message = "ignoring"
			});
		}

		var dstPath = Path.Combine(info.packageFolder, args.packageRelativeTarget);
		Log.Information($"Copying code src=[{args.csProjPath}] to dst=[{dstPath}]");

		// clean up all old cs and meta files, while leaving possible Unity specific files, like asmdef files.
		DeleteAllFilesWithExtensions(dstPath, new string[]{".cs", ".cs.meta"});

		CopyProjectSrcToUnityCommand.CopyProject(args.csProjPath, dstPath);
		return Task.FromResult(new ReleaseSharedUnityCodeCommandOutput
		{
			message = $"Copying code src=[{args.csProjPath}] to dst=[{dstPath}]"
		});
	}

	public static void DeleteAllFilesWithExtensions(string folder, string[] extensions)
	{
		foreach (var ext in extensions)
		{
			var filesToDelete = Directory.GetFiles(folder, $"*{ext}", SearchOption.AllDirectories);
			foreach (var file in filesToDelete)
			{
				File.Delete(file);
			}
		}
	}


}
