using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

using static ServicesRegisterCommand;

public class ServicesModifyCommandArgs : LoginCommandArgs
{
	public string BeamoId;
	public string[] ServiceDependencies;

	public bool EnableOnRemoteDeploy;

	// HttpMicroservice args
	public HttpSpecificArgs HttpSpecificArgs;
}

public class ServicesModifyCommand : AppCommand<ServicesModifyCommandArgs>
{

	private BeamoLocalSystem _localBeamo;

	public ServicesModifyCommand() :
		base("modify",
			"Modifies a new service into the local manifest")
	{
	}

	public override void Configure()
	{
		AddOption(BEAM_SERVICE_OPTION_ID, (args, i) => args.BeamoId = i);
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
			// TODO Nothing for now...
		}
	}



	public override async Task Handle(ServicesModifyCommandArgs args)
	{
		_localBeamo = args.BeamoLocalSystem;

		// Handle Beamo Id Option 
		var existingBeamoIds = _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId).ToList();
		{
			if (string.IsNullOrEmpty(args.BeamoId))
				args.BeamoId = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Choose the [lightskyblue1]Beamo-O Service[/] to Modify:")
					.AddChoices(existingBeamoIds)
					.AddBeamHightlight());
		}

		var serviceDefinition = _localBeamo.BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == args.BeamoId);

		// Remove ourselves from the list of beamo ids so we can use this list as the choices for the service dependency things
		existingBeamoIds.Remove(args.BeamoId);
		// Handle Dependencies Option
		var dependencies = await _localBeamo.GetDependencies(args.BeamoId);
		if (!EnsureServiceDependencies(existingBeamoIds, ref args.ServiceDependencies, dependencies.Select(dep => dep.name).ToArray()))
			return;

		// Protocol specific stuff
		switch (serviceDefinition.Protocol)
		{
			case BeamoProtocolType.HttpMicroservice:
			{
				// Get the HTTP Microservice specific args
				var httpArgs = args.HttpSpecificArgs;
				var localProtocol = _localBeamo.BeamoManifest.HttpMicroserviceLocalProtocols[args.BeamoId];
				var remoteProtocol = _localBeamo.BeamoManifest.HttpMicroserviceRemoteProtocols[args.BeamoId];

				// Handle DockerBuildContext
				if (!EnsureLocalDockerBuildContext(ref httpArgs, localProtocol.DockerBuildContextPath))
					return;

				// Handle Dockerfile path
				if (!EnsureLocalDockerfilePath(ref httpArgs, localProtocol.RelativeDockerfilePath))
					return;

				// Hot Reloading
				EnsureLocalHotReloadingConfig(ref httpArgs,
					new[]
					{
						localProtocol.HotReloadEnabledEndpoint, localProtocol.HotReloadEnabledPort, localProtocol.BindSrcForHotReloading.LocalPath,
						localProtocol.BindSrcForHotReloading.InContainerPath
					});

				// Remote parameters
				EnsureRemoteHealthEndpointAndPort(ref httpArgs, new[] { remoteProtocol.HealthCheckEndpoint, remoteProtocol.HealthCheckPort });

				// Update service dependencies
				// TODO HANDLE THAT
				// serviceDefinition.DependsOnBeamoIds = args.ServiceDependencies; 

				// Update the created protocol based on the received arguments
				await _localBeamo.TryUpdateLocalProtocol(args.BeamoId, UpdateHttpLocalProtocolFromArgs(httpArgs), CancellationToken.None);
				await _localBeamo.TryUpdateRemoteProtocol(args.BeamoId, UpdateHttpRemoteProtocolFromArgs(httpArgs), CancellationToken.None);

				break;
			}
			case BeamoProtocolType.EmbeddedMongoDb:
			{
				// Handle base image
				{
					// TODO Nothing for now...
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
}
