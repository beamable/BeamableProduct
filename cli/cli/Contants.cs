namespace cli;
using System.Collections.Specialized;

public static class Constants
{
	public const string CONFIG_FOLDER = ".beamable";
	public const string CONFIG_LOCAL_OVERRIDES_DIRECTORY = $"{TEMP_FOLDER}/overrides";
	public const string CONTENT_DIRECTORY = "content";
	public const string CONTENT_SNAPSHOTS_DIRECTORY = "content-snapshots";
	public const string CONFIG_DEFAULTS_FILE_NAME = "connection-configuration.json";
	public const string CONFIG_LINKED_PROJECTS = "linked-projects.json";
	
	public const string CONFIG_GIT_IGNORE_FILE_NAME = ".gitignore";
	public const string CONFIG_SVN_IGNORE_FILE_NAME = ".svnignore";
	public const string CONFIG_P4_IGNORE_FILE_NAME = ".p4ignore";
	public const string CONFIG_TOKEN_FILE_NAME = "connection-auth.json";

	public const string CONTENT_TAGS_FORMAT = "{0}-manifest-content-tags.json";
	public const string OLD_CONTENT_TAGS_FORMAT = "contentTags_{0}.json";

	public const string DEFAULT_SLN_NAME = "BeamableServices.sln";

	/// <summary>
	/// The full-path to where we are storing the <see cref="BeamoManifest"/>.
	/// TODO: This part will get abstracted out --- probably into <see cref="ConfigService"/> --- so that we can move this to the Beamable.Common library or some shared space.
	/// </summary>
	public const string BEAMO_LOCAL_MANIFEST_FILE_NAME = "local-services-manifest.json";

	/// <summary>
	/// The full-path to where we are storing the <see cref="BeamoRuntime"/> data. We need to serialize runtime data as, in most cases, we'll need to survive domain reloads or multiple runs of the cli.
	/// TODO: This part will get abstracted out --- probably into <see cref="ConfigService"/> --- so that we can move this to the Beamable.Common library or some shared space.
	/// </summary>
	public const string BEAMO_LOCAL_RUNTIME_FILE_NAME = "local-services-runtime-cache.json";

	public const int CMD_RESULT_CONFIG_RESOLUTION_CONFLICT = 701;

	public const string TEMP_FOLDER = "temp";

	public const string TEMP_OTEL_FOLDER = "otel";

	public const string TEMP_OTEL_LOGS_FOLDER = "logs";
	public const string TEMP_OTEL_METRICS_FOLDER = "metrics";
	public const string TEMP_OTEL_TRACES_FOLDER = "traces";

	public static readonly string[] TEMP_FILES = new[]
	{
		CONFIG_TOKEN_FILE_NAME, 
		BEAMO_LOCAL_RUNTIME_FILE_NAME,
	};

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
	/// Keys that are required to exist in Config File used by <see cref="ConfigService"/>.
	/// </summary>
	public static readonly string[] REQUIRED_CONFIG_KEYS = new[] { CONFIG_PLATFORM, CONFIG_CID, };


	public static readonly OrderedDictionary RENAMED_FILES = new()
	{
		{ "config-defaults.json", CONFIG_DEFAULTS_FILE_NAME },
		{ "user-token.json", CONFIG_TOKEN_FILE_NAME },
		{ "beamoLocalRuntime.json", BEAMO_LOCAL_RUNTIME_FILE_NAME },
		{ "beamoLocalManifest.json", BEAMO_LOCAL_MANIFEST_FILE_NAME },
		{ ".linkedProjects.json", CONFIG_LINKED_PROJECTS }
	};

	public static readonly OrderedDictionary RENAMED_DIRECTORIES = new()
	{
		{ "Content", CONTENT_DIRECTORY }
	};

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

	/// <summary>
	/// OpenAPI extension that describes the semantic type of a primitive field.
	/// </summary>
	public const string EXTENSION_BEAMABLE_SEMANTIC_TYPE = "x-beamable-semantic-type";

	/// <summary>
	/// OpenAPI extension, added here as a <see cref="SwaggerService.DetectNonSelfReferentialTypes"/>, 
	/// </summary>
	public const string EXTENSION_BEAMABLE_SELF_REFERENTIAL_TYPE = "x-beamable-self-referential-type";
}
