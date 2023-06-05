using Beamable.Common.Semantics;
using Serilog;
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
		AddArgument(new Argument<ServiceName>("solution-name", "The name of the solution of the new project"), (args, i) => args.SolutionName = i);
		AddOption(new ConfigurableOptionFlag("skip-common", "If you should create a common library"), (args, i) => args.SkipCommon = i);
	}

	public override async Task Handle(AddServiceToSolutionCommandArgs args)
	{
		// in the current directory, create a project using dotnet. 
		string projectPath = await args.ProjectService.AddToSolution(args.SolutionName, args.ProjectName,!args.SkipCommon);

		var sd = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(args.ProjectName.Value.ToLower(),
			projectPath,
			Path.Combine(args.ProjectName, "Dockerfile"),
			new string[] { },
			CancellationToken.None);
		
		if (!args.SkipCommon)
		{
			var service = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId];
			args.ProjectService.CreateCommon(args.ProjectName, service.RelativeDockerfilePath, service.DockerBuildContextPath);
		}

		args.BeamoLocalSystem.SaveBeamoLocalManifest();
		args.BeamoLocalSystem.SaveBeamoLocalRuntime();

		await args.ProjectService.LinkProjects(_addUnityCommand, _addUnrealCommand, args.Provider);
	}
}
