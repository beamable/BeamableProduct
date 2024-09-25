using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.Project;

public class NewProjectCommandArgs : CommandArgs
{
	public ServiceName ProjectName;
	public bool AutoInit;
	public List<string> LinkedStorages;
	public List<string> Groups = new();
}

public class AutoInitFlag : ConfigurableOptionFlag
{
	public AutoInitFlag() : base("init", "Automatically create a .beamable folder context if no context exists")
	{
		AddAlias("-i");
		IsHidden = true;
	}
}

public interface IHaveSolutionFlag
{
	public string SlnFilePath { get; set; }
	public string DefaultSolutionName { get; }
}

public static class IHaveSolutionFlagExtensions
{
	public static string GetSlnDirectory(this SolutionCommandArgs instance) => Path.GetDirectoryName(instance.SlnFilePath);
	public static bool GetSlnExists(this SolutionCommandArgs instance) => File.Exists(instance.SlnFilePath);
	public static string GetSlnFileName(this SolutionCommandArgs instance) => Path.GetFileNameWithoutExtension(instance.SlnFilePath);
}

public class SolutionCommandArgs : NewProjectCommandArgs
{
	public string SlnFilePath;
	public string ServicesBaseFolderPath;

	public static void ConfigureSolutionFlag<T>(AppCommand<T> command)
		where T : SolutionCommandArgs
	{
		command.AddOption(new Option<string>(
				name: "--sln",
				getDefaultValue: () =>
				{
					if (!ConfigService.TryToFindBeamableFolder(".", out var beamableFolder))
						return String.Empty; // will be converted into PROJECT/PROJECT.sln
					
					var path = Path.GetFullPath(Path.GetDirectoryName(beamableFolder));
					Log.Verbose($"creating default --sln value, found /.beamable=[{path}]");
					var firstSlnPath = Directory.EnumerateFiles(path, "*.sln", SearchOption.AllDirectories).FirstOrDefault();
					if (string.IsNullOrEmpty(firstSlnPath))
						return String.Empty; // will be converted into PROJECT/PROJECT.sln
					
					Log.Verbose($"found default .sln=[{firstSlnPath}]");
					var relativePath = Path.GetRelativePath(".", firstSlnPath);
					return relativePath;
				},
				description:
				"Relative path to the .sln file to use for the new project. " +
				"If the .sln file does not exist, it will be created. " +
				"When no option is configured, if this command is executing inside a .beamable folder, " +
				"then the first .sln found in .beamable/.. will be used. " +
				"If no .sln is found, the .sln path will be <name>.sln. " +
				"If no .beamable folder exists, then the <project>/<project>.sln will be used"),
			(args, i) =>
			{
				if (string.IsNullOrEmpty(i))
				{
					if (!ConfigService.TryToFindBeamableFolder(".", out var beamableFolder))
					{
						// no sln path is given, so we use the defaults.
						// this code-path really only exists when 
						//  the user passes the hidden `-i` flag
						args.SlnFilePath = Path.Combine(args.ProjectName, args.ProjectName + ".sln");
					}
					else
					{
						var path = Path.GetFullPath(Path.GetDirectoryName(beamableFolder));
						args.SlnFilePath = Path.GetRelativePath(".", Path.Combine(path, Constants.DEFAULT_SLN_NAME));
					}

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
		initArgs.path = ".";
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

public class SpecificVersionOption : Option<PackageVersion>
{
	public SpecificVersionOption() : base(
		name: "--version",
		getDefaultValue: () => VersionService.GetNugetPackagesForExecutingCliVersion().ToString(),
		description: $"Specifies version of Beamable project dependencies. Defaults to the current version of the CLI")
	{
	}
}

public class NewMicroserviceArgs : SolutionCommandArgs
{
	public bool GenerateCommon;
	public bool IsBeamableDev;
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
		var serviceDeps = new Option<List<string>>("--link-to", "The name of the storage to link this service to")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(serviceDeps, (x, i) => x.LinkedStorages = i);
		var groups = new Option<List<string>>("--groups", "Specify BeamableGroups for this service")
		{
			Arity = ArgumentArity.ZeroOrMore,
			AllowMultipleArgumentsPerToken = true
		};
		AddOption(groups, (x, i) => x.Groups = i);
		AddOption(new Option<bool>(
				name: "--generate-common",
				description: "If passed, will create a common library for this project"),
			(args, i) => args.GenerateCommon = i);

		AddOption(new Option<bool>("--beamable-dev", () => false, $"INTERNAL This enables a sane workflow for beamable developers to be happy and productive"),
			(args, i) => args.IsBeamableDev = i);
	}

	public override async Task Handle(NewMicroserviceArgs args)
	{
		await args.CreateConfigIfNeeded(_initCommand);

		var newMicroserviceInfo = await args.ProjectService.CreateNewMicroservice(args);

		var isBeamableDev = args.IsBeamableDev;

		// refresh beamoManifest
		Log.Verbose($"setting temp working dir solutiondir=[{newMicroserviceInfo.SolutionDirectory}]");
		var previousWorkingDir = args.ConfigService.WorkingDirectory;
		var previousCurrentDirectory = Environment.CurrentDirectory;
		args.ConfigService.SetTempWorkingDir(newMicroserviceInfo.SolutionDirectory);
		args.ConfigService.SetBeamableDirectory(".");
		try
		{
			await args.BeamoLocalSystem.InitManifest();
			if (!args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(args.ProjectName, out var sd))
			{
				Log.Verbose("manifest... \n " +
				            JsonConvert.SerializeObject(args.BeamoLocalSystem.BeamoManifest, Formatting.Indented));
				throw new CliException("cannot find recently generated project, " + args.ProjectName);
			}

			await args.BeamoLocalSystem.UpdateDockerFile(sd);

			var service = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId];


			await args.BeamoLocalSystem.InitManifest();

			// Make sure we have the correct docker file
			var regularDockerfilePath =
				args.ConfigService.BeamableRelativeToExecutionRelative(service.RelativeDockerfilePath);
			var beamableDevDockerfilePath = regularDockerfilePath + "-BeamableDev";
			if (File.Exists(beamableDevDockerfilePath))
			{
				if (isBeamableDev)
				{
					var beamableDevDockerfileContents = await File.ReadAllTextAsync(beamableDevDockerfilePath);
					await File.WriteAllTextAsync(regularDockerfilePath, beamableDevDockerfileContents);
				}

				// We always delete the -BeamableDev dockerfile from the template (for older versions of the template, this file does not exist so... we need to check for it).
				File.Delete(beamableDevDockerfilePath);
			}

			// Add dependencies if they exist
			string[] dependencies = null;
			var storages = args.BeamoLocalSystem.BeamoManifest.EmbeddedMongoDbLocalProtocols;

			if (storages.Count > 0)
			{
				if ((args.LinkedStorages == null || args.LinkedStorages.Count == 0) && !args.Quiet)
				{
					dependencies = GetChoicesFromPrompt(args.BeamoLocalSystem);
				}
				else if (args.LinkedStorages != null)
				{
					dependencies = GetDependenciesFromName(args.BeamoLocalSystem, args.LinkedStorages);
				}
			}

			if (dependencies != null)
			{
				foreach (var dependency in dependencies)
				{
					if (args.BeamoLocalSystem.BeamoManifest.TryGetDefinition(dependency, out var dependencyDefinition))
					{
						Log.Information("Adding {ArgsStorageName} reference to {Dependency}. ", dependency, args.ProjectName);
						await args.BeamoLocalSystem.AddProjectDependency(sd, dependencyDefinition.ProjectPath);
					}
				}
				await args.BeamoLocalSystem.UpdateDockerFile(sd); //Update dockerfile again in the case that dependencies were added
			}

			if (args.Groups.Count > 0)
			{
				args.BeamoLocalSystem.SetBeamGroups(new UpdateGroupArgs{ToAddGroups = args.Groups, Name = args.ProjectName});
			}
		}
		finally
		{
			//Go back to the default working dir
			args.ConfigService.SetTempWorkingDir(previousWorkingDir);
			args.BeamoLocalSystem.SaveBeamoLocalRuntime();
		}

	}

	private string[] GetChoicesFromPrompt(BeamoLocalSystem localSystem)
	{
		// identify the linkable projects...
		var storages = localSystem.BeamoManifest.EmbeddedMongoDbLocalProtocols;
		var choices = storages.Select(storage => storage.Key).ToList();

		var prompt = new MultiSelectionPrompt<string>()
			.Title("Storage Dependencies")
			.InstructionsText("Which storages will be added to this service?\n[grey](Press [blue]<space>[/] to toggle, " +
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

	private string[] GetDependenciesFromName(BeamoLocalSystem localSystem, List<string> dependencies)
	{
		if (dependencies == null)
		{
			return Array.Empty<string>();
		}

		var storages = localSystem.BeamoManifest.EmbeddedMongoDbLocalProtocols;
		var choices = new List<string>();
		foreach (var dep in dependencies)
		{
			var localProtocol = storages.FirstOrDefault(x => x.Key.Equals(dep)).Value;
			if (localProtocol != null)
			{
				choices.Add(dep);
			}
			else
			{
				Log.Warning($"The dependency {dep} does not exist in the local manifest and cannot be added as reference. Use `beam project deps add <service> <storage> to add after the storage is already created`");
			}
		}

		return choices.ToArray();
	}
}
