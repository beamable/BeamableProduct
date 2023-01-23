using CliWrap;

namespace cli.Services;

public class ProjectService
{
	// TODO: automatically install Beam.Templates if not installed... 
	
	public async Task CreateNewSolution(string directory, string solutionName, string projectName)
	{
		if (string.IsNullOrEmpty(directory))
		{
			directory = ".";
		}
		
		await Cli.Wrap($"dotnet")
			.WithArguments($"new sln -n {solutionName} -o {directory}")
			.ExecuteAsync().Task;
		
		await Cli.Wrap($"dotnet")
			.WithArguments($"new beamservice -n {projectName} -o {directory}/{projectName}")
			.ExecuteAsync().Task;
		
		await Cli.Wrap($"dotnet")
			.WithArguments($"sln {directory} add {directory}/{projectName}")
			.ExecuteAsync().Task;
	}
}
