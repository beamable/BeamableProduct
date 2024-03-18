using Beamable.Common.Semantics;
using cli.Dotnet;
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


public class NewStorageCommand : AppCommand<NewStorageCommandArgs>, IEmptyResult
{
	public NewStorageCommand() : base("storage", "Create and add a new Microstorage")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("name", "The name of the new Microstorage."),
			(args, i) => args.ProjectName = i);
		AddOption(new Option<string>("--existing-solution-file", () => string.Empty, description: "Relative path to current solution file to which standalone microservice should be added."),
			(args, i) => args.RelativeExistingSolutionFile = i);
		AddOption(new Option<ServiceName>("--new-solution-name", "The name of the solution of the new project. Use it if you want to create a new solution."),
			(args, i) => args.SolutionName = i);
		AddOption(new Option<string>("--service-directory", "Relative path to directory where microservice should be created. Defaults to \"SOLUTION_DIR/services\""),
			(args, i) => args.ServicesBaseFolderPath = i);
		AddOption(new SpecificVersionOption(), (args, i) => args.SpecifiedVersion = i);
		AddOption(new Option<bool>("--disable", "Created service by default would not be published"),
			(args, i) => args.Disabled = i);
		var storageDeps = new Option<List<string>>("--link-to", "The name of the project to link this storage to")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(storageDeps, (x, i) => x.linkedServices = i);
	}

	public override async Task Handle(NewStorageCommandArgs args)
	{
		args.ValidateConfig();
		// first, create the project...
		args.SolutionName = string.IsNullOrEmpty(args.SolutionName) ? args.ProjectName : args.SolutionName;

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
