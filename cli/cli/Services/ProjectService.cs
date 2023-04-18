using Beamable.Common;
using Beamable.Common.Dependencies;
using CliWrap;
using Serilog;

namespace cli.Services;

public class ProjectData
{
	public HashSet<string> unityProjectsPaths = new HashSet<string>();
	public HashSet<string> unrealProjectsPaths = new HashSet<string>();
}

public class ProjectService
{
	private readonly ConfigService _configService;

	private ProjectData _projects;

	public ProjectService(ConfigService configService)
	{
		_configService = configService;
		_projects = configService.LoadDataFile<ProjectData>(".linkedProjects");
	}

	public List<string> GetLinkedUnityProjects()
	{
		return _projects.unityProjectsPaths.ToList();
	}

	public List<string> GetLinkedUnrealProjects()
	{
		return _projects.unrealProjectsPaths.ToList();
	}

	public void AddUnityProject(string relativePath)
	{
		_projects.unityProjectsPaths.Add(relativePath);
		_configService.SaveDataFile(".linkedProjects", _projects);
	}

	public void AddUnrealProject(string relativePath)
	{
		_projects.unrealProjectsPaths.Add(relativePath);
		_configService.SaveDataFile(".linkedProjects", _projects);
	}

	public async Task EnsureCanUseTemplates()
	{
		var canUseTemplates = await Cli.Wrap("dotnet")
			.WithArguments($"new list --tag beamable")
			.WithValidation(CommandResultValidation.None)
			.ExecuteAsync().Select(res => res.ExitCode == 0).Task;

		if (!canUseTemplates)
		{
			throw new CliException("Cannot access Beamable.Templates dotnet templates. Please install the Beamable templates and try again.");
		}
	}

	public async Task AddProjectReference(string slnFilePath, string project, string projectReference)
	{
		var slnDirectory = Path.GetDirectoryName(slnFilePath);
		var rootServicesPath = Path.Combine(slnDirectory, "services");

		var projectPath = Path.Combine(rootServicesPath, project);
		var referencePath = Path.Combine(rootServicesPath, projectReference);
		await Cli.Wrap($"dotnet")
			.WithArguments($"add {projectPath} reference {referencePath}")
			.ExecuteAsyncAndLog().Task;
	}

	public async Task CreateNewStorage(string slnFilePath, string storageName)
	{
		var slnDirectory = Path.GetDirectoryName(slnFilePath);
		var rootServicesPath = Path.Combine(slnDirectory, "services");
		var storagePath = Path.Combine(rootServicesPath, storageName);

		if (Directory.Exists(storagePath))
		{
			throw new CliException("Cannot create a storage because the directory already exists");
		}

		await EnsureCanUseTemplates();

		// create the beam microservice project
		await Cli.Wrap($"dotnet")
			.WithArguments($"new beamstorage -n {storageName} -o {storagePath}")
			.ExecuteAsyncAndLog().Task;

		// add the new project as a reference to the solution
		await Cli.Wrap($"dotnet")
			.WithArguments($"sln {slnFilePath} add {storagePath}")
			.ExecuteAsyncAndLog().Task;
	}

	public async Task<string> CreateNewSolution(string directory, string solutionName, string projectName)
	{
		if (string.IsNullOrEmpty(directory))
		{
			directory = projectName;
		}

		var solutionPath = Path.Combine(_configService.WorkingDirectory, directory);
		var rootServicesPath = Path.Combine(solutionPath, "services");
		var commonProjectName = $"{projectName}Common";
		var projectPath = Path.Combine(rootServicesPath, projectName);
		var commonProjectPath = Path.Combine(rootServicesPath, commonProjectName);

		if (Directory.Exists(solutionPath))
		{
			throw new CliException("Cannot create a solution because the directory already exists");
		}

		// check that we have the templates available
		await EnsureCanUseTemplates();
		// create the solution
		await Cli.Wrap($"dotnet")
			.WithArguments($"new sln -n {solutionName} -o {solutionPath}")
			.ExecuteAsyncAndLog().Task;

		// create the beam microservice project
		await Cli.Wrap($"dotnet")
			.WithArguments($"new beamservice -n {projectName} -o {projectPath}")
			.ExecuteAsyncAndLog().Task;

		// restore the microservice tools
		await Cli.Wrap($"dotnet")
			.WithArguments($"tool restore --tool-manifest {Path.Combine(projectName, ".config", "dotnet-tools.json")}")
			.ExecuteAsyncAndLog().Task;

		// add the microservice to the solution
		await Cli.Wrap($"dotnet")
			.WithArguments($"sln {solutionPath} add {projectPath}")
			.ExecuteAsyncAndLog().Task;

		// create the shared library project
		await Cli.Wrap($"dotnet")
			.WithArguments($"new beamlib -n {commonProjectName} -o {commonProjectPath}")
			.ExecuteAsyncAndLog().Task;

		// restore the shared library tools
		await Cli.Wrap($"dotnet")
			.WithArguments($"tool restore --tool-manifest {Path.Combine(commonProjectPath, ".config", "dotnet-tools.json")}")
			.ExecuteAsyncAndLog().Task;

		// add the shared library to the solution
		await Cli.Wrap($"dotnet")
			.WithArguments($"sln {solutionPath} add {commonProjectPath}")
			.ExecuteAsyncAndLog().Task;

		// add the shared library as a reference of the project
		await Cli.Wrap($"dotnet")
			.WithArguments($"add {projectPath} reference {commonProjectPath}")
			.ExecuteAsyncAndLog().Task;

		return solutionPath;
	}
}

public static class CliExtensions
{
	public static CommandTask<CommandResult> ExecuteAsyncAndLog(this Command command)
	{
		Log.Information($"Running '{command.TargetFilePath} {command.Arguments}'");
		return command.ExecuteAsync();
	}
}
