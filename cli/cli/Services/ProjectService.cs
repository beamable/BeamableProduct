using Beamable.Common;
using Beamable.Common.Dependencies;
using CliWrap;

namespace cli.Services;

public class ProjectService
{
	private readonly InitCommand _initCommand;
	private readonly ConfigService _configService;
	private readonly IDependencyProvider _provider;
	// TODO: automatically install Beam.Templates if not installed... 

	public ProjectService(InitCommand initCommand, ConfigService configService, IDependencyProvider provider)
	{
		_initCommand = initCommand;
		_configService = configService;
		_provider = provider;
	}
	
	public async Task CreateNewSolution(string directory, string solutionName, string projectName)
	{
		if (string.IsNullOrEmpty(directory))
		{
			directory = ".";
		}

		var path = Path.Combine(_configService.WorkingDirectory, directory);
		var projectPath = Path.Combine(path, projectName);
	
		await Cli.Wrap($"dotnet")
			.WithArguments($"new sln -n {solutionName} -o {path}")
			.ExecuteAsync().Task;
		
		await Cli.Wrap($"dotnet")
			.WithArguments($"new beamservice -n {projectName} -o {projectPath}")
			.ExecuteAsync().Task;
		
		await Cli.Wrap($"dotnet")
			.WithArguments($"sln {path} add {projectPath}")
			.ExecuteAsync().Task;

		_configService.SetTempWorkingDir(path);
		await _initCommand.Handle(new InitCommandArgs
		{
			Provider = _provider,
			saveToFile = true
		});
	}
}
