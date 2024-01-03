namespace cli.Utils;

public static class CliExceptions
{
	public static readonly CliException DOCKER_NOT_RUNNING =
		new ("Docker is not running in this machine.",
			Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_DOCKER_NOT_RUNNING, true,
			"Please start Docker before running this command.");
}
