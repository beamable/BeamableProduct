using Beamable.Common;
using Beamable.Common.Semantics;
using cli.Dotnet;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using System.CommandLine;
using Beamable.Server;

namespace cli.Commands.Project;

public class NewProjectCommandArgs : CommandArgs
{
	public ServiceName ProjectName;
	public List<string> LinkedStorages;
	public List<string> Groups = new();
}

public interface IHaveSolutionFlag
{
	public string SlnFilePath { get; set; }
	public string DefaultSolutionName { get; }
}

public static class IHaveSolutionFlagExtensions
{
	public static string GetSlnDirectory(this IHasSolutionFileArg instance) => Path.GetDirectoryName(instance.SolutionFilePath);
	public static bool GetSlnExists(this IHasSolutionFileArg instance) => File.Exists(instance.SolutionFilePath);
	public static string GetSlnFileName(this IHasSolutionFileArg instance) => Path.GetFileNameWithoutExtension(instance.SolutionFilePath);
}

public interface IHasSolutionFileArg
{
	public string SolutionFilePath { get; set; }
}

public class SolutionCommandArgs : NewProjectCommandArgs, IHasSolutionFileArg
{
	public string SlnFilePath;

	public string SolutionFilePath
	{
		get => SlnFilePath;
		set => SlnFilePath = value;
	}

	public string ServicesBaseFolderPath;

	public static void ConfigureSolutionFlag<T>(AppCommand<T> command)
		where T : SolutionCommandArgs
	{
		ConfigureSolutionFlag<T>(command, args => Path.Combine(args.ProjectName, args.ProjectName + ".sln"));
	}
	
	public static void ConfigureSolutionFlag<T>(AppCommand<T> command, Func<T, string> generateSlnPathWhenNoBeamableFolderExists )
		where T : CommandArgs, IHasSolutionFileArg
	{
		command.AddOption(new Option<string>(
				name: "--sln",
				getDefaultValue: () =>
				{
					if (!ConfigService.TryToFindBeamableFolder(".", out var beamableFolder))
						return String.Empty; // will be converted into PROJECT/PROJECT.sln
					
					var path = Path.GetFullPath(Path.GetDirectoryName(beamableFolder));
					Log.Verbose($"creating default --sln value, found /.beamable=[{path}]");
					var firstSlnPath = Directory.EnumerateFiles(path, "*.sln", SearchOption.AllDirectories).FirstOrDefault(
						f =>
						{
							var slnContent = File.ReadAllText(f);
							// ignore the unity-like .sln file
							var hasUnityIsm = slnContent.Contains("\"Assembly-CSharp.csproj\"",
								StringComparison.InvariantCultureIgnoreCase);
							return !hasUnityIsm;
						});
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
						args.SolutionFilePath = generateSlnPathWhenNoBeamableFolderExists(args);
					}
					else
					{
						var path = Path.GetFullPath(Path.GetDirectoryName(beamableFolder));
						args.SolutionFilePath = Path.GetRelativePath(".", Path.Combine(path, Constants.DEFAULT_SLN_NAME));
					}

				}
				else
				{
					args.SolutionFilePath = i;
					if (!args.SolutionFilePath.EndsWith(".sln"))
					{
						args.SolutionFilePath += ".sln";
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

	public Promise CreateConfigIfNeeded(InitCommand command)
	{
		// TODO: I think this method can be deleted? 
		if (ConfigService.DirectoryExists.GetValueOrDefault(false))
		{
			return Promise.Success;
		}

		return Promise.Success;
	}

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
	public string TargetFramework;
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
		AddOption(
			new Option<string>(new string[] { "--target-framework", "-f" },
				() =>
				{
					return "net" + AppContext.TargetFrameworkName.Split('=')[1].Substring(1);
				},
				"The target framework to use for the new project. Defaults to the current dotnet runtime framework."),
			(args, i) =>
			{
				args.TargetFramework = i;
			});
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
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


		{ // saved for legacy reasons. I don't want the old commands to CRASH
			var hiddenDockerDevOption = new Option<bool>("--beamable-dev", () => false,
				$"INTERNAL This enables a sane workflow for beamable developers to be happy and productive")
			{
				IsHidden = true
			};
			AddOption(hiddenDockerDevOption,
				(args, i) =>
				{
					if (i)
					{
						Log.Warning("The --beamable-dev flag is obsolete and has no effect. ");
					}
				});
		}
	}

	public override async Task Handle(NewMicroserviceArgs args)
	{
		await args.CreateConfigIfNeeded(_initCommand);

		var newMicroserviceInfo = await args.ProjectService.CreateNewMicroservice(args);

		// refresh beamoManifest
		Log.Verbose($"setting temp working dir solutiondir=[{newMicroserviceInfo.SolutionDirectory}]");
		var previousWorkingDir = args.ConfigService.WorkingDirectory;
		var previousCurrentDirectory = Environment.CurrentDirectory;
		args.ConfigService.SetWorkingDir(newMicroserviceInfo.SolutionDirectory);
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
			args.ConfigService.SetWorkingDir(previousWorkingDir);
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
