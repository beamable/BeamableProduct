using cli.Services;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.Project;

public class UpdateUnityBeamPackageCommandArgs : CommandArgs
{
	public string path;
	public string version;
	public BeamNexusRepository repository;
	public bool skipServerPackage;
}

public class UpdateUnityBeamPackageCommand : AppCommand<UpdateUnityBeamPackageCommandArgs>
{
	public UpdateUnityBeamPackageCommand() : base("update-unity-beam-package", "Updates or adds Beamable packages to Unity project")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("path", "Relative path to the Unity project"), (args, i) => args.path = i);
		AddOption(new Option<string>("--version", () => string.Empty, "Version of beam package"), (args, i) => args.version = i);
		AddOption(new Option<BeamNexusRepository>("--repository", () => BeamNexusRepository.Release, "Beamable repository to use"), (args, i) => args.repository = i);
		AddOption(new ConfigurableOptionFlag("skip-server-package", "Skips adding server package"), (args, i) => args.skipServerPackage = i);
	}

	public override async Task Handle(UpdateUnityBeamPackageCommandArgs args)
	{
		var unityProjectClient = new ProjectClientHelper<UnityProjectClient>();
		var startingDir = args.path;

		var expectedUnityParentDirectories = new[]
		{
			".", // maybe the unity project a child of the current folder...
			".." // or maybe the unity project is a sibling of the current folder...
		}.Select(p => Path.Combine(startingDir, p)).ToArray();

		var defaultPaths = unityProjectClient.GetProjectClientTypeCandidates(expectedUnityParentDirectories).ToList();
		var directory = args.path;

		switch (defaultPaths.Count)
		{
			case 1:
				directory = defaultPaths[0];
				break;
			case 0:
				throw new CliException("Could not found Unity project");
			default:
				directory = AnsiConsole.Prompt(new SelectionPrompt<string>()
					.Title("Select Unity Project to update")
					.AddChoices(defaultPaths)
					.AddBeamHightlight());
				break;
		}


		var filePath = Path.Combine(directory, "Packages", "manifest.json");
		if (!File.Exists(filePath))
		{
			throw new CliException($"Could not found manifest.json file for project at path: {filePath}");
		}
		var packageManifest = UnityPackageManifest.FromFile(filePath);
		packageManifest.AddOrUpdateScopedRegistryForBeam(args.repository);
		packageManifest.SetBeamablePackagesVersion(args.version, !args.skipServerPackage);
		await packageManifest.SaveToFile(filePath);
		AnsiConsole.WriteLine($"Packages at {filePath} updated, current beamable version: {args.version}");
	}
}

