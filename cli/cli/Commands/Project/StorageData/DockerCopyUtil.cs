using CliWrap;
using Serilog;

namespace cli.Commands.Project.StorageData;

public static class DockerCopyUtil
{
	public enum CopyDirection
	{
		INTO_CONTAINER,
		FROM_CONTAINER
	}

	private static string GetCopyStr(string containerName,
		string containerPath,
		string hostPath,
		CopyDirection direction)
	{
		var containerPart = $"{containerName}:{containerPath}";
		switch (direction)
		{
			case CopyDirection.FROM_CONTAINER: return $"{containerPart} {hostPath}";
			case CopyDirection.INTO_CONTAINER: return $"{hostPath} {containerPart}";
			default: return "";
		}
	}


	public static async Task<bool> Copy(
		string dockerPath,
		string containerName,
		string containerPath,
		string hostPath,
		CopyDirection direction)
	{

		var argString = $"cp {GetCopyStr(containerName, containerPath, hostPath, direction)}";

		Log.Verbose($"docker exec string=[{dockerPath} {argString}]");

		var command = Cli
			.Wrap(dockerPath)
			.WithArguments(argString)
			.WithValidation(CommandResultValidation.None)
			.WithStandardOutputPipe(PipeTarget.ToDelegate(Log.Debug))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Error));


		var result = await command.ExecuteAsync();
		var isSuccess = result.ExitCode == 0;
		return isSuccess;
	}
}
