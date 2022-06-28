namespace cli;

public static class Constants
{
	public const string CONFIG_FOLDER = ".beamable";
	public const string CONFIG_DEFAULTS_FILE_NAME = "config-defaults.json";

	public const string PLATFORM_DEV = "https://dev.api.beamable.com";
	public const string PLATFORM_STAGING = "https://staging.api.beamable.com";
	public const string PLATFORM_PRODUCTION = "https://api.beamable.com";
	public const string DEFAULT_PLATFORM = PLATFORM_PRODUCTION;

	public const string CONFIG_CID = "cid";
	public const string CONFIG_PID = "pid";
	public const string CONFIG_PLATFORM = "host";
	public const string CONFIG_ACCESS_TOKEN = "access_token";
	public const string CONFIG_REFRESH_TOKEN = "refresh_token";
}
