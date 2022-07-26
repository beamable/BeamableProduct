using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace cli;

public partial class BeamoLocalSystem
{
	private static readonly Regex BeamoServiceIdRegex = new Regex("^[a-zA-Z0-9_]+$");

	/// <summary>
	/// A local manifest instance that we keep in sync with the <see cref="_beamoLocalManifestFile"/> json file. 
	/// </summary>
	public readonly BeamoLocalManifest BeamoManifest;

	/// <summary>
	/// The full-path to where we are storing the <see cref="BeamoManifest"/>.
	/// TODO: This part will get abstracted out --- probably into <see cref="ConfigService"/> --- so that we can move this to the Beamable.Common library or some shared space.  
	/// </summary>
	private readonly string _beamoLocalManifestFile;

	/// <summary>
	/// The current local state of containers, associated with the <see cref="BeamoLocalManifest.ServiceDefinitions"/>, keept in sync with the <see cref="_beamoLocalRuntimeFile"/> json file. 
	/// </summary>
	public readonly BeamoLocalRuntime BeamoRuntime;

	/// <summary>
	/// The full-path to where we are storing the <see cref="BeamoRuntime"/> data. We need to serialize runtime data as, in most cases, we'll need to survive domain reloads or multiple runs of the cli.
	/// TODO: This part will get abstracted out --- probably into <see cref="ConfigService"/> --- so that we can move this to the Beamable.Common library or some shared space.  
	/// </summary>
	private readonly string _beamoLocalRuntimeFile;


	/// <summary>
	/// The context for this service's execution. Holds the current <see cref="IAppContext.Pid"/>, <see cref="IAppContext.Cid"/> and <see cref="IAppContext.Token"/>
	/// which we use make custom requests to Beam-O's upload flow among other things.
	/// </summary>
	private readonly IAppContext _ctx;

	/// <summary>
	/// The current support API for talking to Beam-O.
	/// </summary>
	private readonly BeamoService _beamo;

	/// <summary>
	/// The realm service so we can grab the production realm's PID from whichever realm we are in.
	/// We do this so that Beam-O shares Docker images across realms to save space.
	/// </summary>
	private IRealmsApi _realmService;

	/// <summary>
	/// An instance of the docker client so that it can communicate with Docker for Windows and Docker for Mac ---- really, it should talk to any docker daemon.
	/// </summary>
	private readonly DockerClient _client;

	/// <summary>
	/// <see cref="StartListeningToDocker"/> and <see cref="StopListeningToDocker"/>.
	/// </summary>
	private Task _dockerListeningThread;

	/// <summary>
	/// <see cref="StartListeningToDocker"/> and <see cref="StopListeningToDocker"/>.
	/// </summary>
	private readonly CancellationTokenSource _dockerListeningThreadCancel;

	/**
	 * TODO: Make sense of this and move it into C#MS Vision Doc
	 *
	 * - Route at C#MS
  - Route that returns "where to find each microfront for that C#MS"
  - Server side rendered front-end or not?
    - public async Task<string> CallToSomewhere();
  - External Data


- Javascript SPA:
  - How does portal know how to talk to the microfront-end in localhost?
  - in Unity, prepare project files
  - BeamoProtocol stuff, zip stuff up and upload to beamo or to a local Microfront-end admin service
  - Beamo does magic with cloudfront and s3 to set the SPA up.
    - How does auth work with cloudfront to make sure the request coming in is auth'ed by dev-people only?
    - Game stuff public api sites?
  - Portal gets from beamo where the micro-frontends for that realm/customer combo are and serves them.

- Server-Side-Rendering Approach:
  - How do we do this without locking ourselves into a single Server-side rendering framework?
  - Can we cloudfront a client callable so we cache the responses worlwide?


	 */
	public BeamoLocalSystem(ConfigService configService, IAppContext ctx, IRealmsApi service, BeamoService beamo)
	{
		_ctx = ctx;
		_beamo = beamo;
		_realmService = service;

		// We use a 60 second timeout because the Docker Daemon is VERY slow... If you ever see an "The operation was cancelled" message that happens inconsistently,
		// try changing this value before going down the rabbit hole. 
		// TODO: Verify that the protocol and path here works on Mac... I think it won't...
		_client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"), new AnonymousCredentials(), TimeSpan.FromSeconds(60))
			.CreateClient();

		// TODO: Make this persistence part agnostic of where this is running so we can use it in Unity as well, maybe?
		var beamableFilePath = configService.ConfigFilePath;
		// Load or create the local manifest
		_beamoLocalManifestFile = Path.Combine(beamableFilePath, "beamoLocalManifest.json");
		if (File.Exists(_beamoLocalManifestFile))
		{
			BeamoManifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(File.ReadAllText(_beamoLocalManifestFile), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
		}
		else
		{
			BeamoManifest = new BeamoLocalManifest()
			{
				ServiceDefinitions = new List<BeamoServiceDefinition>(8),
				HttpMicroserviceLocalProtocols = new Dictionary<string, HttpMicroserviceLocalProtocol>(4),
				HttpMicroserviceRemoteProtocols = new Dictionary<string, HttpMicroserviceRemoteProtocol>(4),
				EmbeddedMongoDbLocalProtocols = new Dictionary<string, EmbeddedMongoDbLocalProtocol>(4),
				EmbeddedMongoDbRemoteProtocols = new Dictionary<string, EmbeddedMongoDbRemoteProtocol>(4),
				LayeredDependencyGraph = null
			};
			SaveBeamoLocalManifest();
		}

		// Load or create the local runtime data
		_beamoLocalRuntimeFile = Path.Combine(beamableFilePath, "beamoLocalRuntime.json");
		if (File.Exists(_beamoLocalRuntimeFile))
		{
			BeamoRuntime = JsonConvert.DeserializeObject<BeamoLocalRuntime>(File.ReadAllText(_beamoLocalRuntimeFile), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
		}
		else
		{
			BeamoRuntime = new BeamoLocalRuntime() { ExistingLocalServiceInstances = new List<BeamoServiceInstance>(8) };
			SaveBeamoLocalRuntime();
		}

		// Make a cancellation token source to cancel the docker event stream we listen for updates. See StartListeningToDocker.
		_dockerListeningThreadCancel = new CancellationTokenSource();
	}

	/// <summary>
	/// Persists the current state of <see cref="BeamoManifest"/> out to disk. TODO: Make this persistence part agnostic of where this is running so we can use it in Unity as well, maybe?
	/// </summary>
	public void SaveBeamoLocalManifest() =>
		File.WriteAllText(_beamoLocalManifestFile, JsonConvert.SerializeObject(BeamoManifest, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto }));

	public void SaveBeamoLocalRuntime() =>
		File.WriteAllText(_beamoLocalRuntimeFile, JsonConvert.SerializeObject(BeamoRuntime, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto }));

	/// <summary>
	/// Checks to see if the service id matches the <see cref="BeamoServiceIdRegex"/>. 
	/// </summary>
	public bool ValidateBeamoServiceId_ValidCharacters(string beamoServiceId) =>
		BeamoServiceIdRegex.IsMatch(beamoServiceId);

	/// <summary>
	/// Checks if the given BeamO Service Id is already known in the current <see cref="BeamoManifest"/>.
	/// </summary>
	public bool ValidateBeamoServiceId_DoesntExists(string beamoServiceId) =>
		!BeamoManifest.ServiceDefinitions.Contains(new BeamoServiceDefinition() { BeamoId = beamoServiceId }, new BeamoServiceDefinition.IdEquality());

	public bool IsServiceRegistered(string beamoServiceId) =>
		BeamoManifest.ServiceDefinitions.Contains(new BeamoServiceDefinition() { BeamoId = beamoServiceId }, new BeamoServiceDefinition.IdEquality());

	/// <summary>
	/// Verifies, by expanding the dependency DAG from root, we don't see root again until we have walked through all dependencies. 
	/// </summary>
	private static bool ValidateBeamoService_NoCyclicalDependencies(BeamoServiceDefinition root, List<BeamoServiceDefinition> registeredDependencies)
	{
		var depsToVisit = new Stack<BeamoServiceDefinition>();
		depsToVisit.Push(root);

		// TODO: Keep track of path you are taking in the tree
		var rootSeenCount = 0;
		while (depsToVisit.Count > 0)
		{
			var checking = depsToVisit.Pop();

			// Add to this counter whenever we see the root.
			rootSeenCount += checking.BeamoId == root.BeamoId ? 1 : 0;

			// If we see it more than once it means we have a cyclical dependency.
			if (rootSeenCount > 1)
				break;

			// Pushes dependencies of this service onto the stack
			var deps = checking.DependsOnBeamoIds.Select(
					depId => registeredDependencies.FirstOrDefault(sd => sd.BeamoId == depId)
				)
				.Where(a => a != null)
				.ToList();

			foreach (var depService in deps)
				depsToVisit.Push(depService);
		}

		return rootSeenCount == 1;
	}

	/// <summary>
	/// Creates a service definition given the parameters.
	/// </summary>
	/// <param name="beamoId">A unique-id across all services in <see cref="RegisteredContainers"/>.</param>
	/// <param name="type">The type of the protocol we are adding.</param>
	/// <param name="projectPath">If the container's image will be built locally, this is the path to the docker-build-context (prefer absolute path).</param>
	/// <param name="dockerfilePath">If the container's image will be built locally, this is the relative path to a Dockerfile from inside the docker-build-context.</param>
	/// <param name="baseImage">If the container's image should be pulled or used locally (not built), this is the image's name (in the "name:tag" form).</param>
	/// <param name="beamoIdDependencies">The other Beam-O ids that this depends on. We validate for cyclical dependencies.</param>
	/// <param name="localConstructor">A task that will prepare the default parameters for the local protocol we are creating the service with.</param>
	/// <param name="remoteConstructor">A task that will prepare the default parameters for the remote protocol we are creating the service with.</param>
	/// <param name="cancellationToken">A cancellation token that we pass into both local and remote tasks. Can be used to cancel both tasks.</param>
	/// <typeparam name="TLocal">The type of the <see cref="IBeamoLocalProtocol"/> that this service definition uses.</typeparam>
	/// <typeparam name="TRemote">The type of the <see cref="IBeamoRemoteProtocol"/> that this service definition uses.</typeparam>
	/// <returns>The created service definition.</returns>
	private async Task<BeamoServiceDefinition> AddServiceDefinition<TLocal, TRemote>(string beamoId,
		BeamoProtocolType type,
		string[] beamoIdDependencies,
		Func<BeamoServiceDefinition, TLocal, Task> localConstructor, Func<BeamoServiceDefinition, TRemote, Task> remoteConstructor, CancellationToken cancellationToken)
		where TLocal : class, IBeamoLocalProtocol, new() where TRemote : class, IBeamoRemoteProtocol, new()
	{
		// Verify that we aren't creating a non-unique beamo id.
		if (!ValidateBeamoServiceId_DoesntExists(beamoId))
			throw new ArgumentOutOfRangeException(nameof(beamoId), $"Attempting to register a service definition that's already registered [BeamoId={beamoId}]. This is not allowed.");

		// Verify that we aren't creating a non-unique beamo id.
		if (!ValidateBeamoServiceId_ValidCharacters(beamoId))
			throw new ArgumentOutOfRangeException(nameof(beamoId), $"Attempting to register a service with an invalid [BeamoId={beamoId}]. Only alphanumeric and underscore are allowed.");

		var serviceDefinition = new BeamoServiceDefinition()
		{
			BeamoId = beamoId,
			Protocol = type,
			DependsOnBeamoIds = beamoIdDependencies,
			ImageId = string.Empty,
			ShouldBeEnabledOnRemote = false,
		};

		// Register the services before initializing protocols so that the protocol initialization can know about the service. 
		BeamoManifest.ServiceDefinitions.Add(serviceDefinition);

		// Verify that we aren't creating cyclical dependencies
		if (!ValidateBeamoService_NoCyclicalDependencies(serviceDefinition, BeamoManifest.ServiceDefinitions))
			throw new ArgumentOutOfRangeException(nameof(beamoIdDependencies), "Attempting to register a service definition with a cyclical dependency. Please make sure that is not the case.");

		// Set up local and remote protocol with their defaults.
		var local = new TLocal();
		var localConstructorTask = localConstructor(serviceDefinition, local);

		var remote = new TRemote();
		var remoteConstructorTask = remoteConstructor(serviceDefinition, remote);

		// Wait for the protocols to run and assign them
		await Task.WhenAll(localConstructorTask, remoteConstructorTask).WaitAsync(cancellationToken);
		switch (type)
		{
			case BeamoProtocolType.HttpMicroservice:
			{
				BeamoManifest.HttpMicroserviceLocalProtocols.Add(beamoId, local as HttpMicroserviceLocalProtocol);
				BeamoManifest.HttpMicroserviceRemoteProtocols.Add(beamoId, remote as HttpMicroserviceRemoteProtocol);
				break;
			}
			case BeamoProtocolType.EmbeddedMongoDb:
			{
				BeamoManifest.EmbeddedMongoDbLocalProtocols.Add(beamoId, local as EmbeddedMongoDbLocalProtocol);
				BeamoManifest.EmbeddedMongoDbRemoteProtocols.Add(beamoId, remote as EmbeddedMongoDbRemoteProtocol);
				break;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}

		return serviceDefinition;
	}

	/// <summary>
	/// Tries to run the given update task on the <see cref="IBeamoLocalProtocol"/> of the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/>. 
	/// </summary>
	/// <param name="cancellationToken">A token that we pass to the given task. Can be used to cancel the task, if needed.</param>
	/// <typeparam name="TLocal">The <see cref="IBeamoLocalProtocol"/> that the service definition with the given <paramref name="beamoId"/> is expected to contain.</typeparam>
	/// <returns>Whether or not the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> was found.</returns>
	public async Task<bool> TryUpdateLocalProtocol<TLocal>(string beamoId, Func<BeamoServiceDefinition, TLocal, Task> localProtocolModifier, CancellationToken cancellationToken)
		where TLocal : class, IBeamoLocalProtocol
	{
		var containerIdx = BeamoManifest.ServiceDefinitions.FindIndex(container => container.BeamoId == beamoId);
		var foundContainer = containerIdx != -1;

		if (foundContainer)
		{
			var sd = BeamoManifest.ServiceDefinitions[containerIdx];
			var type = sd.Protocol;
			switch (type)
			{
				case BeamoProtocolType.HttpMicroservice:
				{
					await localProtocolModifier(sd, BeamoManifest.HttpMicroserviceLocalProtocols[sd.BeamoId] as TLocal).WaitAsync(cancellationToken);
					break;
				}
				case BeamoProtocolType.EmbeddedMongoDb:
				{
					await localProtocolModifier(sd, BeamoManifest.EmbeddedMongoDbLocalProtocols[sd.BeamoId] as TLocal).WaitAsync(cancellationToken);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		return foundContainer;
	}

	/// <summary>
	/// Tries to run the given update task on the <see cref="IBeamoRemoteProtocol"/> of the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/>. 
	/// </summary>
	/// <param name="cancellationToken">A token that we pass to the given task. Can be used to cancel the task, if needed.</param>
	/// <typeparam name="TRemote">The <see cref="IBeamoRemoteProtocol"/> that the service definition with the given <paramref name="beamoId"/> is expected to contain.</typeparam>
	/// <returns>Whether or not the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> was found.</returns>
	public async Task<bool> TryUpdateRemoteProtocol<TRemote>(string beamoId, Func<BeamoServiceDefinition, TRemote, Task> remoteProtocolModifier, CancellationToken cancellationToken)
		where TRemote : class, IBeamoRemoteProtocol
	{
		var containerIdx = BeamoManifest.ServiceDefinitions.FindIndex(container => container.BeamoId == beamoId);
		var foundContainer = containerIdx != -1;

		if (foundContainer)
		{
			var sd = BeamoManifest.ServiceDefinitions[containerIdx];
			var type = sd.Protocol;
			switch (type)
			{
				case BeamoProtocolType.HttpMicroservice:
				{
					await remoteProtocolModifier(sd, BeamoManifest.HttpMicroserviceRemoteProtocols[sd.BeamoId] as TRemote).WaitAsync(cancellationToken);
					break;
				}
				case BeamoProtocolType.EmbeddedMongoDb:
				{
					await remoteProtocolModifier(sd, BeamoManifest.EmbeddedMongoDbRemoteProtocols[sd.BeamoId] as TRemote).WaitAsync(cancellationToken);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		return foundContainer;
	}
}

public class BeamoLocalManifest
{
	// TODO : Need to change this to work without polymorphism if we want to send this "as is" to server --- but... the more I think about this, the more I think we shouldn't...
	public List<BeamoServiceDefinition> ServiceDefinitions;

	public Dictionary<string, HttpMicroserviceLocalProtocol> HttpMicroserviceLocalProtocols;
	public Dictionary<string, HttpMicroserviceRemoteProtocol> HttpMicroserviceRemoteProtocols;

	public Dictionary<string, EmbeddedMongoDbLocalProtocol> EmbeddedMongoDbLocalProtocols;
	public Dictionary<string, EmbeddedMongoDbRemoteProtocol> EmbeddedMongoDbRemoteProtocols;

	/// <summary>
	/// Built out of the <see cref="ServiceDefinitions"/>, each sub-array contains all the image dependencies for that particular layer. 
	/// </summary>
	public int[][] LayeredDependencyGraph;
}

public class BeamoServiceDefinition
{
	/// <summary>
	/// The id that this service will be know, both locally and remotely.
	/// </summary>
	public string BeamoId;

	/// <summary>
	/// The protocol this service respects.
	/// </summary>
	public BeamoProtocolType Protocol;

	/// <summary>
	/// List of <see cref="BeamoId"/>s that this service depends on.
	/// </summary>
	public string[] DependsOnBeamoIds;

	/// <summary>
	/// Gets the truncated version of the image id (used for deploying the service manifest to Beamo. TODO Ideally, we should make beamo use the full ID later...
	/// </summary>
	public string TruncImageId => ImageId.Contains(':') ? ImageId.Split(':')[1].Substring(0, 12) : ImageId;

	/// <summary>
	/// This is what we need for deployment.
	/// </summary>
	public string ImageId;

	/// <summary>
	/// Whether or not this service should be enabled when we deploy remotely.  
	/// </summary>
	public bool ShouldBeEnabledOnRemote;

	public struct IdEquality : IEqualityComparer<BeamoServiceDefinition>
	{
		public bool Equals(BeamoServiceDefinition x, BeamoServiceDefinition y) => x.BeamoId == y.BeamoId;

		public int GetHashCode(BeamoServiceDefinition obj) => (obj.BeamoId != null ? obj.BeamoId.GetHashCode() : 0);
	}

	public static void BuildExposedPorts(List<DockerPortBinding> bindings, out Dictionary<string, EmptyStruct> exposedPorts)
	{
		exposedPorts = bindings.ToDictionary(b => b.InContainerPort, _ => new EmptyStruct());
	}

	public static void BuildHostPortBinding(List<DockerPortBinding> bindings, out Dictionary<string, IList<PortBinding>> boundPorts)
	{
		boundPorts = bindings
			.ToDictionary(b => b.InContainerPort,
				b => (IList<PortBinding>)new List<PortBinding>() { new() { HostPort = b.LocalPort } });
	}

	public static void BuildEnvVars(out List<string> envs, params List<DockerEnvironmentVariable>[] envVarMaps)
	{
		envs = new List<string>();
		foreach (var envVars in envVarMaps)
			envs.AddRange(envVars.Select(envVar => $"{envVar.VariableName}={envVar.Value}"));
	}

	public static void BuildVolumes(List<DockerVolume> volumes, out List<string> boundVolumes)
	{
		boundVolumes = new List<string>(volumes.Count);
		boundVolumes.AddRange(volumes.Select(v => $"{v.VolumeName}:{v.InContainerPath}"));
	}

	public static void BuildBindMounts(List<DockerBindMount> bindMounts, out List<string> boundMounts)
	{
		boundMounts = new List<string>(bindMounts.Count);
		boundMounts.AddRange(bindMounts.Select(bm =>
		{
			var options = bm.IsReadOnly ? ":ro" : "";
			return $"{bm.LocalPath}:{bm.InContainerPath}{options}";
		}));
	}
}

public struct DockerPortBinding
{
	public string LocalPort;
	public string InContainerPort;
}

public struct DockerVolume
{
	public string VolumeName;
	public string InContainerPath;
}

public struct DockerBindMount
{
	public bool IsReadOnly;
	public string LocalPath;
	public string InContainerPath;
}

public struct DockerEnvironmentVariable
{
	public string VariableName;
	public string Value;
}

public enum BeamoProtocolType
{
	// Current C#MS stuff (after we remove the WebSocket stuff)
	HttpMicroservice,

	// Current Mongo-based Data Storage
	EmbeddedMongoDb,
}

public interface IBeamoLocalProtocol
{
	public bool VerifyCanBeBuiltLocally();
}

public interface IBeamoRemoteProtocol
{
}

// Move this stuff to app context and initialization
[Serializable]
public class CustomerResponse
{
	public CustomerDTO customer;
}

[Serializable]
public class CustomerDTO
{
	public List<ProjectDTO> projects;
}

[Serializable]
public class ProjectDTO
{
	public string name;
	public string secret;
}
