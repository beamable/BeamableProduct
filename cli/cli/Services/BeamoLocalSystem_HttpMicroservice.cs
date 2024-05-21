/**
 * This part of the class defines how we manage Beamo Services that have the protocol: HttpMicroservices.
 * It handles default values, how to start the container with its data and other utility functions around this protocol.
 */

using Beamable.Common;
using cli.Utils;
using Docker.DotNet.Models;
using Serilog;
using System.Text.RegularExpressions;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	public string GetBeamIdAsMicroserviceContainer(string beamoId) => $"{beamoId}_httpMicroservice";

	private const string HTTM_MICROSERVICE_CONTAINER_PORT = "6565";
	
	public async Task<List<DockerEnvironmentVariable>> GetLocalConnectionStrings(BeamoLocalManifest localManifest,
		string host = "gateway.docker.internal")
	{
		var output = new List<DockerEnvironmentVariable>();
		foreach (var local in localManifest.EmbeddedMongoDbLocalProtocols)
		{
			var environmentVariable = await GetLocalConnectionString(localManifest, local.Key, host);
			output.Add(environmentVariable);
		}

		return output;
	}

	public async Promise<string> GetMicroserviceHostPort(string serviceName)
	{
		var localMicroserviceName = GetBeamIdAsMicroserviceContainer(serviceName);

		ContainerInspectResponse storageDesc = await _client.Containers.InspectContainerAsync(localMicroserviceName);

		if (!storageDesc.NetworkSettings.Ports.TryGetValue($"{HTTM_MICROSERVICE_CONTAINER_PORT}/tcp", out IList<PortBinding> bindings))
		{
			throw new Exception(
				$"could not get host port of microservice=[{serviceName}] because it was not mapped in container");
		}

		return bindings[0].HostPort;
	}

	public async Promise<string> GetStorageHostPort(string storageName)
	{
		var localStorageContainerName = GetBeamIdAsMongoContainer(storageName);

		ContainerInspectResponse storageDesc = await _client.Containers.InspectContainerAsync(localStorageContainerName);
		
		if (!storageDesc.NetworkSettings.Ports.TryGetValue($"{MONGO_DATA_CONTAINER_PORT}/tcp", out IList<PortBinding> bindings))
		{
			throw new Exception(
				$"could not get host port of storage=[{storageName}] because it was not mapped in storage container");
		}
		
		return bindings[0].HostPort;
	}

	public async Task<DockerEnvironmentVariable> GetLocalConnectionString(BeamoLocalManifest localManifest,
		string storageName, string host = "gateway.docker.internal")
	{
		if (!localManifest.EmbeddedMongoDbLocalProtocols.TryGetValue(storageName, out var localStorage))
		{
			throw new Exception($"Could not find entry for {storageName}");
		}
		
		var hostPort = await GetStorageHostPort(storageName);

		var str = $"mongodb://{localStorage.RootUsername}:{localStorage.RootPassword}@{host}:{hostPort}";
		var key = $"STORAGE_CONNSTR_{storageName}";

		return new DockerEnvironmentVariable { VariableName = key, Value = str };
	}

	/// <summary>
	/// Runs a service locally, enforcing the <see cref="BeamoProtocolType.HttpMicroservice"/> protocol.
	/// </summary>
	public async Task RunLocalHttpMicroservice(BeamoServiceDefinition serviceDefinition,
		HttpMicroserviceLocalProtocol localProtocol, BeamoLocalSystem localSystem)
	{
		const string ENV_CID = "CID";
		const string ENV_PID = "PID";
		const string ENV_SECRET = "SECRET";
		const string ENV_HOST = "HOST";
		const string ENV_LOG_LEVEL = "LOG_LEVEL";
		const string ENV_NAME_PREFIX = "NAME_PREFIX";
		const string ENV_WATCH_TOKEN = "WATCH_TOKEN";
		const string ENV_INSTANCE_COUNT = "BEAM_INSTANCE_COUNT";


		var imageId = serviceDefinition.ImageId;
		var containerName = GetBeamIdAsMicroserviceContainer(serviceDefinition.BeamoId);

		var portBindings = new List<DockerPortBinding>();
		portBindings.AddRange(localProtocol.CustomPortBindings);

		var volumes = new List<DockerVolume>();
		volumes.AddRange(localProtocol.CustomVolumes);

		var bindMounts = new List<DockerBindMount>();

		var shouldPrepareWatch = !string.IsNullOrEmpty(localProtocol.BindSrcForHotReloading.LocalPath);
		if (shouldPrepareWatch)
			bindMounts.Add(localProtocol.BindSrcForHotReloading);

		bindMounts.AddRange(localProtocol.CustomBindMounts);

		// TODO: Move this out of here and into another service then get the cached value here.
		var secret = await _beamo.GetRealmSecret();

		var environmentVariables = new List<DockerEnvironmentVariable>()
		{
			new() { VariableName = ENV_CID, Value = _ctx.Cid },
			new() { VariableName = ENV_PID, Value = _ctx.Pid },
			new() { VariableName = ENV_SECRET, Value = secret },
			new()
			{
				VariableName = ENV_HOST,
				Value = $"{_ctx.Host.Replace("http://", "wss://").Replace("https://", "wss://")}/socket"
			},
			new() { VariableName = ENV_LOG_LEVEL, Value = _ctx.LogLevel.ToString() },
			new() { VariableName = ENV_NAME_PREFIX, Value = MachineHelper.GetUniqueDeviceId() },
			new() { VariableName = ENV_WATCH_TOKEN, Value = shouldPrepareWatch.ToString() },
			new() { VariableName = ENV_INSTANCE_COUNT, Value = localProtocol.InstanceCount.ToString() },
		};
		Log.Information("Building Env Vars.. {host} {prefix} {cid} {pid}",
			(object)$"{_ctx.Host.Replace("http://", "wss://").Replace("https://", "wss://")}/socket",
			(object)MachineHelper.GetUniqueDeviceId(),
			(object)_ctx.Cid, (object)_ctx.Pid);


		// add in connection string environment vars for mongo storage dependencies
		var dependencies = localSystem.GetDependencies(serviceDefinition.BeamoId);
		foreach (var dependencyId in dependencies)
		{
			try
			{
				var connectionEnvVar = await GetLocalConnectionString(localSystem.BeamoManifest, dependencyId.name);
				environmentVariables.Add(connectionEnvVar);
			}
			catch (Exception ex)
			{
				BeamableLogger.LogException(ex);
				continue;
			}
		}


		environmentVariables.AddRange(localProtocol.CustomEnvironmentVariables);

		// Configures docker's own health check command to target our application's configured health check endpoint.
		// It'll try these amount of times with a linear backoff (it's docker's default). If it ever fails, it'll terminate the application.
		var reqTimeout = 1;
		var waitRetryMax = 3;
		var tries = 5;
		var port = 6565;
		var endpoint = "health";
		var pipeCmd = "kill";
		var cmdStr =
			$"wget -O- -q --timeout={reqTimeout} --waitretry={waitRetryMax} --tries={tries} http://localhost:{port}/{endpoint} || {pipeCmd} 1";

		// Creates and runs the container. This container will auto destroy when it stops.
		// TODO: Make the auto destruction optional to help CS identify issues in the wild.
		await CreateAndRunContainer(imageId, containerName, cmdStr, false, portBindings, volumes, bindMounts,
			environmentVariables);
	}
	
}

public class HttpMicroserviceRemoteProtocol : IBeamoRemoteProtocol
{
}

public class HttpMicroserviceLocalProtocol : IBeamoLocalProtocol
{
	/// <summary>
	/// This is for local and development things
	/// </summary>
	public string DockerBuildContextPath; // TODO: we should delete this, because the build context is now ALWAYS known as the parent of the .beamable folder.

	/// <summary>
	/// This is for local and development things
	/// </summary>
	public string RelativeDockerfilePath;

	public DockerBindMount BindSrcForHotReloading;
	public string HotReloadEnabledEndpoint;
	public string HotReloadEnabledPort;

	public List<DockerPortBinding> CustomPortBindings;
	public List<DockerBindMount> CustomBindMounts;
	public List<DockerVolume> CustomVolumes;
	public List<DockerEnvironmentVariable> CustomEnvironmentVariables;

	public int InstanceCount = 1;
	
	/// <summary>
	/// A list of beamo ids for dependencies on storage projects
	/// </summary>
	public List<string> StorageDependencyBeamIds = new List<string> { };

	/// <summary>
	/// A list of beamo ids for dependencies on storage projects
	/// </summary>
	public List<string> GeneralDependencyProjectPaths = new List<string> { };



	public bool VerifyCanBeBuiltLocally(ConfigService configService)
	{
		var hasPaths = !string.IsNullOrEmpty(DockerBuildContextPath) && !string.IsNullOrEmpty(RelativeDockerfilePath);
		if (!hasPaths)
		{
			return false;
		}

		var path = configService.BeamableRelativeToExecutionRelative(DockerBuildContextPath);
		if (!Directory.Exists(path))
			throw new Exception($"DockerBuildContext doesn't exist: [{path}]");

		var dockerfilePath = Path.Combine(path, RelativeDockerfilePath);
		if (!File.Exists(dockerfilePath))
			throw new Exception($"No Dockerfile found at path: [{dockerfilePath}]");

		return true;
	}
}
