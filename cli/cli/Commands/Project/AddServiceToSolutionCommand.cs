using Beamable.Common.Semantics;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class AddServiceToSolutionCommandArgs : CommandArgs
{
	public ServiceName ProjectName;
	public ServiceName SolutionName;
	public bool SkipCommon;
}

public class AddServiceToSolutionCommand : AppCommand<AddServiceToSolutionCommandArgs>
{
	private readonly AddUnityClientOutputCommand _addUnityCommand;
	private readonly AddUnrealClientOutputCommand _addUnrealCommand;

	public AddServiceToSolutionCommand(AddUnityClientOutputCommand addUnityCommand,
		AddUnrealClientOutputCommand addUnrealCommand) : base("add",
		"Add new project to an existing solution in current working directory")
	{
		_addUnityCommand = addUnityCommand;
		_addUnrealCommand = addUnrealCommand;
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("name", "Name of the new project"), (args, i) => args.ProjectName = i);
		AddOption(new Option<ServiceName>("--solution-name", "Name of the existing solution"),
			(args, i) => args.SolutionName = i);
		AddOption(new ConfigurableOptionFlag("skip-common", "If you should create a common library"),
			(args, i) => args.SkipCommon = i);
	}

	public override async Task Handle(AddServiceToSolutionCommandArgs args)
	{
		if (string.IsNullOrEmpty(args.SolutionName.Value))
		{
			List<string> solutionPaths = Directory.GetFiles(args.ConfigService.WorkingDirectory, "*.sln",
				SearchOption.TopDirectoryOnly).ToList();

			List<string> solutionFiles = new();

			foreach (string solutionPath in solutionPaths)
			{
				string[] split = solutionPath.Split(Path.DirectorySeparatorChar);
				string solutionName = split[^1].Split(".")[0];
				solutionFiles.Add(solutionName);
			}

			switch (solutionFiles.Count)
			{
				case 0:
					throw new CliException(
						$"No solution files found in {args.ConfigService.WorkingDirectory} directory");
				case 1:
					args.SolutionName = new ServiceName(solutionFiles[0]);
					break;
				default:
				{
					solutionFiles.Add("cancel");

					string selection = AnsiConsole.Prompt(
						new SelectionPrompt<string>()
							.Title("Select solution file You would like new project add to:")
							.AddChoices(solutionFiles)
					);

					if (selection == "cancel")
					{
						return;
					}

					args.SolutionName = new ServiceName(selection);
					break;
				}
			}
		}

		string projectPath =
			await args.ProjectService.AddToSolution(args.SolutionName, args.ProjectName, !args.SkipCommon);

		var sd = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(args.ProjectName.Value,
			projectPath,
			Path.Combine(args.ProjectName, "Dockerfile"),
			new string[] { },
			CancellationToken.None);

		if (!args.SkipCommon)
		{
			var service = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId];
			await args.ProjectService.CreateCommon(args.ConfigService, args.ProjectName, service.RelativeDockerfilePath,
				service.DockerBuildContextPath);
		}

		args.BeamoLocalSystem.SaveBeamoLocalManifest();
		args.BeamoLocalSystem.SaveBeamoLocalRuntime();

		await args.ProjectService.LinkProjects(_addUnityCommand, _addUnrealCommand, args.Provider);
	}
}
