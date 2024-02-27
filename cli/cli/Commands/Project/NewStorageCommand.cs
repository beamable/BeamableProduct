using Beamable.Common.Semantics;
using cli.Services;
using cli.Utils;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.Project;

public class NewStorageCommandArgs : CommandArgs
{
	public ServiceName storageName;
	public string slnPath;
	public List<string> linkedServices;
	public bool quiet;
	public string outputPath;
}

public class QuietNameOption : Option<bool>
{
	public QuietNameOption() : base("--quiet", () => false, "When true, skip input waiting and use defaults")
	{
		AddAlias("-q");
	}
}

public class NewStorageCommand : AppCommand<NewStorageCommandArgs>, IEmptyResult
{
	public NewStorageCommand() : base("new-storage", "Create and add a new Microstorage")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("name", "The name of the new Microstorage."),
			(args, i) => args.storageName = i);
		AddOption(new Option<string>("--sln", "The path to the solution that the Microstorage will be added to"),
			(args, i) => args.slnPath = i);
		AddOption(new Option<string>("--output-path", "The path where the storage is going to be created, a new sln is going to be created as well"),
			(args, i) => args.outputPath = i);

		var storageDeps = new Option<List<string>>("--link-to", "The name of the project to link this storage to")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(storageDeps, (x, i) => x.linkedServices = i);
		AddOption(new QuietNameOption(), (args, i) => args.quiet = i);
	}

	public override async Task Handle(NewStorageCommandArgs args)
	{
		// first, create the project...

		bool ignoreSln = !string.IsNullOrEmpty(args.outputPath);

		if (!ignoreSln && string.IsNullOrEmpty(args.slnPath))
		{
			// we can make some best-effort attempts to find the .sln file.
			// 1. if there is exactly 1 .sln file in our current directly, use that.

			var files = Directory.GetFiles(args.AppContext.WorkingDirectory);
			var slnFiles = files.Where(f => f.EndsWith(".sln")).ToArray();
			if (slnFiles.Length == 1)
			{
				args.slnPath = slnFiles[0];
			}
		}

		if (!ignoreSln && string.IsNullOrEmpty(args.slnPath))
		{
			throw new CliException($"Was not able to infer sln file, please provide one with --sln.",
				Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_SOLUTION_NOT_FOUND, true);
		}

		if (!ignoreSln && !File.Exists(args.slnPath))
		{
			string correctSlnPath = string.Empty;
			string dir = Path.GetDirectoryName(args.slnPath);
			if (!string.IsNullOrWhiteSpace(dir))
			{
				var files = Directory.GetFiles(dir);
				var slnFiles = files.Where(f => f.EndsWith(".sln")).ToArray();
				if (slnFiles.Length == 1)
				{
					correctSlnPath = slnFiles[0];
				}
			}

			var exception = new CliException($"No sln file found at path=[{args.slnPath}]",
				Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_SOLUTION_NOT_FOUND, true,
				string.IsNullOrWhiteSpace(correctSlnPath) ? null : $"Try using \"{correctSlnPath}\" as --sln option value");

			throw exception;
		}

		var path = ignoreSln ? args.outputPath : args.slnPath;
		Log.Information(
			$"Registering local project... 'beam services register --id {args.storageName} --type EmbeddedMongoDb'");
		var storageDef = await args.BeamoLocalSystem.AddDefinition_EmbeddedMongoDb(args.storageName, "mongo:latest",
			args.ProjectService.GeneratePathForProject(path, args.storageName),
			CancellationToken.None);

		string[] dependencies = null;
		if ((args.linkedServices == null || args.linkedServices.Count == 0) && !args.quiet)
		{ 
			dependencies = GetChoicesFromPrompt(args.BeamoLocalSystem);
		}
		else if(args.linkedServices != null)
		{
			dependencies = GetDependencieFromName(args.BeamoLocalSystem, args.linkedServices);
		}

		// add the project itself
		_ = await args.ProjectService.CreateNewStorage(args.slnPath, args.outputPath, args.storageName, args.quiet);
		args.BeamoLocalSystem.SaveBeamoLocalManifest();

		if (dependencies == null)
			return;

		foreach (var dependency in dependencies)
		{
			if (args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(dependency, out var dependencyDefinition))
			{
				Log.Information("Adding {ArgsStorageName} reference to {Dependency}. ", args.storageName, dependency);
				await args.BeamoLocalSystem.AddProjectDependency(dependencyDefinition, storageDef);
			}
		}
	}

	private string[] GetDependencieFromName(BeamoLocalSystem localSystem, List<string> dependencies)
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
