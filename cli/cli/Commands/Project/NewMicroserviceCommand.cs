using Beamable.Common;
using Beamable.Common.Semantics;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Dotnet;

public class SolutionCommandArgs : CommandArgs
{
	public ServiceName SolutionName;
	public ServiceName ProjectName;
	public string SpecifiedVersion;
	public bool Disabled;
	public string RelativeNewSolutionDirectory;
	public string RelativeExistingSolutionFile;
	public string ServicesBaseFolderPath;
	
	
	public void ValidateConfig()
	{
		var shouldUseExistingSolution = !string.IsNullOrWhiteSpace(RelativeExistingSolutionFile);
		var shouldCreateNewSolution = !string.IsNullOrWhiteSpace(RelativeNewSolutionDirectory);
		shouldCreateNewSolution |= !string.IsNullOrWhiteSpace(SolutionName);
		
		if (shouldUseExistingSolution && shouldCreateNewSolution)
		{
			throw new CliException("Cannot specify both --existing-solution-file and --new-solution-directory or --new-solution-name options.");
		}
	}
}

public class SkipCommonOptionFlag : ConfigurableOptionFlag
{
	public SkipCommonOptionFlag() : base("skip-common", "If you should create a common library") { }
}

public class ServiceNameArgument : Argument<ServiceName>
{
	public ServiceNameArgument(string description = "Name of the new project") : base("name", description) { }
}

public class SpecificVersionOption : Option<string>
{
	public SpecificVersionOption() : base("--version", () => string.Empty,
		"Specifies version of Beamable project dependencies")
	{
	}
}

public class NewMicroserviceArgs : SolutionCommandArgs
{
	public bool SkipCommon;
}


public class RegenerateSolutionFilesCommandArgs : SolutionCommandArgs
{
	public bool SkipCommon;
	public string tempDirectory;
	public string projectDirectory;
}

public class NewMicroserviceCommand : AppCommand<NewMicroserviceArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;
	private readonly AddUnityClientOutputCommand _addUnityCommand;
	private readonly AddUnrealClientOutputCommand _addUnrealCommand;

	public NewMicroserviceCommand(InitCommand initCommand, AddUnityClientOutputCommand addUnityCommand,
		AddUnrealClientOutputCommand addUnrealCommand) : base("microservice",
		"Create new standalone microservice.")
	{
		_initCommand = initCommand;
		_addUnityCommand = addUnityCommand;
		_addUnrealCommand = addUnrealCommand;
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		AddOption(new Option<string>("--new-solution-directory", () => string.Empty, description: "Relative path to current directory where new solution should be created."),
			(args, i) => args.RelativeNewSolutionDirectory = i);
		AddOption(new Option<string>("--existing-solution-file", () => string.Empty, description: "Relative path to current solution file to which standalone microservice should be added."),
			(args, i) => args.RelativeExistingSolutionFile = i);
		AddOption(new SkipCommonOptionFlag(), (args, i) => args.SkipCommon = i);
		AddOption(new Option<ServiceName>("--new-solution-name", "The name of the solution of the new project. Use it if you want to create a new solution."),
			(args, i) => args.SolutionName = i);
		AddOption(new Option<string>("--service-directory", "Relative path to directory where microservice should be created. Defaults to \"SOLUTION_DIR/services\""),
			(args, i) => args.ServicesBaseFolderPath = i);
		AddOption(new SpecificVersionOption(), (args, i) => args.SpecifiedVersion = i);
		AddOption(new Option<bool>("--disable", "Created service by default would not be published"),
			(args, i) => args.Disabled = i);
	}

	public override async Task Handle(NewMicroserviceArgs args)
	{
		args.ValidateConfig();
		// Default the solution name to the project name.
		args.SolutionName = string.IsNullOrEmpty(args.SolutionName) ? args.ProjectName : args.SolutionName;
		// in the current directory, create a project using dotnet. 
		var newMicroserviceInfo = await args.ProjectService.CreateNewMicroservice(args);

		// initialize a beamable project in that directory...
		var createdNewWorkingDir = false;
		var currentPath = Directory.GetCurrentDirectory();
		if (!args.ConfigService.DirectoryExists.GetValueOrDefault(false))
		{
			args.ConfigService.SetTempWorkingDir(newMicroserviceInfo.SolutionDirectory);
			Directory.SetCurrentDirectory(newMicroserviceInfo.SolutionDirectory);

			try
			{
				await _initCommand.Handle(new InitCommandArgs { Provider = args.Provider, saveToFile = true });
			}
			catch
			{
				Directory.SetCurrentDirectory(currentPath);
				throw;
			}

			createdNewWorkingDir = true;
		}

		var sd = await args.ProjectService.AddDefinitonToNewService(args, newMicroserviceInfo);

		if (!args.SkipCommon)
		{
			var service = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId];
			await args.ProjectService.UpdateDockerFileWithCommonProject(args.ConfigService, args.ProjectName, service.RelativeDockerfilePath,
				service.DockerBuildContextPath);
		}
		args.BeamoLocalSystem.SaveBeamoLocalManifest();
		args.BeamoLocalSystem.SaveBeamoLocalRuntime();

		if (!args.Quiet)
		{
			await args.ProjectService.LinkProjects(_addUnityCommand, _addUnrealCommand, args.Provider);
		}

		if (createdNewWorkingDir) HandleCreatedNewWorkingDirectory(currentPath, newMicroserviceInfo.SolutionDirectory, args.SolutionName);
	}

	private static void HandleCreatedNewWorkingDirectory(string currentPath, string path, string solutionName)
	{
		Directory.SetCurrentDirectory(currentPath);
		BeamableLogger.Log("A new Beamable microservice project has been created successfully!");
		AnsiConsole.MarkupLine($"To get started:\n[lime]cd {path}[/]");
		AnsiConsole.MarkupLine($"Then open [lime]{solutionName}.sln[/] in your IDE and run the project");
	}
}
