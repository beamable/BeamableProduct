/**
 * This part of the class defines how we manage Beamo Services that have the protocol: EmbeddedMongoDb.
 * It handles default values, how to start the container and other utility functions around this protocol.
 * TODO: Always run the mongo-express data-explorer tool as part of the local deployment protocol. 
 */

using Beamable.Server;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	public static string GetBeamIdAsMongoContainer(string beamoId) => $"{beamoId}_mongoDb";

	public static string GetDataVolumeName(string beamoId) => $"{beamoId}_data";
	public static string GetFilesVolumeName(string beamoId) => $"{beamoId}_files";
	
	public const string MONGO_DATA_CONTAINER_PORT = "27017";

	/// <summary>
	/// Prefix of the environment variables `beam project run` hands to each service it launches, one per storage the
	/// service depends on, carrying that storage's already-resolved local connection string. They flow down to the
	/// service's `generate-env` child process, which uses them instead of looking the storage up in Docker (or deploying it).
	/// </summary>
	public const string ENV_PRERESOLVED_STORAGE_CONNSTR_PREFIX = "BEAM_LOCAL_STORAGE_CONNSTR_";

	public static string GetStorageConnectionStringVarName(string storageName) => $"STORAGE_CONNSTR_{storageName}";

	public static string BuildLocalStorageConnectionString(EmbeddedMongoDbLocalProtocol localProtocol, string host, string hostPort) =>
		$"mongodb://{localProtocol.RootUsername}:{localProtocol.RootPassword}@{host}:{hostPort}";

	/// <summary>
	/// Runs a service locally, enforcing the <see cref="BeamoProtocolType.EmbeddedMongoDb"/> protocol.
	/// </summary>
	public async Task RunLocalEmbeddedMongoDb(BeamoServiceDefinition serviceDefinition, EmbeddedMongoDbLocalProtocol localProtocol, Action onFinish = null)
	{
		try
		{
			await CreateAndRunEmbeddedMongoContainer(serviceDefinition, localProtocol);
			onFinish?.Invoke();
		}
		catch (Exception e)
		{
			Log.Error("An error occured while deploying service: " + serviceDefinition.BeamoId);
			Log.Error(e.Message);
		}
	}

	/// <summary>
	/// Creates and runs the storage's container, letting any failure propagate to the caller.
	/// </summary>
	private Task<bool> CreateAndRunEmbeddedMongoContainer(BeamoServiceDefinition serviceDefinition, EmbeddedMongoDbLocalProtocol localProtocol, CancellationToken token = default)
	{
		const string ENV_MONGO_ROOT_USERNAME = "MONGO_INITDB_ROOT_USERNAME";
		const string ENV_MONGO_ROOT_PASSWORD = "MONGO_INITDB_ROOT_PASSWORD";
		var imageId = localProtocol.BaseImage;
		var containerName = GetBeamIdAsMongoContainer(serviceDefinition.BeamoId);

		var portBindings = new List<DockerPortBinding>();
		if (!string.IsNullOrEmpty(localProtocol.MongoLocalPort))
		{
			portBindings.Add(new DockerPortBinding() { LocalPort = localProtocol.MongoLocalPort, InContainerPort = MONGO_DATA_CONTAINER_PORT });
		}

		var volumes = new List<DockerVolume>();
		volumes.Add(new DockerVolume { VolumeName = localProtocol.DataVolumeName, InContainerPath = localProtocol.DataVolumeInContainerPath });
		volumes.Add(new DockerVolume { VolumeName = localProtocol.FilesVolumeName, InContainerPath = localProtocol.FilesVolumeInContainerPath });

		var bindMounts = new List<DockerBindMount>();

		var environmentVariables = new List<DockerEnvironmentVariable>()
		{
			new() { VariableName = ENV_MONGO_ROOT_USERNAME, Value = localProtocol.RootUsername }, new() { VariableName = ENV_MONGO_ROOT_PASSWORD, Value = localProtocol.RootPassword },
		};

		// Configures the default mongo image's health check.
		var cmdStr = $"--interval=5s --timeout=3s CMD /etc/init.d/mongodb status || exit 1";

		// Creates and runs the container. This container will auto destroy when it stops.
		// TODO: Make the auto destruction optional to help CS identify issues in the wild.
		return CreateAndRunContainer(imageId, containerName, cmdStr, true, portBindings, volumes, bindMounts,
			environmentVariables, token);
	}

	/// <summary>
	/// Gets the local Docker state of each of the given storages with a single container-list call, using Docker as the
	/// source of truth (rather than <see cref="BeamoLocalRuntime.ExistingLocalServiceInstances"/>, which can lag behind).
	/// </summary>
	public async Task<Dictionary<string, LocalStorageState>> GetLocalStorageStates(IEnumerable<string> storageIds)
	{
		var ids = storageIds.Distinct().ToList();
		if (ids.Count == 0)
		{
			return new Dictionary<string, LocalStorageState>();
		}

		// Docker ORs multiple values of the same filter key, so one request covers every storage.
		var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
		{
			All = true,
			Filters = new Dictionary<string, IDictionary<string, bool>>
			{
				["name"] = ids.ToDictionary(GetBeamIdAsMongoContainer, _ => true)
			}
		});
		return MatchStorageContainers(containers, ids);
	}

	/// <summary>
	/// Maps a Docker container listing onto the given storages. A storage is <see cref="LocalStorageStatus.Running"/>
	/// when its container is in the "running" state, <see cref="LocalStorageStatus.Stopped"/> when the container exists
	/// in any other state, and <see cref="LocalStorageStatus.Missing"/> when there is no container for it.
	/// </summary>
	public static Dictionary<string, LocalStorageState> MatchStorageContainers(IEnumerable<ContainerListResponse> containers, IEnumerable<string> storageIds)
	{
		var containerList = containers?.ToList() ?? new List<ContainerListResponse>();
		var states = new Dictionary<string, LocalStorageState>();
		foreach (var storageId in storageIds.Distinct())
		{
			// The Docker name filter matches substrings, so match the exact name (container names are reported
			// with a leading '/') to avoid picking up e.g. "NCStorage2_mongoDb" for "NCStorage".
			var expectedName = "/" + GetBeamIdAsMongoContainer(storageId);
			var container = containerList.FirstOrDefault(c => c.Names != null && c.Names.Any(n => n == expectedName));
			if (container == null)
			{
				states[storageId] = new LocalStorageState { StorageId = storageId, Status = LocalStorageStatus.Missing };
				continue;
			}

			var isRunning = string.Equals(container.State, "running", StringComparison.OrdinalIgnoreCase);
			var hostPort = container.Ports?
				.FirstOrDefault(p => p.PrivatePort.ToString() == MONGO_DATA_CONTAINER_PORT && p.PublicPort != 0)?
				.PublicPort.ToString();
			states[storageId] = new LocalStorageState
			{
				StorageId = storageId,
				Status = isRunning ? LocalStorageStatus.Running : LocalStorageStatus.Stopped,
				ContainerId = container.ID,
				HostPort = hostPort
			};
		}

		return states;
	}

	/// <summary>
	/// Makes sure every given storage has a running local container and resolves its connection string, in parallel
	/// (one task per storage). Storages that are already running are left untouched; the others are pulled, created and
	/// started. A storage is considered ready under the same rules the `generate-env --auto-deploy` flow uses: its
	/// container started, and its mongo port is mapped to a host port. Failures are reported per storage instead of thrown.
	/// </summary>
	/// <param name="onUpdate">Invoked with (storageId, message, progress) as each storage advances.</param>
	public async Task<Dictionary<string, LocalStorageResolution>> EnsureLocalStorages(IEnumerable<string> storageIds,
		string host = "localhost", Action<string, string, float> onUpdate = null, CancellationToken token = default)
	{
		var ids = storageIds.Distinct().ToList();
		var states = await GetLocalStorageStates(ids);

		// We just read the real Docker state, so drop any cached instances for the containers we are about to (re)create;
		// otherwise a stale entry could make CreateAndRunContainer think a container that is gone is still running.
		var toStart = ids.Where(id => states[id].Status != LocalStorageStatus.Running).ToList();
		foreach (var id in toStart)
		{
			var containerName = GetBeamIdAsMongoContainer(id);
			BeamoRuntime.ExistingLocalServiceInstances.RemoveAll(si => si.ContainerName == containerName);
		}

		var results = new ConcurrentDictionary<string, LocalStorageResolution>();
		await Task.WhenAll(ids.Select(async id =>
		{
			try
			{
				if (!BeamoManifest.EmbeddedMongoDbLocalProtocols.TryGetValue(id, out var localProtocol))
				{
					throw new CliException($"Could not find a local storage definition for storage=[{id}]");
				}

				var state = states[id];
				var wasStarted = false;
				var hostPort = state.HostPort;
				if (state.Status == LocalStorageStatus.Running)
				{
					Log.Trace($"storage=[{id}] is already running container=[{state.ContainerId}]");
					if (string.IsNullOrEmpty(hostPort))
					{
						// Same lookup (and failure) as the generate-env flow when the port is not mapped.
						hostPort = await GetStorageHostPort(id);
					}
				}
				else
				{
					Log.Trace($"storage=[{id}] is {state.Status.ToString().ToLowerInvariant()}; starting it");
					onUpdate?.Invoke(id, "starting storage...", .1f);
					var serviceDefinition = BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == id);
					await PrepareBeamoServiceImage(serviceDefinition,
						(_, progress) => onUpdate?.Invoke(id, "pulling storage image...", .1f + progress * .6f), token: token);
					var didRun = await CreateAndRunEmbeddedMongoContainer(serviceDefinition, localProtocol, token);
					if (!didRun)
					{
						throw new CliException($"Docker did not start the container for storage=[{id}]");
					}

					hostPort = await GetStorageHostPort(id);
					wasStarted = true;
				}

				results[id] = new LocalStorageResolution
				{
					StorageId = id,
					WasStarted = wasStarted,
					ConnectionString = BuildLocalStorageConnectionString(localProtocol, host, hostPort)
				};
				onUpdate?.Invoke(id, wasStarted ? "storage started" : "storage already running", 1f);
			}
			catch (Exception ex)
			{
				Log.Trace($"failed to ensure storage=[{id}] is running: {ex}");
				results[id] = new LocalStorageResolution { StorageId = id, Error = ex };
			}
		}));

		return new Dictionary<string, LocalStorageResolution>(results);
	}
}

public enum LocalStorageStatus
{
	Missing,
	Stopped,
	Running
}

public class LocalStorageState
{
	public string StorageId;
	public LocalStorageStatus Status;
	public string ContainerId;

	/// <summary>
	/// The host port the container's mongo port is mapped to, or null when it is not mapped (or the container is not running).
	/// </summary>
	public string HostPort;
}

public class LocalStorageResolution
{
	public string StorageId;
	public bool WasStarted;

	/// <summary>
	/// The storage's local connection string; null when <see cref="Error"/> is set.
	/// </summary>
	public string ConnectionString;

	public Exception Error;
	public bool Succeeded => Error == null;
}

[Serializable]
public class EmbeddedMongoDbLocalProtocol : IBeamoLocalProtocol
{
	public string BaseImage;

	public string RootUsername;
	public string RootPassword;

	public string MongoLocalPort;

	public string DataVolumeName;
	public string FilesVolumeName;
	
	public string DataVolumeInContainerPath;
	public string FilesVolumeInContainerPath;
	
	[System.Text.Json.Serialization.JsonIgnore]
	[JsonIgnore]
	public CsharpProjectMetadata Metadata;
	
	/// <summary>
	/// A list of beamo ids for dependencies on storage projects
	/// </summary>
	public List<string> GeneralDependencyProjectPaths = new List<string> { };

	/// <summary>
	/// A list of paths to Unity Assembly Definition project paths.
	/// These projects are auto generated by the Beamable Unity SDK when an assembly
	/// definition is referenced through Project Settings.
	/// </summary>
	public List<UnityAssemblyReferenceData> UnityAssemblyDefinitionProjectReferences = new List<UnityAssemblyReferenceData>();


	public bool VerifyCanBeBuiltLocally(ConfigService _)
	{
		if (!BaseImage.Contains("mongo:"))
			throw new Exception($"Base Image [{BaseImage}] must be a version of mongo.");

		return !string.IsNullOrWhiteSpace(BaseImage);
	}
}

public class EmbeddedMongoDbRemoteProtocol : IBeamoRemoteProtocol
{
}
