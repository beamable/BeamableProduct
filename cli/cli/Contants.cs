namespace cli;

public static class Constants
{
	public const string CONFIG_FOLDER = ".beamable";
	public const string CONFIG_DEFAULTS_FILE_NAME = "config-defaults.json";

	public const string PLATFORM_DEV = "https://dev.api.beamable.com";
	public const string PLATFORM_STAGING = "https://staging.api.beamable.com";
	public const string PLATFORM_PRODUCTION = "https://api.beamable.com";
	public const string DEFAULT_PLATFORM = PLATFORM_PRODUCTION;

	public const string DOCKER_REGISTRY_DEV = "https://dev-microservices.beamable.com/v2/";
	public const string DOCKER_REGISTRY_STAGING = "https://staging-microservices.beamable.com/v2/";
	public const string DOCKER_REGISTRY_PRODUCTION = "https://microservices.beamable.com/v2/";
	
	public const string CONFIG_CID = "cid";
	public const string CONFIG_PID = "pid";
	public const string CONFIG_PLATFORM = "host";
	public const string CONFIG_ACCESS_TOKEN = "access_token";
	public const string CONFIG_REFRESH_TOKEN = "refresh_token";
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
