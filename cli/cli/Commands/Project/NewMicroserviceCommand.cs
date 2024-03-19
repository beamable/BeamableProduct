using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Utils;
using System.CommandLine;

namespace cli.Commands.Project;

public class NewProjectCommandArgs : CommandArgs
{
	public ServiceName ProjectName;
	public bool AutoInit;

}

public class AutoInitFlag : ConfigurableOptionFlag
{
	public AutoInitFlag() : base("init", "Automatically create a .beamable folder context if no context exists")
	{
		AddAlias("-i");
	}
}

public interface IHaveSolutionFlag
{
	public string SlnFilePath { get; set; }
	public string DefaultSolutionName { get; }
}

public static class IHaveSolutionFlagExtensions
{
	public static string GetSlnDirectory(this IHaveSolutionFlag instance) => Path.GetDirectoryName(instance.SlnFilePath);
	public static bool GetSlnExists(this IHaveSolutionFlag instance) => File.Exists(instance.SlnFilePath);
	public static string GetSlnFileName(this IHaveSolutionFlag instance) => Path.GetFileNameWithoutExtension(instance.SlnFilePath);
}

public class SolutionCommandArgs : NewProjectCommandArgs, IHaveSolutionFlag
{
	public string SlnFilePath { get; set; }
	string IHaveSolutionFlag.DefaultSolutionName => ProjectName;
	// public bool SlnExists => File.Exists(SlnFilePath);
	// public string SlnDirectory => Path.GetDirectoryName(SlnFilePath);
	// public string SlnFileName => Path.GetFileNameWithoutExtension(SlnFilePath);
	
	public string SpecifiedVersion;
	public bool Disabled;
	public string ServicesBaseFolderPath;

	public static void ConfigureSolutionFlag<T>(AppCommand<T> command)
		where T : CommandArgs, IHaveSolutionFlag
	{
		command.AddOption(new Option<string>(
				name: "--sln",
				getDefaultValue: () => string.Empty,
				description:
				"Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. By default, when no value is provided, the .sln path will be <name>/<name>.sln"),
			(args, i) =>
			{
				if (string.IsNullOrEmpty(i))
				{
					// no sln path is given, so we use the defaults.
					args.SlnFilePath = Path.Combine(args.DefaultSolutionName, args.DefaultSolutionName + ".sln");
				}
				else
				{
					args.SlnFilePath = i;
					if (!args.SlnFilePath.EndsWith(".sln"))
					{
						args.SlnFilePath += ".sln";
					}
				}
				
			});

	}
	
	/// <summary>
	/// Register common solution based options
	/// </summary>
	/// <param name="command"></param>
	/// <typeparam name="T"></typeparam>
	public static void Configure<T>(AppCommand<T> command) 
		where T : SolutionCommandArgs
	{
		ConfigureSolutionFlag(command);

		command.AddOption(new Option<string>(
				name: "--service-directory", 
				description: "Relative path to directory where project should be created. Defaults to \"SOLUTION_DIR/services\""),
			(args, i) => args.ServicesBaseFolderPath = i);
		
		command.AddOption(new SpecificVersionOption(), (args, i) => args.SpecifiedVersion = i);
		
		command.AddOption(new Option<bool>(
				name: "--disable", 
				description: "Created service by default would not be published"),
			(args, i) => args.Disabled = i);
	}
	
	public async Promise CreateConfigIfNeeded(InitCommand command)
	{
		if (ConfigService.DirectoryExists.GetValueOrDefault(false))
		{
			return;
		}
		if (!AutoInit)
		{
			throw CliExceptions.CONFIG_DOES_NOT_EXISTS;
		}
		
		
		var initArgs = Create<InitCommandArgs>();
		initArgs.saveToFile = true;
		var oldDir = initArgs.ConfigService.WorkingDirectory;
		initArgs.ConfigService.SetTempWorkingDir(this.GetSlnDirectory());
		await command.Handle(initArgs);
		initArgs.ConfigService.SetTempWorkingDir(oldDir);
		initArgs.ConfigService.SetBeamableDirectory(this.GetSlnDirectory());

		
	}
	
	// public void 
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

}


public class RegenerateSolutionFilesCommandArgs : SolutionCommandArgs
{
	public string tempDirectory;
	public string projectDirectory;
}

public class NewMicroserviceCommand : AppCommand<NewMicroserviceArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;
	private readonly AddUnityClientOutputCommand _addUnityCommand;
	private readonly AddUnrealClientOutputCommand _addUnrealCommand;

	public NewMicroserviceCommand(InitCommand initCommand, AddUnityClientOutputCommand addUnityCommand,
		AddUnrealClientOutputCommand addUnrealCommand) : base("service",
		"Create a new microservice project")
	{
		_initCommand = initCommand;
		_addUnityCommand = addUnityCommand;
		_addUnrealCommand = addUnrealCommand;
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		AddOption(new AutoInitFlag(), (args, b) => args.AutoInit = b);
		SolutionCommandArgs.Configure(this);
	}

	public override async Task Handle(NewMicroserviceArgs args)
	{
		// args.ValidateConfig();
		await args.CreateConfigIfNeeded(_initCommand);
		
		var newMicroserviceInfo = await args.ProjectService.CreateNewMicroservice(args);

		var sd = await args.ProjectService.AddDefinitonToNewService(args, newMicroserviceInfo);
		
		var service = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId];
		await args.ProjectService.UpdateDockerFileWithCommonProject(args.ConfigService, args.ProjectName, service.RelativeDockerfilePath,
			service.DockerBuildContextPath);
	
		args.BeamoLocalSystem.SaveBeamoLocalManifest();
		args.BeamoLocalSystem.SaveBeamoLocalRuntime();

		if (!args.Quiet)
		{
			await args.ProjectService.LinkProjects(_addUnityCommand, _addUnrealCommand, args.Provider);
		}
	}
}
