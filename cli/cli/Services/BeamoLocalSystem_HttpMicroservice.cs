/**
 * This part of the class defines how we manage Beamo Services that have the protocol: HttpMicroservices.
 * It handles default values, how to start the container with its data and other utility functions around this protocol. 
 */

namespace cli;

public partial class BeamoLocalSystem
{
	public async Task<BeamoServiceDefinition> AddDefinition_HttpMicroservice(string beamId, string projectPath, string dockerfilePath, string[] dependencyBeamIds, CancellationToken cancellationToken)
	{
		dependencyBeamIds ??= Array.Empty<string>();
		return await AddServiceDefinition<HttpMicroserviceLocalProtocol, HttpMicroserviceRemoteProtocol>(
			beamId,
			BeamoProtocolType.HttpMicroservice,
			dependencyBeamIds,
			async (definition, protocol) =>
			{
				await PrepareDefaultLocalProtocol_HttpMicroservice(definition, protocol);
				protocol.DockerBuildContextPath = projectPath;
				protocol.RelativeDockerfilePath = dockerfilePath;
			},
			PrepareDefaultRemoteProtocol_HttpMicroservice,
			cancellationToken);
	}

	public async Task<bool> ResetLocalProtocol_HttpMicroservice(string beamoId, CancellationToken cancellationToken) =>
		await TryUpdateLocalProtocol<HttpMicroserviceLocalProtocol>(beamoId, PrepareDefaultLocalProtocol_HttpMicroservice, cancellationToken);

	public async Task<bool> ResetRemoteProtocol_HttpMicroservice(string beamoId, CancellationToken cancellationToken) =>
		await TryUpdateRemoteProtocol<HttpMicroserviceRemoteProtocol>(beamoId, PrepareDefaultRemoteProtocol_HttpMicroservice, cancellationToken);

	public async Task RunLocalHttpMicroservice(BeamoServiceDefinition serviceDefinition, HttpMicroserviceLocalProtocol localProtocol)
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

		var reqTimeout = 1;
		var waitRetryMax = 3;
		var tries = 5;
		var port = localProtocol.HealthCheckInternalPort;
		var endpoint = localProtocol.HealthCheckEndpoint;
		var pipeCmd = "kill";
		var cmdStr = $"wget -O- -q --timeout={reqTimeout} --waitretry={waitRetryMax} --tries={tries} http://localhost:{port}/{endpoint} || {pipeCmd} 1";

		await CreateAndRunContainer(imageId, containerName, cmdStr, true, portBindings, volumes, bindMounts, environmentVariables);
	}


	private async Task PrepareDefaultRemoteProtocol_HttpMicroservice(BeamoServiceDefinition owner, HttpMicroserviceRemoteProtocol remote)
	{
		remote.HealthCheckEndpoint = "health";
		remote.HealthCheckPort = "6565";
		remote.CustomEnvironmentVariables = new List<DockerEnvironmentVariable>();

		await Task.CompletedTask;
	}

	private async Task PrepareDefaultLocalProtocol_HttpMicroservice(BeamoServiceDefinition owner, HttpMicroserviceLocalProtocol local)
	{
		// TODO: Move this out of here and into another service
		var secret = await _beamo.GetRealmSecret();

		local.CID = _ctx.Cid;
		local.PID = _ctx.Pid;
		local.RealmSecret = secret;
		local.WebSocketHost = $"{_ctx.Host.Replace("http://", "wss://").Replace("https://", "wss://")}/socket";
		local.Prefix = Environment.MachineName;
		local.LogLevel = _ctx.LogLevel.ToString();

		local.HealthCheckEndpoint = "health";
		local.HealthCheckInternalPort = "6565";

		local.CustomPortBindings = new List<DockerPortBinding>();
		local.CustomVolumes = new List<DockerVolume>();
		local.CustomBindMounts = new List<DockerBindMount>();
		local.CustomEnvironmentVariables = new List<DockerEnvironmentVariable>();
	}

	/// <summary>
	/// Resets the protocol data for the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> to the default settings. 
	/// </summary>
	public async Task<bool> ResetToDefaultValues_HttpMicroservice(string beamoId)
	{
		var localUpdated = await TryUpdateLocalProtocol<HttpMicroserviceLocalProtocol>(beamoId, PrepareDefaultLocalProtocol_HttpMicroservice, CancellationToken.None);
		var remoteUpdated = await TryUpdateRemoteProtocol<HttpMicroserviceRemoteProtocol>(beamoId, PrepareDefaultRemoteProtocol_HttpMicroservice, CancellationToken.None);
		return localUpdated && remoteUpdated;
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
	/// <summary>
	/// This is for local and development things
	/// </summary>
	public string DockerBuildContextPath;

	/// <summary>
	/// This is for local and development things
	/// </summary>
	public string RelativeDockerfilePath;


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
	public string HotReloadEnabledEndpoint;
	public string HotReloadEnabledPort;
	
	public List<DockerPortBinding> CustomPortBindings;
	public List<DockerBindMount> CustomBindMounts;
	public List<DockerVolume> CustomVolumes;
	public List<DockerEnvironmentVariable> CustomEnvironmentVariables;

	public bool VerifyCanBeBuiltLocally()
	{
		var hasPaths = !string.IsNullOrEmpty(DockerBuildContextPath) && !string.IsNullOrEmpty(RelativeDockerfilePath);
		if (hasPaths)
		{
			if (!Directory.Exists(DockerBuildContextPath))
				throw new Exception($"DockerBuildContext doesn't exist: [{DockerBuildContextPath}]");

			var dockerfilePath = Path.Combine(DockerBuildContextPath, RelativeDockerfilePath);
			if (!File.Exists(dockerfilePath))
				throw new Exception($"No Dockerfile found at path: [{dockerfilePath}]");
		}
		return hasPaths;
	}
}

// TODO: DOCKer COMPOSE INTO VISION DOC????
