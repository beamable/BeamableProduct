using Docker.DotNet.Models;
using Serilog.Events;

namespace cli;

public partial class BeamoLocalService
{
	public async Task<BeamoServiceDefinition> AddHttpMicroserviceDefinition(string beamId, string projectPath, string dockerfilePath, string[] dependencyBeamIds, CancellationToken cancellationToken)
	{
		dependencyBeamIds ??= Array.Empty<string>();
		return await AddServiceDefinition<HttpMicroserviceLocalProtocol, HttpMicroserviceRemoteProtocol>(
			beamId,
			BeamoProtocolType.HttpMicroservice,
			projectPath,
			dockerfilePath,
			"",
			dependencyBeamIds,
			PrepareDefaultLocalProtocol_HttpMicroservice,
			PrepareDefaultRemoteProtocol_HttpMicroservice,
			cancellationToken);
	}

	public async Task<bool> ResetHttpMicroserviceLocalProtocol(string beamoId, CancellationToken cancellationToken) =>
		await TryUpdateLocalProtocol<HttpMicroserviceLocalProtocol>(beamoId, PrepareDefaultLocalProtocol_HttpMicroservice, cancellationToken);

	public async Task<bool> ResetHttpMicroserviceRemoteProtocol(string beamoId, CancellationToken cancellationToken) =>
		await TryUpdateRemoteProtocol<HttpMicroserviceRemoteProtocol>(beamoId, PrepareDefaultRemoteProtocol_HttpMicroservice, cancellationToken);

	public async Task BuildHttpMicroservice(BeamoServiceDefinition cont, Action<JSONMessage> buildProgress)
	{
		await BuildAndCreateImage(cont.BeamoId, cont.DockerBuildContextPath, cont.RelativeDockerfilePath, buildProgress);
	}

	public async Task RunLocalHttpMicroservice(BeamoServiceDefinition serviceDefinition)
	{
		const string ENV_CID = "CID";
		const string ENV_PID = "PID";
		const string ENV_SECRET = "SECRET";
		const string ENV_HOST = "HOST";
		const string ENV_LOG_LEVEL = "LOG_LEVEL";
		const string ENV_NAME_PREFIX = "NAME_PREFIX";
		const string ENV_WATCH_TOKEN = "WATCH_TOKEN";

		var imageId = serviceDefinition.ImageId;
		var containerName = $"{serviceDefinition.BeamoId}_httpMicroservice";

		var localProtocol = serviceDefinition.LocalProtocol as HttpMicroserviceLocalProtocol;

		var portBindings = new List<DockerPortBinding>();
		portBindings.AddRange(localProtocol.CustomPortBindings);

		var volumes = new List<DockerVolume>();
		volumes.AddRange(localProtocol.CustomVolumes);

		var bindMounts = new List<DockerBindMount>();
		
		var shouldPrepareWatch = !string.IsNullOrEmpty(localProtocol.BindSrcForHotReloading.LocalPath);
		if (shouldPrepareWatch)
			bindMounts.Add(localProtocol.BindSrcForHotReloading);

		bindMounts.AddRange(localProtocol.CustomBindMounts);

		var environmentVariables = new List<DockerEnvironmentVariable>()
		{
			new() { VariableName = ENV_CID, Value = localProtocol.CID },
			new() { VariableName = ENV_PID, Value = localProtocol.PID },
			new() { VariableName = ENV_SECRET, Value = localProtocol.RealmSecret },
			new() { VariableName = ENV_HOST, Value = localProtocol.WebSocketHost },
			new() { VariableName = ENV_LOG_LEVEL, Value = localProtocol.LogLevel },
			new() { VariableName = ENV_NAME_PREFIX, Value = localProtocol.Prefix },
			new() { VariableName = ENV_WATCH_TOKEN, Value = shouldPrepareWatch.ToString() },
		};
		environmentVariables.AddRange(localProtocol.CustomEnvironmentVariables);

		var healthConfig = new DockerHealthConfig()
		{
			StopContainerWhenUnhealthy = true,
			AutoRemoveContainerWhenStopped = true, 
			InContainerEndpoint = localProtocol.HealthCheckEndpoint,
			InContainerPort = localProtocol.HealthCheckInternalPort,
			NumberOfRetries = 5,
			HealthRequestTimeout = 1,
			MaximumSecondsBetweenRetries = 3,
		};

		await CreateAndRunContainer(imageId, containerName, healthConfig, portBindings, volumes, bindMounts, environmentVariables);
	}


	private async Task PrepareDefaultRemoteProtocol_HttpMicroservice(BeamoServiceDefinition owner, HttpMicroserviceRemoteProtocol remote)
	{
		remote.HealthCheckEndpoint = "admin/health";
		remote.HealthCheckPort = "6565";
		remote.CustomEnvironmentVariables = new List<DockerEnvironmentVariable>();

		await Task.CompletedTask;
	}

	private async Task PrepareDefaultLocalProtocol_HttpMicroservice(BeamoServiceDefinition owner, HttpMicroserviceLocalProtocol local)
	{
		// TODO: Move this out of here and into another service
		var secret = await GetRealmSecret();

		local.CID = _ctx.Cid;
		local.PID = _ctx.Pid;
		local.RealmSecret = secret;
		local.WebSocketHost =  $"{_ctx.Host.Replace("http://", "wss://").Replace("https://", "wss://")}/socket";
		local.Prefix = Environment.MachineName;
		local.LogLevel = _ctx.LogLevel.ToString();
		
		local.HealthCheckEndpoint = "health";
		local.HealthCheckInternalPort = "6565";

		local.CustomPortBindings = new List<DockerPortBinding>();
		local.CustomVolumes = new List<DockerVolume>();
		local.CustomBindMounts = new List<DockerBindMount>();
		local.CustomEnvironmentVariables = new List<DockerEnvironmentVariable>();
	}
}

public class HttpMicroserviceRemoteProtocol : IBeamoRemoteProtocol
{
	// For when we support people exposing what the health check endpoint is
	public string HealthCheckEndpoint;
	public string HealthCheckPort;
	public List<DockerEnvironmentVariable> CustomEnvironmentVariables;
}

public class HttpMicroserviceLocalProtocol : IBeamoLocalProtocol
{
	public string CID;
	public string PID;

	/// <summary>
	/// TODO: We should add secret storage/resolution to Beam-O...
	/// </summary>
	public string RealmSecret;

	/// <summary>
	/// TODO: Discuss with Drazen how to step out of this problem by leveraging the Http Service Discovery thing.
	/// </summary>
	public string WebSocketHost;

	public string LogLevel;
	public string Prefix;

	public string HealthCheckEndpoint;
	public string HealthCheckInternalPort;

	public DockerBindMount BindSrcForHotReloading;
	public string HotReloadSupportedEndpoint;
	public string HotReloadEnabledEndpoint;

	public List<DockerPortBinding> CustomPortBindings;
	public List<DockerBindMount> CustomBindMounts;
	public List<DockerVolume> CustomVolumes;
	public List<DockerEnvironmentVariable> CustomEnvironmentVariables;
}

// TODO: DOCKer COMPOSE INTO VISION DOC????
