using Beamable.Common.Semantics;
using cli.Utils;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.Project;

public class NewStorageCommandArgs : CommandArgs
{
	public ServiceName storageName;
	public string slnPath;
}

public class NewStorageCommand : AppCommand<NewStorageCommandArgs>
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
	}

	public override async Task Handle(NewStorageCommandArgs args)
	{
		// first, create the project...

		if (string.IsNullOrEmpty(args.slnPath))
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

		if (string.IsNullOrEmpty(args.slnPath))
		{
			throw new CliException($"Was not able to infer sln file, please provide one with --sln.",
				Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_SOLUTION_NOT_FOUND, true);
		}

		if (!File.Exists(args.slnPath))
		{
			throw new CliException($"No sln file found at path=[{args.slnPath}]",
				Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_SOLUTION_NOT_FOUND, true);
		}

		Log.Information(
			$"Registering local project... 'beam services register --id {args.storageName} --type EmbeddedMongoDb'");
		var storageDef = await args.BeamoLocalSystem.AddDefinition_EmbeddedMongoDb(args.storageName, "mongo:latest",
			new string[] { },
			CancellationToken.None);


		// identify the linkable projects...
		var services = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols;
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
		var dependencies = AnsiConsole.Prompt(prompt).ToArray();


		foreach (var service in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions)
		{
			var isDep = dependencies.Any(d => d.ToLowerInvariant().Equals(service.BeamoId.ToLowerInvariant()));
			if (!isDep) continue;
			var next = service.DependsOnBeamoIds.ToList();
			next.Add(storageDef.BeamoId);

			Log.Information(
				$"Adding storage=[{storageDef.BeamoId}] to service=[{service.BeamoId}] {nameof(service.DependsOnBeamoIds)} array.");
			service.DependsOnBeamoIds = next.ToArray();
		}

		foreach (var service in services)
		{
			var dockerfilePath = service.Value.RelativeDockerfilePath;
			Log.Information("Docker file path is " + dockerfilePath);
			var serviceFolder = Path.GetDirectoryName(dockerfilePath);
			Log.Information("Docker file folder is " + serviceFolder);

			var isDep = dependencies.Any(d => d == serviceFolder);
			if (!isDep) continue;

			dockerfilePath = args.ConfigService.GetFullPath(Path.Combine(service.Value.DockerBuildContextPath, dockerfilePath));
			var dockerfileText = await File.ReadAllTextAsync(dockerfilePath);

			const string search =
				"# <BEAM-CLI-INSERT-FLAG:COPY> do not delete this line. It is used by the beam CLI to insert custom actions";
			var replacement = @$"WORKDIR /subsrc/{args.storageName}
COPY {args.storageName}/. .
{search}";
			Log.Information($"Updating service=[{service.Key}] Dockerfile to include storage reference");
			dockerfileText = dockerfileText.Replace(search, replacement);
			await File.WriteAllTextAsync(dockerfilePath, dockerfileText);
		}

		args.BeamoLocalSystem.SaveBeamoLocalManifest();

		// add the project itself
		await args.ProjectService.CreateNewStorage(args.slnPath, args.storageName);

		foreach (var dependency in dependencies)
		{
			Log.Information($"Adding {args.storageName} reference to {dependency}. ");
			await args.ProjectService.AddProjectReference(args.slnPath, dependency, args.storageName);
		}
	}
}
