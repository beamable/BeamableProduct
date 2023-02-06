using Beamable.Common;
using Beamable.Common.Dependencies;
using CliWrap;

namespace cli.Services;

public class ProjectData
{
	public HashSet<string> unityProjectsPaths = new HashSet<string>();
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
	
	public void AddUnityProject(string relativePath)
	{
		_projects.unityProjectsPaths.Add(relativePath);
		_configService.SaveDataFile( ".linkedProjects", _projects);
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
	
		// TODO: automatically install Beam.Templates if not installed... 
		
		// TODO: if the folder already exists, fail the command. 
		
		// create the solution
		await Cli.Wrap($"dotnet")
			.WithArguments($"new sln -n {solutionName} -o {solutionPath}")
			.ExecuteAsync().Task;
		
		// create the beam microservice project
		await Cli.Wrap($"dotnet")
			.WithArguments($"new beamservice -n {projectName} -o {projectPath}")
			.ExecuteAsync().Task;
		
		// restore the microservice tools
		await Cli.Wrap($"dotnet")
			.WithArguments($"tool restore --tool-manifest {Path.Combine(projectName, ".config", "dotnet-tools.json")}")
			.ExecuteAsync().Task;
		
		// add the microservice to the solution
		await Cli.Wrap($"dotnet")
			.WithArguments($"sln {solutionPath} add {projectPath}")
			.ExecuteAsync().Task;

		// create the shared library project
		await Cli.Wrap($"dotnet")
			.WithArguments($"new beamlib -n {commonProjectName} -o {commonProjectPath}")
			.ExecuteAsync().Task;
		
		// restore the shared library tools
		await Cli.Wrap($"dotnet")
			.WithArguments($"tool restore --tool-manifest {Path.Combine(commonProjectPath, ".config", "dotnet-tools.json")}")
			.ExecuteAsync().Task;
		
		// add the shared library to the solution
		await Cli.Wrap($"dotnet")
			.WithArguments($"sln {solutionPath} add {commonProjectPath}")
			.ExecuteAsync().Task;
		
		// add the shared library as a reference of the project
		await Cli.Wrap($"dotnet")
			.WithArguments($"add {projectPath} reference {commonProjectPath}")
			.ExecuteAsync().Task;

		return solutionPath;
	}
}
