namespace cli;

public static class Constants
{
	public const string CONFIG_FOLDER = ".beamable";
	public const string CONFIG_DEFAULTS_FILE_NAME = "config-defaults.json";
	public const string CONFIG_GIT_IGNORE_FILE_NAME = ".gitignore";
	public const string CONFIG_SVN_IGNORE_FILE_NAME = ".svnignore";
	public const string CONFIG_P4_IGNORE_FILE_NAME = ".p4ignore";
	public const string CONFIG_TOKEN_FILE_NAME = "user-token.json";

	/// <summary>
	/// The full-path to where we are storing the <see cref="BeamoManifest"/>.
	/// TODO: This part will get abstracted out --- probably into <see cref="ConfigService"/> --- so that we can move this to the Beamable.Common library or some shared space.
	/// </summary>
	// private readonly string _beamoLocalManifestFile;
	public const string BEAMO_LOCAL_MANIFEST_FILE_NAME = "beamoLocalManifest";

	/// <summary>
	/// The full-path to where we are storing the <see cref="BeamoRuntime"/> data. We need to serialize runtime data as, in most cases, we'll need to survive domain reloads or multiple runs of the cli.
	/// TODO: This part will get abstracted out --- probably into <see cref="ConfigService"/> --- so that we can move this to the Beamable.Common library or some shared space.
	/// </summary>
	// private readonly string _beamoLocalRuntimeFile;
	public const string BEAMO_LOCAL_RUNTIME_FILE_NAME = "beamoLocalRuntime";

	public static readonly string[] FILES_TO_IGNORE = new[] { CONFIG_TOKEN_FILE_NAME, BEAMO_LOCAL_MANIFEST_FILE_NAME, BEAMO_LOCAL_RUNTIME_FILE_NAME };

	public const string PLATFORM_DEV = "https://dev.api.beamable.com";
	public const string PLATFORM_STAGING = "https://staging.api.beamable.com";
	public const string PLATFORM_PRODUCTION = "https://api.beamable.com";
	public const string DEFAULT_PLATFORM = PLATFORM_PRODUCTION;
	public const string CONFIG_CID = "cid";
	public const string CONFIG_PID = "pid";
	public const string CONFIG_DIR = "dir";
	public const string CONFIG_PLATFORM = "host";
	public const string CONFIG_ACCESS_TOKEN = "access-token";
	public const string CONFIG_REFRESH_TOKEN = "refresh-token";
	public const string CONFIG_HEADER = "header";


	/// <summary>
	/// Key to extract <see cref="CliEnvironment.LogLevel"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>. 
	/// </summary>
	public const string KEY_ENV_LOG_LEVEL = "BEAM_LOG_LEVEL";

	/// <summary>
	/// Key to extract <see cref="CliEnvironment.Api"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>.
	/// </summary>
	public const string KEY_ENV_API = "BEAM_API";

	/// <summary>
	/// Key to extract <see cref="CliEnvironment.Cid"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>.
	/// </summary>
	public const string KEY_ENV_CID = "BEAM_CID";

	/// <summary>
	/// Key to extract <see cref="CliEnvironment.Pid"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>.
	/// </summary>
	public const string KEY_ENV_PID = "BEAM_PID";

	/// <summary>
	/// Key to extract <see cref="CliEnvironment.AccessToken"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>.
	/// </summary>
	public const string KEY_ENV_ACCESS_TOKEN = "BEAM_ACCESS_TOKEN";

	/// <summary>
	/// Key to extract <see cref="CliEnvironment.RefreshToken"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>.
	/// </summary>
	public const string KEY_ENV_REFRESH_TOKEN = "BEAM_REFRESH_TOKEN";

	/// <summary>
	/// Key to extract <see cref="CliEnvironment.ConfigDir"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>.
	/// </summary>
	public const string KEY_ENV_CONFIG_DIR = "BEAM_CONFIG_DIR";
}
