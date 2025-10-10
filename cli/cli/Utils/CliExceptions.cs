namespace cli.Utils;

public static class CliExceptions
{
	public static readonly CliException COMMAND_NO_LONGER_SUPPORTED =
		new("This command is no longer supported",
			Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_COMMAND_NO_LONGER_SUPPORTED, false);
	public static readonly CliException DOCKER_NOT_RUNNING =
		new("Docker is not running in this machine.",
			Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_DOCKER_NOT_RUNNING, true,
			"Please start Docker before running this command.");
	public static readonly CliException CONFIG_DOES_NOT_EXISTS =
		new("Could not find any .beamable config folder which is required for this command.",
			Beamable.Common.Constants.Features.Services.CMD_RESULT_CODE_CONFIG_DOES_NOT_EXISTS, true,
			"Consider calling `beam init` first.");
}
