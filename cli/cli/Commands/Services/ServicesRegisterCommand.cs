
using System.CommandLine;

namespace cli;

public enum CliLogEventLevel
{
	/// <summary>
	/// Anything and everything you might want to know about
	/// a running block of code.
	/// </summary>
	Verbose,

	/// <summary>
	/// Internal system events that aren't necessarily
	/// observable from the outside.
	/// </summary>
	Debug,

	/// <summary>
	/// The lifeblood of operational intelligence - things
	/// happen.
	/// </summary>
	Information,

	/// <summary>
	/// Service is degraded or endangered.
	/// </summary>
	Warning,

	/// <summary>
	/// Functionality is unavailable, invariants are broken
	/// or data is lost.
	/// </summary>
	Error,

	/// <summary>
	/// If you have a pager, it goes off when one of these
	/// occurs.
	/// </summary>
	Fatal
}


public class ServicesRegisterCommand
{
	public static readonly Option<string> BEAM_SERVICE_OPTION_ID =
		new("--id", "The Unique Id for this service within this Beamable CLI context");

	public static readonly Option<string[]> BEAM_SERVICE_OPTION_DEPENDENCIES = new("--deps",
		"The ','-separated list of existing Beam-O Ids that this service depends on");

	public static readonly Option<string> HTTP_MICROSERVICE_OPTION_LOCAL_BUILD_CONTEXT = new("--local-build-context",
		"The path to a valid docker build context with a Dockerfile in it");

	public static readonly Option<string> HTTP_MICROSERVICE_OPTION_LOCAL_DOCKERFILE =
		new("--local-dockerfile",
			"The relative file path, from the given build-context, to a valid Dockerfile inside that context");

	public static readonly Option<CliLogEventLevel?> HTTP_MICROSERVICE_OPTION_LOCAL_LOG_LEVEL =
		new("--local-log", $"The log level this service should be deployed locally with");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_HEALTH_ENDPOINT_AND_PORT = new(
		"--local-health-endpoint",
		"The health check endpoint and port, with no trailing or heading '/', that determines if application is up.\n" +
		"Example: --local-health-endpoint health 6565");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_HOT_RELOADING = new("--local-hot-reloading",
		"In order, (1, 2) an endpoint and port, with no trailing or heading '/', that determines if the server is set up for hot-reloading.\n" +
		"(3) the relative path, with no trailing or heading '/', to where the source files to watch are.\n" +
		"(4) the in-container path where the container expects the source files to be in order to support hot-reloading.\n" +
		"Example: --local-hot-reloading hot_reloading_enabled 6565 Local\\Path\\To\\Files\\My\\Image\\Expects Path\\In\\Container\\Where\\Files\\Need\\To\\Be");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_PORTS = new("--local-custom-ports",
		"Any number of arguments representing pairs of ports in the format LocalPort:InContainerPort.\n" +
		"Leaving local port empty, as in ':InContainerPort', will expose the container port at the next available local port (this changes every container creation)\n");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_BIND_MOUNTS = new(
		"--local-custom-bind-mounts",
		"Any number of arguments in the format LOCAL_PATH:IN_CONTAINER_PATH to bind between your machine and the docker container");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_VOLUMES = new(
		"--local-custom-volumes",
		"Any number of arguments in the format VOLUME_NAME:IN_CONTAINER_PATH to create and bind named volumes into the docker container");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_ENV_VARS = new("--local-env-vars",
		"Any number of arguments in the format NAME=VALUE representing environment variables to set into the local container");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_REMOTE_HEALTH_ENDPOINT;

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_REMOTE_ENV_VARS = new("--remote-env-vars",
		"Any number of arguments in the format 'NAME=VALUE' representing environment variables Beam-O should set on the container it runs in AWS");
}
