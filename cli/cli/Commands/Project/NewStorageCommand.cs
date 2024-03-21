using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.Project;

public class NewStorageCommandArgs : SolutionCommandArgs
{
	public List<string> linkedServices;
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
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		AddOption(new AutoInitFlag(), (args, b) => args.AutoInit = b);

		SolutionCommandArgs.Configure(this);

		var storageDeps = new Option<List<string>>("--link-to", "The name of the project to link this storage to")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(storageDeps, (x, i) => x.linkedServices = i);
	}

	public override async Task Handle(NewStorageCommandArgs args)
	{
		await args.CreateConfigIfNeeded(_initCommand);
		var newMicroserviceInfo = await args.ProjectService.CreateNewStorage(args);
		Log.Information(
			$"Registering local project... 'beam services register --id {args.ProjectName} --type EmbeddedMongoDb'");

		var storageDef = await args.BeamoLocalSystem.AddDefinition_EmbeddedMongoDb(args.ProjectName, "mongo:latest",
			args.ConfigService.GetRelativePath(newMicroserviceInfo.ServicePath),
			CancellationToken.None);

		string[] dependencies = null;
		if ((args.linkedServices == null || args.linkedServices.Count == 0) && !args.Quiet)
		{
			dependencies = GetChoicesFromPrompt(args.BeamoLocalSystem);
		}
		else if (args.linkedServices != null)
		{
			dependencies = GetDependenciesFromName(args.BeamoLocalSystem, args.linkedServices);
		}

		// add the project itself
		args.BeamoLocalSystem.SaveBeamoLocalManifest();

		if (dependencies == null)
			return;

		foreach (var dependency in dependencies)
		{
			if (args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(dependency, out var dependencyDefinition))
			{
				Log.Information("Adding {ArgsStorageName} reference to {Dependency}. ", args.ProjectName, dependency);
				await args.BeamoLocalSystem.AddProjectDependency(dependencyDefinition, storageDef);
			}
		}
	}

	private string[] GetDependenciesFromName(BeamoLocalSystem localSystem, List<string> dependencies)
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
			if (localProtocol != null)
			{
				var dockerfilePath = localProtocol.RelativeDockerfilePath;
				var serviceFolder = Path.GetDirectoryName(dockerfilePath);
				choices.Add(serviceFolder);
			}
		}

		return choices.ToArray();
	}

	private string[] GetChoicesFromPrompt(BeamoLocalSystem localSystem)
	{
		// identify the linkable projects...
		var services = localSystem.BeamoManifest.HttpMicroserviceLocalProtocols;
		var choices = new List<string>();
		foreach (var service in services)
		{
			var dockerfilePath = service.Value.RelativeDockerfilePath;
			var serviceFolder = Path.GetDirectoryName(dockerfilePath);
			choices.Add(serviceFolder);
		}

		var prompt = new MultiSelectionPrompt<string>()
			.Title("Service Dependencies")
			.InstructionsText("Which services will use this storage?\n[grey](Press [blue]<space>[/] to toggle, " +
							  "[green]<enter>[/] to accept)[/]")
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
