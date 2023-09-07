using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesRegisterCommandArgs : LoginCommandArgs
{
	public string BeamoId;
	public BeamoProtocolType? ProtocolType;

	public string[] ServiceDependencies;

	// HttpMicroservice args
	public HttpSpecificArgs HttpSpecificArgs;

	// Embedded Mongo args
	public string BaseImage;
}

public struct HttpSpecificArgs
{
	public string LocalDockerBuildContext;
	public string LocalDockerfileRelativePath;
	public LogEventLevel? LocalLogLevel;
	public string[] LocalHealthEndpointAndPort;
	public string[] LocalHotReloadingConfig;

	// These all get initialized to string[0] instead of null because they are NOT presented in the interactive mode.
	public string[] LocalCustomPorts;
	public string[] LocalCustomBindMounts;
	public string[] LocalCustomVolumes;

	public string[] LocalCustomEnvVars;
	// These all get initialized to string[0] instead of null because they are NOT presented in the interactive mode.


	public string[] RemoteHealthEndpointAndPort;
	public string[] RemoteCustomEnvVars;
}

public class ServicesRegisterCommand : AppCommand<ServicesRegisterCommandArgs>
{
	public static readonly Option<string> BEAM_SERVICE_OPTION_ID = new("--id", "The Unique Id for this service within this Beamable CLI context");
	public static readonly Option<string[]> BEAM_SERVICE_OPTION_DEPENDENCIES = new("--deps", "The ','-separated list of existing Beam-O Ids that this service depends on");

	public static readonly Option<string> HTTP_MICROSERVICE_OPTION_LOCAL_BUILD_CONTEXT = new("--local-build-context", "The path to a valid docker build context with a Dockerfile in it");

	public static readonly Option<string> HTTP_MICROSERVICE_OPTION_LOCAL_DOCKERFILE =
		new("--local-dockerfile", "The relative file path, from the given build-context, to a valid Dockerfile inside that context");

	public static readonly Option<LogEventLevel?> HTTP_MICROSERVICE_OPTION_LOCAL_LOG_LEVEL = new("--local-log", $"The log level this service should be deployed locally with");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_HEALTH_ENDPOINT_AND_PORT = new("--local-health-endpoint",
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

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_BIND_MOUNTS = new("--local-custom-bind-mounts",
		"Any number of arguments in the format LOCAL_PATH:IN_CONTAINER_PATH to bind between your machine and the docker container");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_VOLUMES = new("--local-custom-volumes",
		"Any number of arguments in the format VOLUME_NAME:IN_CONTAINER_PATH to create and bind named volumes into the docker container");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_LOCAL_ENV_VARS = new("--local-env-vars",
		"Any number of arguments in the format NAME=VALUE representing environment variables to set into the local container");

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_REMOTE_HEALTH_ENDPOINT;

	public static readonly Option<string[]> HTTP_MICROSERVICE_OPTION_REMOTE_ENV_VARS = new("--remote-env-vars",
		"Any number of arguments in the format 'NAME=VALUE' representing environment variables Beam-O should set on the container it runs in AWS");


	private BeamoLocalSystem _localBeamo;


	public ServicesRegisterCommand() :
		base("register",
			"Registers a new service into the local manifest")
	{
	}

	static ServicesRegisterCommand()
	{
		HTTP_MICROSERVICE_OPTION_REMOTE_HEALTH_ENDPOINT = new Option<string[]>("--remote-health-endpoint",
			"The health check endpoint and port, with no trailing or heading '/', that Beam-O should call on the deployed container to see if application is up.\n" +
			"Example: --local-health-endpoint health 6565");
	}

	public override void Configure()
	{
		AddOption(BEAM_SERVICE_OPTION_ID, (args, i) => args.BeamoId = i);

		AddOption(new Option<BeamoProtocolType?>("--type", () => null, $"The type of protocol this service implements: {string.Join(",", Enum.GetNames(typeof(BeamoProtocolType)))}"),
			(args, i) => args.ProtocolType = i);

		AddOption(BEAM_SERVICE_OPTION_DEPENDENCIES, (args, i) => args.ServiceDependencies = i.Length == 0 ? null : i);

		// For HttpProtocol
		{
			AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_BUILD_CONTEXT, (args, i) => args.HttpSpecificArgs.LocalDockerBuildContext = i);
			AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_DOCKERFILE, (args, i) => args.HttpSpecificArgs.LocalDockerfileRelativePath = i);
			AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_LOG_LEVEL, (args, i) => args.HttpSpecificArgs.LocalLogLevel = i);
			AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_HEALTH_ENDPOINT_AND_PORT, (args, i) => args.HttpSpecificArgs.LocalHealthEndpointAndPort = i.Length == 0 ? null : i);
			AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_HOT_RELOADING, (args, i) => args.HttpSpecificArgs.LocalHotReloadingConfig = i.Length == 0 ? null : i);

			// These all get initialized to string[0] instead of null because they are NOT presented in the interactive mode.
			{
				AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_PORTS, (args, i) => args.HttpSpecificArgs.LocalCustomPorts = i);
				AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_BIND_MOUNTS, (args, i) => args.HttpSpecificArgs.LocalCustomBindMounts = i);
				AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_CUSTOM_VOLUMES, (args, i) => args.HttpSpecificArgs.LocalCustomVolumes = i);
				AddOption(HTTP_MICROSERVICE_OPTION_LOCAL_ENV_VARS, (args, i) => args.HttpSpecificArgs.LocalCustomEnvVars = i);
			}

			AddOption(HTTP_MICROSERVICE_OPTION_REMOTE_HEALTH_ENDPOINT, (args, i) => args.HttpSpecificArgs.RemoteHealthEndpointAndPort = i.Length == 0 ? null : i);
			AddOption(HTTP_MICROSERVICE_OPTION_REMOTE_ENV_VARS, (args, i) => args.HttpSpecificArgs.RemoteCustomEnvVars = i.Length == 0 ? null : i);
		}

		// For EmbeddedMongo Protocol
		{
			AddOption(new Option<string>("--base-image", () => null, "Name and tag of the base image to use for the local mongo db instance"),
				(args, i) => args.BaseImage = i);
		}
	}

	public override async Task Handle(ServicesRegisterCommandArgs args)
	{
		_localBeamo = args.BeamoLocalSystem;

		// Handle Beamo Id Option 
		var existingBeamoIds = _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId).ToList();
		{
			if (string.IsNullOrEmpty(args.BeamoId))
				args.BeamoId = AnsiConsole.Prompt(new TextPrompt<string>("Enter an unique identifier for this [lightskyblue1]Beam-O Container[/]:"));

			if (!BeamoLocalSystem.ValidateBeamoServiceId_ValidCharacters(args.BeamoId))
			{
				AnsiConsole.MarkupLine($"[red]\nBeam-O Ids can only contain alphanumeric and underscore characters.[/]");
				return;
			}

			if (!BeamoLocalSystem.ValidateBeamoServiceId_DoesntExists(args.BeamoId, _localBeamo.BeamoManifest.ServiceDefinitions))
			{
				AnsiConsole.MarkupLine(
					$"[red]\nBeam-O Ids must be unique per-project. Here are the ones already taken: {string.Join(", ", existingBeamoIds)}[/]");
				return;
			}
		}

		// Handle Type Option
		if (args.ProtocolType == null)
			args.ProtocolType = AnsiConsole.Prompt(new SelectionPrompt<BeamoProtocolType>()
				.Title("What [green]Beam-O Protocol Type[/] does the container respect?")
				.AddChoices(BeamoProtocolType.EmbeddedMongoDb, BeamoProtocolType.HttpMicroservice)
				.AddBeamHightlight());

		// Handle Dependencies Option
		if (!EnsureServiceDependencies(existingBeamoIds, ref args.ServiceDependencies))
			return;

		switch (args.ProtocolType)
		{
			case BeamoProtocolType.HttpMicroservice:
			{
				// Get the HTTP Microservice specific args
				var httpArgs = args.HttpSpecificArgs;

				// Handle DockerBuildContext
				if (!EnsureLocalDockerBuildContext(ref httpArgs))
					return;

				// Handle Dockerfile path
				if (!EnsureLocalDockerfilePath(ref httpArgs))
					return;

				// Log Level
				EnsureLocalLogLevel(ref httpArgs);

				// Health Check
				EnsureLocalHealthEndpointAndPort(ref httpArgs);

				// Hot Reloading
				EnsureLocalHotReloadingConfig(ref httpArgs);

				// Remote parameters
				EnsureRemoteHealthEndpointAndPort(ref httpArgs);

				await _localBeamo.AddDefinition_HttpMicroservice(args.BeamoId,
					httpArgs.LocalDockerBuildContext,
					httpArgs.LocalDockerfileRelativePath,
					args.ServiceDependencies,
					CancellationToken.None);

				// Update the created protocol based on the received arguments
				await _localBeamo.TryUpdateLocalProtocol(args.BeamoId, UpdateHttpLocalProtocolFromArgs(httpArgs), CancellationToken.None);
				await _localBeamo.TryUpdateRemoteProtocol(args.BeamoId, UpdateHttpRemoteProtocolFromArgs(httpArgs), CancellationToken.None);

				break;
			}
			case BeamoProtocolType.EmbeddedMongoDb:
			{
				// Handle base image
				{
					if (string.IsNullOrEmpty(args.BaseImage))
						args.BaseImage = AnsiConsole.Prompt(new TextPrompt<string>("Enter the base image name of a [lightskyblue1]Mongo Db[/] image:").DefaultValue("mongo:latest"));

					await _localBeamo.AddDefinition_EmbeddedMongoDb(args.BeamoId, args.BaseImage, args.ServiceDependencies, CancellationToken.None);
				}
				break;
			}
			default:
				throw new ArgumentOutOfRangeException();
		}

		_localBeamo.SaveBeamoLocalManifest();
		_localBeamo.SaveBeamoLocalRuntime();

		await _localBeamo.StopListeningToDocker();
	}

	public static bool EnsureServiceDependencies(List<string> existingBeamoIds, ref string[] serviceDependencies, string[] existingDeps = null)
	{
		existingDeps ??= Array.Empty<string>();

		if (serviceDependencies == null)
		{
			var multiSelectionPrompt = new MultiSelectionPrompt<string>()
				.Title("Service Dependencies")
				.InstructionsText("Select any number of other Beam-O containers as dependencies of this one. We check for cyclical dependencies so don't worry.")
				.AddChoices(existingBeamoIds)
				.AddBeamHightlight()
				.NotRequired();

			foreach (var serviceDependency in existingDeps)
				multiSelectionPrompt.Select(serviceDependency);

			serviceDependencies = AnsiConsole.Prompt(multiSelectionPrompt).ToArray();
		}


		if (serviceDependencies.Length > 0 && !serviceDependencies.All(existingBeamoIds.Contains))
		{
			AnsiConsole.MarkupLine(
				$"[red]\nCannot depend on unregistered service. Here are the services already registered: {string.Join(", ", existingBeamoIds)}[/]");
			return false;
		}

		return true;
	}

	public static bool EnsureLocalDockerBuildContext(ref HttpSpecificArgs httpArgs, string currentBuildContextPath = null)
	{
		if (string.IsNullOrEmpty(httpArgs.LocalDockerBuildContext))
			httpArgs.LocalDockerBuildContext =
				AnsiConsole.Prompt(new TextPrompt<string>("Enter the relative path to a valid [lightskyblue1]Docker Build Context[/]:").DefaultValue(currentBuildContextPath));

		if (!Directory.Exists(httpArgs.LocalDockerBuildContext))
		{
			AnsiConsole.MarkupLine($"[red]\nThe given path does not exist![/]");
			return false;
		}

		return true;
	}

	public static bool EnsureLocalDockerfilePath(ref HttpSpecificArgs httpArgs, string currentDockerfile = null)
	{
		if (string.IsNullOrEmpty(httpArgs.LocalDockerfileRelativePath))
			httpArgs.LocalDockerfileRelativePath =
				AnsiConsole.Prompt(
					new TextPrompt<string>("Enter the [lightskyblue1]Dockerfile[/]'s path (from the given [lightskyblue1]Docker Build Context[/]'s root):").DefaultValue(currentDockerfile));

		var dockerfilePath = Path.Combine(httpArgs.LocalDockerBuildContext, httpArgs.LocalDockerfileRelativePath);
		if (!File.Exists(dockerfilePath))
		{
			AnsiConsole.MarkupLine($"[red]\nNo dockerfile found at path [{dockerfilePath}]![/]");
			return false;
		}

		return true;
	}

	public static void EnsureLocalLogLevel(ref HttpSpecificArgs httpArgs, LogEventLevel? currentLogLevel = null)
	{
		var curr = currentLogLevel.HasValue ? $"(Current: {currentLogLevel.Value.ToString()})" : "";
		httpArgs.LocalLogLevel ??= AnsiConsole.Prompt(new SelectionPrompt<LogEventLevel?>()
			.Title($"Choose the [lightskyblue1]LogLevel[/] {curr}:")
			.AddBeamHightlight()
			.AddChoices(Enum.GetValues<LogEventLevel>().Cast<LogEventLevel?>())
		);
	}

	public static void EnsureLocalHealthEndpointAndPort(ref HttpSpecificArgs httpArgs, string[] currHealthEndpointAndPort = null)
	{
		if (httpArgs.LocalHealthEndpointAndPort == null)
		{
			httpArgs.LocalHealthEndpointAndPort = new string[2];

			var defaults = currHealthEndpointAndPort ?? new string[2] { "health", "6565" };
			httpArgs.LocalHealthEndpointAndPort[0] = AnsiConsole.Prompt(new TextPrompt<string>("Enter the [lightskyblue1]Healthcheck Endpoint[/]:").DefaultValue(defaults[0]));
			httpArgs.LocalHealthEndpointAndPort[1] = AnsiConsole.Prompt(new TextPrompt<string>("Enter the [lightskyblue1]Healthcheck Port[/]:").DefaultValue(defaults[1]));
		}

		if (httpArgs.LocalHealthEndpointAndPort.Length != 2)
			throw new ArgumentOutOfRangeException(nameof(httpArgs.LocalHealthEndpointAndPort), "Must pass two arguments. See 'beam services register --help' for more information.");
	}

	public static void EnsureLocalHotReloadingConfig(ref HttpSpecificArgs httpArgs, string[] currentHotReloading = null)
	{
		if (httpArgs.LocalHotReloadingConfig == null)
		{
			var defaults = currentHotReloading ?? new[] { "", "", "", "" };
			httpArgs.LocalHotReloadingConfig = new string[4];
			httpArgs.LocalHotReloadingConfig[0] =
				AnsiConsole.Prompt(new TextPrompt<string>("(Optional) Enter the [lightskyblue1]\"Hot-Reloading Enabled\" Endpoint[/]:").DefaultValue(defaults[0]));
			httpArgs.LocalHotReloadingConfig[1] =
				AnsiConsole.Prompt(new TextPrompt<string>("(Optional) Enter the [lightskyblue1]\"Hot-Reloading Enabled\" Port[/]:").DefaultValue(defaults[1]));
			httpArgs.LocalHotReloadingConfig[2] =
				AnsiConsole.Prompt(new TextPrompt<string>("(Optional) Enter the [lightskyblue1]\"Source File Path\"[/] required for hot reloading:").DefaultValue(defaults[2]));
			httpArgs.LocalHotReloadingConfig[3] =
				AnsiConsole.Prompt(new TextPrompt<string>("(Optional) Enter the [lightskyblue1]\"In-Container File Path\"[/] the image expects to find the files in:").DefaultValue(defaults[3]));
		}

		if (httpArgs.LocalHotReloadingConfig.Length != 4)
			throw new ArgumentOutOfRangeException(nameof(httpArgs.LocalHotReloadingConfig), "Must pass two arguments. See 'beam services register --help' for more information.");
	}

	public static void EnsureRemoteHealthEndpointAndPort(ref HttpSpecificArgs httpArgs, string[] currHealthEndpointAndPort = null)
	{
		if (httpArgs.RemoteHealthEndpointAndPort == null)
		{
			var defaults = currHealthEndpointAndPort ?? new string[2] { "health", "6565" };
			httpArgs.RemoteHealthEndpointAndPort = new string[2];
			httpArgs.RemoteHealthEndpointAndPort[0] = AnsiConsole.Prompt(new TextPrompt<string>("Enter the [lightskyblue1]Remote Healthcheck Endpoint[/]:").DefaultValue(defaults[0]));
			httpArgs.RemoteHealthEndpointAndPort[1] = AnsiConsole.Prompt(new TextPrompt<string>("Enter the [lightskyblue1]Remote Healthcheck Port[/]:").DefaultValue(defaults[1]));
		}

		if (httpArgs.RemoteHealthEndpointAndPort.Length != 2)
			throw new ArgumentOutOfRangeException(nameof(httpArgs.RemoteHealthEndpointAndPort), "Must pass two arguments. See 'beam services register --help' for more information.");
	}

	public static LocalProtocolModifier<HttpMicroserviceLocalProtocol> UpdateHttpLocalProtocolFromArgs(HttpSpecificArgs httpArgs)
	{
		return (_, localProtocol) =>
		{
			if (httpArgs.LocalDockerBuildContext != null)
				localProtocol.DockerBuildContextPath = httpArgs.LocalDockerBuildContext;

			if (httpArgs.LocalDockerfileRelativePath != null)
				localProtocol.RelativeDockerfilePath = httpArgs.LocalDockerfileRelativePath;

			if (httpArgs.LocalHotReloadingConfig != null)
			{
				localProtocol.HotReloadEnabledEndpoint = httpArgs.LocalHotReloadingConfig[0];
				localProtocol.HotReloadEnabledPort = httpArgs.LocalHotReloadingConfig[1];

				localProtocol.BindSrcForHotReloading = new DockerBindMount()
				{
					IsReadOnly = true,
					LocalPath = httpArgs.LocalHotReloadingConfig[2],
					InContainerPath = httpArgs.LocalHotReloadingConfig[3]
				};
			}

			localProtocol.CustomPortBindings = httpArgs.LocalCustomPorts
				.Select(s => new DockerPortBinding() { LocalPort = s.Split(':')[0], InContainerPort = s.Split(':')[1] }).ToList();
			localProtocol.CustomBindMounts = httpArgs.LocalCustomBindMounts
				.Select(s => new DockerBindMount() { IsReadOnly = true, LocalPath = s.Split(':')[0], InContainerPath = s.Split(':')[1] }).ToList();
			localProtocol.CustomVolumes = httpArgs.LocalCustomVolumes
				.Select(s => new DockerVolume() { VolumeName = s.Split(':')[0], InContainerPath = s.Split(':')[1] }).ToList();
			localProtocol.CustomEnvironmentVariables = httpArgs.LocalCustomEnvVars
				.Select(s => new DockerEnvironmentVariable() { VariableName = s.Split('=')[0], Value = s.Split('=')[1] }).ToList();

			return Task.CompletedTask;
		};
	}

	public static RemoteProtocolModifier<HttpMicroserviceRemoteProtocol> UpdateHttpRemoteProtocolFromArgs(HttpSpecificArgs httpArgs)
	{
		return (_, remoteProtocol) =>
		{
			if (httpArgs.RemoteHealthEndpointAndPort != null)
			{
				remoteProtocol.HealthCheckEndpoint = httpArgs.RemoteHealthEndpointAndPort[0];
				remoteProtocol.HealthCheckPort = httpArgs.RemoteHealthEndpointAndPort[1];
			}

			if (httpArgs.RemoteCustomEnvVars != null)
			{
				remoteProtocol.CustomEnvironmentVariables = httpArgs.RemoteCustomEnvVars
					.Select(s => new DockerEnvironmentVariable() { VariableName = s.Split('=')[0], Value = s.Split('=')[1] }).ToList();
			}

			return Task.CompletedTask;
		};
	}
}
