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
		
		string path =
			await args.ProjectService.AddToSolution(args.SolutionName, args.ProjectName, !args.SkipCommon);
		
		string projectDirectory = GetServicesDir(args, path);

		var sd = await args.BeamoLocalSystem.AddDefinition_HttpMicroservice(args.ProjectName.Value.ToLower(),
			projectDirectory,
			Path.Combine(args.ProjectName, "Dockerfile"),
			new string[] { },
			CancellationToken.None);

		if (!args.SkipCommon)
		{
			var service = args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId];
			args.ProjectService.CreateCommon(args.ProjectName, service.RelativeDockerfilePath,
				service.DockerBuildContextPath);
		}

		args.BeamoLocalSystem.SaveBeamoLocalManifest();
		args.BeamoLocalSystem.SaveBeamoLocalRuntime();

		await args.ProjectService.LinkProjects(_addUnityCommand, _addUnrealCommand, args.Provider);
	}
	
	private static string GetServicesDir(AddServiceToSolutionCommandArgs args, string newSolutionPath)
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
		catch
		{
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
		catch
		{
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
