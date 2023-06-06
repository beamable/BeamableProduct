using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Services;
using CliWrap;
using Serilog;
using Spectre.Console;
using System.CommandLine;
using UnityEngine;

namespace cli.Dotnet;

public class NewSolutionCommandArgs : CommandArgs
{
	public ServiceName SolutionName;
	public ServiceName ProjectName;
	public string directory;
	public bool SkipCommon;
}

public class NewSolutionCommand : AppCommand<NewSolutionCommandArgs>
{
	private readonly InitCommand _initCommand;
	private readonly AddUnityClientOutputCommand _addUnityCommand;
	private readonly AddUnrealClientOutputCommand _addUnrealCommand;

	public NewSolutionCommand(InitCommand initCommand, AddUnityClientOutputCommand addUnityCommand, AddUnrealClientOutputCommand addUnrealCommand) : base("new",
		"Start a brand new beamable solution using dotnet")
	{
		_initCommand = initCommand;
		_addUnityCommand = addUnityCommand;
		_addUnrealCommand = addUnrealCommand;
	}

	public override void Configure()
	{
		AddArgument(new Argument<ServiceName>("name", "Name of the new project"), (args, i) => args.ProjectName = i);
		AddArgument(new Argument<string>("output", () => "", description: "Where the project be created"), (args, i) => args.directory = i);
		AddOption(new ConfigurableOptionFlag("skip-common", "If you should create a common library"), (args, i) => args.SkipCommon = i);
		AddOption(new Option<ServiceName>("--solution-name", "The name of the solution of the new project"), (args, i) => args.SolutionName = i);
	}

	public override async Task Handle(NewSolutionCommandArgs args)
	{
		// Default the solution name to the project name.
		args.SolutionName = string.IsNullOrEmpty(args.SolutionName) ? args.ProjectName : args.SolutionName;

		// in the current directory, create a project using dotnet. 
		var path = await args.ProjectService.CreateNewSolution(args.directory, args.SolutionName, args.ProjectName, !args.SkipCommon);

		// initialize a beamable project in that directory...
		var createdNewWorkingDir = false;
		if (!args.ConfigService.ConfigFileExists.GetValueOrDefault(false))
		{
			args.ConfigService.SetTempWorkingDir(path);


			await _initCommand.Handle(new InitCommandArgs { Provider = args.Provider, saveToFile = true });
			createdNewWorkingDir = true;
		}

		// Find path to service folders: either it is in the working directory, or it will be inside 'args.name\\services' from the working directory.
		string projectDirectory = createdNewWorkingDir ? "services" : GetServicesDir(args, path);

		// now that a .beamable folder has been created, setup the beamo manifest
		var sd = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(args.ProjectName.Value.ToLower(),
			projectDirectory,
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

	private static string GetServicesDir(NewSolutionCommandArgs args, string newSolutionPath)
	{
		string result = string.Empty;
		//using try catch because of the Directory.EnumerateDirectories behaviour
		try
		{
			var list = Directory.EnumerateDirectories(args.ConfigService.BaseDirectory,
				$"{args.SolutionName}\\services",
				SearchOption.AllDirectories).ToList();
			if (list.Count > 0)
			{
				result = Path.GetRelativePath(args.ConfigService.BaseDirectory, list.First());
			}
		}
		catch {
			//
		}

		try
		{
			if (string.IsNullOrWhiteSpace(result))
			{
				var list = Directory.EnumerateDirectories(newSolutionPath, "services",
					SearchOption.AllDirectories).ToList();
				result = Path.GetRelativePath(args.ConfigService.BaseDirectory, list.First());
			}
		}
		catch {
			//
		}

		if (string.IsNullOrWhiteSpace(result))
		{
			const string SERVICES_PATH_ERROR = "Could not find Solution services path!";
			Log.Error(SERVICES_PATH_ERROR);
		}

		return result;
	}
}
