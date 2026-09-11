using Beamable.Server;
using cli.Services.Unity;
using System.CommandLine;

namespace cli.UnityCommands;

public class VerifyPackageMetasCommandArgs : CommandArgs
{
	public string packagePath;
}

public class VerifyPackageMetasCommandOutput
{
	/// <summary>
	/// The directories, relative to the given package path, that Unity would import but that have no
	/// sibling meta file.
	/// </summary>
	public List<string> directoriesMissingMetaFiles = new List<string>();

	public int directoriesChecked;
}

public class VerifyPackageMetasCommand : AtomicCommand<VerifyPackageMetasCommandArgs, VerifyPackageMetasCommandOutput>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public VerifyPackageMetasCommand() : base("verify-package-metas",
		"Verify that every importable directory in a Unity package has a sibling .meta file")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("packagePath", "the path to the Unity package folder to verify"),
			(args, i) => args.packagePath = i);
	}

	public override Task<VerifyPackageMetasCommandOutput> GetResult(VerifyPackageMetasCommandArgs args)
	{
		if (!Directory.Exists(args.packagePath))
		{
			throw new CliException($"No directory exists at packagePath=[{args.packagePath}]");
		}

		var missing = UnityProjectUtil.FindDirectoriesMissingMetaFiles(args.packagePath);
		var output = new VerifyPackageMetasCommandOutput
		{
			directoriesMissingMetaFiles = missing,
			directoriesChecked = Directory.GetDirectories(args.packagePath, "*", SearchOption.AllDirectories).Length
		};

		if (missing.Count > 0)
		{
			foreach (var path in missing)
			{
				Log.Error($"Missing meta file for directory=[{path}]");
			}

			throw new CliException(
				$"Unity would ignore {missing.Count} directories in [{args.packagePath}] because they have no meta file.");
		}

		Log.Information($"All {output.directoriesChecked} directories in [{args.packagePath}] have a meta file.");
		return Task.FromResult(output);
	}
}
