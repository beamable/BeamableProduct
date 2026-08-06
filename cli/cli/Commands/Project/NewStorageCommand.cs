using Beamable.Common;
using cli.Services;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;
using Beamable.Server;

namespace cli.Commands.Project;

public class NewStorageCommandArgs : SolutionCommandArgs
{
	public List<string> linkedServices;
	public string targetFramework;
	public bool IsZone;
}


public class NewStorageCommand : AppCommand<NewStorageCommandArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;

	public NewStorageCommand(InitCommand initCommand) : base("storage", "Create and add a new Microstorage")
	{
		_initCommand = initCommand;
	}

	public override void Configure()
	{
		AddOption(
			new Option<string>(new string[] { "--target-framework", "-f" },
				() =>
				{
					var currentVersion = AppContext.TargetFrameworkName.Split('=')[1].Substring(1);
					var currentVersionDouble = double.Parse(currentVersion, System.Globalization.CultureInfo.InvariantCulture);
					if (currentVersionDouble < 8.0)
					{
						currentVersion = "8.0";
					}
					return "net" + currentVersion;
				},
				"The target framework to use for the new project. Defaults to the current dotnet runtime framework"),
			(args, i) =>
			{
				args.targetFramework = i;
			});
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		SolutionCommandArgs.Configure(this);

		var storageDeps = new Option<List<string>>("--link-to", "The name of the project to link this storage to")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(storageDeps, (x, i) => x.linkedServices = i);
		var groups = new Option<List<string>>("--groups", "Specify BeamableGroups for this service")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(groups, (x, i) => x.Groups = i);
		AddOption(new Option<bool>(
				name: "--zone",
				description: "If passed, creates a zone-scoped storage (deployed per cid.zid, into the zone manifest) instead of a realm-scoped one"),
			(args, i) => args.IsZone = i);
	}

	public override async Task Handle(NewStorageCommandArgs args)
	{
		await args.CreateConfigIfNeeded(_initCommand);
		var newMicroserviceInfo = await args.ProjectService.CreateNewStorage(args);
		Log.Information(
			$"Registering local project... 'beam services register --id {args.ProjectName} --type EmbeddedMongoDb'");

		var relativePathToStorage = newMicroserviceInfo.ServicePath;

		string[] dependencies = null;
		if ((args.linkedServices == null || args.linkedServices.Count == 0) && !args.Quiet)
		{
			dependencies = GetChoicesFromPrompt(args.BeamoLocalSystem, args.IsZone);
		}
		else if (args.linkedServices != null)
		{
			dependencies = GetDependenciesFromName(args.BeamoLocalSystem, args.linkedServices, args.IsZone);
		}

		if (dependencies == null)
			return;

		var definitions = new List<BeamoServiceDefinition>();

		foreach (var dependency in dependencies)
		{
			if (args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(dependency, out var dependencyDefinition))
			{
				Log.Information("Adding {ArgsStorageName} reference to {Dependency}. ", args.ProjectName, dependency);
				await args.BeamoLocalSystem.AddProjectDependency(dependencyDefinition, relativePathToStorage);
				definitions.Add(dependencyDefinition);
			}
		}

		args.ConfigService.SetWorkingDir(newMicroserviceInfo.SolutionDirectory);

		await args.BeamoLocalSystem.InitManifest();

		if (args.Groups.Count > 0)
		{
			args.BeamoLocalSystem.SetBeamGroups(new UpdateGroupArgs{ToAddGroups = args.Groups, Name = args.ProjectName});
		}

		var promises = new List<Promise<Unit>>();

		foreach (var def in definitions)
		{
			promises.Add(UpdateDependencyDockerFile(args, def));
		}

		var sequence = Promise.Sequence(promises);
		await sequence;
	}

	private async Promise UpdateDependencyDockerFile(NewStorageCommandArgs args, BeamoServiceDefinition definition)
	{
		await args.BeamoLocalSystem.UpdateDockerFile(definition);
	}

	private string[] GetDependenciesFromName(BeamoLocalSystem localSystem, List<string> dependencies, bool isZone)
	{
		if (dependencies == null)
		{
			return Array.Empty<string>();
		}

		var services = localSystem.BeamoManifest.HttpMicroserviceLocalProtocols;
		var choices = new List<string>();
		foreach (var dep in dependencies)
		{
			var localProtocol = services.FirstOrDefault(x => x.Key.Equals(dep)).Value;
			if (localProtocol == null)
			{
				Log.Warning($"The dependency {dep} does not exist in the local manifest and cannot be added as reference. Use `beam project deps add <service> <storage> to add after the storage is already created`");
				continue;
			}

			// A dependency cannot cross scopes — a zone storage can only be used by zone services and a realm
			// storage only by realm services, since they deploy into different manifests. Skip mismatches.
			if (!IsSameScope(localSystem, dep, isZone))
			{
				Log.Warning($"Skipping {dep}: a {(isZone ? "zone" : "realm")}-scoped storage cannot be a dependency of a " +
							$"{(isZone ? "realm" : "zone")}-scoped service. Only same-scope services can use this storage.");
				continue;
			}

			choices.Add(dep);
		}

		return choices.ToArray();
	}

	// A storage and the service that uses it must share a scope (both zone, or both realm); otherwise the
	// reference would span two manifests and could never deploy. Used to filter link candidates.
	private static bool IsSameScope(BeamoLocalSystem localSystem, string beamoId, bool isZone)
		=> localSystem.BeamoManifest.TryGetDefinition(beamoId, out var def) && def.IsZoneScoped == isZone;

	private string[] GetChoicesFromPrompt(BeamoLocalSystem localSystem, bool isZone)
	{
		// identify the linkable projects... only same-scope services can use this storage (a zone storage
		// belongs to the zone manifest, a realm storage to the realm manifest — a cross-scope link is invalid).
		var services = localSystem.BeamoManifest.HttpMicroserviceLocalProtocols;
		var choices = services.Select(service => service.Key)
			.Where(key => IsSameScope(localSystem, key, isZone))
			.ToList();

		var prompt = new MultiSelectionPrompt<string>()
			.Title("Service Dependencies")
			.InstructionsText("Which services will use this storage?\n[grey](Press [blue]<space>[/] to toggle," +
							  " [green]<enter>[/] to accept)[/]")
			.AddChoices(choices)
			.AddBeamHightlight()
			.NotRequired();
		foreach (string choice in choices)
		{
			prompt.Select(choice);
		}
		return AnsiConsole.Prompt(prompt).ToArray();
	}
}
