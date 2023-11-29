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
	public bool SkipCommon;
	public string SpecifiedVersion;
	public bool Disabled;
}

public class SkipCommonOptionFlag : ConfigurableOptionFlag
{
	public SkipCommonOptionFlag() : base("skip-common", "If you should create a common library") { }
}

public class ServiceNameArgument : Argument<ServiceName>
{
	public ServiceNameArgument() : base("name", "Name of the new project") { }
}

public class SpecificVersionOption : Option<string>
{
	public SpecificVersionOption() : base("--version", () => string.Empty,
		"Specifies version of Beamable project dependencies")
	{
	}
}

public class NewSolutionCommandArgs : SolutionCommandArgs
{
	public string directory;
	public bool quiet;
}

public class QuietNameOption : Option<bool>
{
	public QuietNameOption() : base("--quiet", () => false, "When true, automatically accept path suggestions")
	{
		AddAlias("-q");
	}
}

public class RegenerateSolutionFilesCommandArgs : SolutionCommandArgs
{
	public string tempDirectory;
	public string projectDirectory;
}

public class NewSolutionCommand : AppCommand<NewSolutionCommandArgs>, IStandaloneCommand
{
	private readonly InitCommand _initCommand;
	private readonly AddUnityClientOutputCommand _addUnityCommand;
	private readonly AddUnrealClientOutputCommand _addUnrealCommand;

	public NewSolutionCommand(InitCommand initCommand, AddUnityClientOutputCommand addUnityCommand,
		AddUnrealClientOutputCommand addUnrealCommand) : base("new",
		"Start a brand new beamable solution using dotnet")
	{
		_initCommand = initCommand;
		_addUnityCommand = addUnityCommand;
		_addUnrealCommand = addUnrealCommand;
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		AddArgument(new Argument<string>("output", () => string.Empty, description: "Where the project be created"),
			(args, i) => args.directory = i);
		AddOption(new SkipCommonOptionFlag(), (args, i) => args.SkipCommon = i);
		AddOption(new Option<ServiceName>("--solution-name", "The name of the solution of the new project"),
			(args, i) => args.SolutionName = i);
		AddOption(new SpecificVersionOption(), (args, i) => args.SpecifiedVersion = i);
		AddOption(new Option<bool>("--disable", "Create service that is disabled on publish"),
			(args, i) => args.Disabled = i);
		AddOption(new QuietNameOption(), (args, i) => args.quiet = i);
	}

	public override async Task Handle(NewSolutionCommandArgs args)
	{
		// Default the solution name to the project name.
		args.SolutionName = string.IsNullOrEmpty(args.SolutionName) ? args.ProjectName : args.SolutionName;

		// in the current directory, create a project using dotnet. 
		var path = await args.ProjectService.CreateNewSolution(args);

		// initialize a beamable project in that directory...
		var createdNewWorkingDir = false;
		var currentPath = Directory.GetCurrentDirectory();
		if (!args.ConfigService.ConfigFileExists.GetValueOrDefault(false))
		{
			args.ConfigService.SetTempWorkingDir(path);
			Directory.SetCurrentDirectory(path);

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

		var sd = await args.ProjectService.AddDefinitonToNewService(args,path);
		
		if (!args.SkipCommon)
		{
			var service = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId];
			await args.ProjectService.CreateCommon(args.ConfigService, args.ProjectName, service.RelativeDockerfilePath,
				service.DockerBuildContextPath);
		}
		args.BeamoLocalSystem.SaveBeamoLocalManifest();
		args.BeamoLocalSystem.SaveBeamoLocalRuntime();

		await args.ProjectService.LinkProjects(_addUnityCommand, _addUnrealCommand, args.Provider);

		if (createdNewWorkingDir) HandleCreatedNewWorkingDirectory(currentPath, path, args.SolutionName);
	}

	private static void HandleCreatedNewWorkingDirectory(string currentPath, string path, string solutionName)
	{
		Directory.SetCurrentDirectory(currentPath);
		BeamableLogger.Log("A new Beamable microservice project has been created successfully!");
		AnsiConsole.MarkupLine($"To get started:\n[lime]cd {path}[/]");
		AnsiConsole.MarkupLine($"Then open [lime]{solutionName}.sln[/] in your IDE and run the project");
	}
}
