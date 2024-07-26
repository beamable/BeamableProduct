using CliWrap;
using Serilog;
using System.CommandLine;

namespace cli;

public class DotnetPathOption : Option<string>
{
	public DotnetPathOption() : base("--dotnet-path", "a custom location for dotnet")
	{
	}
}

public class DockerPathOption : Option<string>
{
	public static readonly DockerPathOption Instance = new DockerPathOption();
	private DockerPathOption() : base(
		name: "--docker-cli-path", 
		description: "a custom location for docker. By default, the CLI will attempt to resolve" +
		             $" docker through its usual install locations. You can also use the {ConfigService.ENV_VAR_DOCKER_EXE} " +
		             "environment variable to specify. ")
	{
		if (TryGetDockerPath(out var dockerPath, out _))
		{
			Description += "\nCurrently, a docker path has been automatically identified.";
			SetDefaultValue(dockerPath);
		}
		else
		{
			if (!string.IsNullOrEmpty(ConfigService.CustomDockerExe))
			{
				SetDefaultValue(ConfigService.CustomDockerExe);
			}
			Description += "\nCurrently, no docker path is available, and you must set this option to use docker CLI.";
		}
	}
	
	
	public static bool TryGetDockerPath(out string dockerPath, out string errorMessage)
	{
		dockerPath = ConfigService.CustomDockerExe;
		errorMessage = null;
		if (!string.IsNullOrEmpty(dockerPath))
		{
			// the path is specified, so we must use it.
			if (!TryValidateDockerExec(dockerPath, out var message))
			{
				errorMessage = $"specified docker path via {ConfigService.ENV_VAR_DOCKER_EXE} env var, but {message}";
				return false;
			}

			return true;
		}

		var paths = new string[]
		{
			"docker", // hopefully its just on the PATH
			"C:\\Program Files\\Docker\\Docker\\resources\\bin\\docker.exe", // common windows installation
			"E:\\Program Files\\Docker\\Docker\\resources\\bin\\docker.exe", // common windows installation
			"D:\\Program Files\\Docker\\Docker\\resources\\bin\\docker.exe", // common windows installation
			"/usr/local/bin/docker", // common mac installation
		};
		foreach (var path in paths)
		{
			if (!TryValidateDockerExec(path, out _))
				continue;

			dockerPath = path;
			return true; // yahoo!
		}

		errorMessage =
			$"docker executable not found using common paths. Please specify with {ConfigService.ENV_VAR_DOCKER_EXE} env var";
		return false;

	}
	
	
	public static bool TryValidateDockerExec(string candidatePath, out string message)
	{
		message = null;
		Log.Verbose($"testing docker candidate=[{candidatePath}]");

		var command = CliWrap.Cli
			.Wrap(candidatePath)
			.WithStandardOutputPipe(PipeTarget.ToDelegate(x =>
			{
				Log.Verbose($"docker-version-check-stdout=[{x}]");
			}))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(x =>
			{
				Log.Verbose($"docker-version-check-stderr=[{x}]");
			}))
			.WithArguments("--version")
			.WithValidation(CommandResultValidation.None);

		try
		{
			var res = command.ExecuteAsync();
			res.Task.Wait();
			var success = res.Task.Result.ExitCode == 0;
			if (!success)
			{
				message = $"given path=[{candidatePath}] is not a valid docker executable";
				return false;
			}

			Log.Verbose($"found docker path=[{candidatePath}]");
			return true;
		}
		catch (Exception ex)
		{
			Log.Verbose($"given path=[{candidatePath}] was not able to invoke docker, and threw an error=[{ex.Message}]");
			return false;
		}
	}

}
