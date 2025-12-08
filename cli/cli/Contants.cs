namespace cli;

using System.Collections.Specialized;

public static class Constants
{
	public const string DEFAULT_SLN_NAME = "BeamableServices.sln";

	public const string PLATFORM_DEV = "https://dev.api.beamable.com";
	public const string PLATFORM_STAGING = "https://staging.api.beamable.com";
	public const string PLATFORM_PRODUCTION = "https://api.beamable.com";
	public const string DEFAULT_PLATFORM = PLATFORM_PRODUCTION;

	/// <summary>
	/// Key to extract <see cref="CliEnvironment.RefreshToken"/> from <see cref="Environment.GetEnvironmentVariable(string)"/>.
	/// </summary>
	public const string KEY_ENV_REFRESH_TOKEN = "BEAM_REFRESH_TOKEN";
	
	/// <summary>
	/// OpenAPI extension that describes the semantic type of a primitive field.
	/// </summary>
	public const string EXTENSION_BEAMABLE_SEMANTIC_TYPE = "x-beamable-semantic-type";

	/// <summary>
	/// OpenAPI extension, added here as a <see cref="SwaggerService.DetectNonSelfReferentialTypes"/>, 
	/// </summary>
	public const string EXTENSION_BEAMABLE_SELF_REFERENTIAL_TYPE = "x-beamable-self-referential-type";
}
