using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text.RegularExpressions;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	private static readonly Regex BeamoServiceIdRegex = new Regex("^[a-zA-Z0-9_]+$");

	/// <summary>
	/// A local manifest instance that we keep in sync with the <see cref="_beamoLocalManifestFile"/> json file.
	/// </summary>
	public readonly BeamoLocalManifest BeamoManifest;

	public readonly Dictionary<BeamoServiceDefinition,List<string>> ServicesDependencies = new();

	/// <summary>
	/// The current local state of containers, associated with the <see cref="BeamoLocalManifest.ServiceDefinitions"/>, keept in sync with the <see cref="_beamoLocalRuntimeFile"/> json file.
	/// </summary>
	public readonly BeamoLocalRuntime BeamoRuntime;

	private readonly ConfigService _configService;

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
	private IRealmsApi _realmApi;

	/// <summary>
	/// The requester used to make requests to the beamale API.
	/// This is used to get information from microservices that are running locally during the deploy.
	/// </summary>
	private IBeamableRequester _beamableRequester;

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

	public BeamoLocalSystem(ConfigService configService, IAppContext ctx, IRealmsApi realmsApi, BeamoService beamo, IBeamableRequester beamableRequester)
	{
		_configService = configService;
		_ctx = ctx;
		_beamo = beamo;
		_realmApi = realmsApi;
		_beamableRequester = beamableRequester;

		// We use a 60 second timeout because the Docker Daemon is VERY slow... If you ever see an "The operation was cancelled" message that happens inconsistently,
		// try changing this value before going down the rabbit hole.
		_client = new DockerClientConfiguration(new AnonymousCredentials(), TimeSpan.FromSeconds(60))
			.CreateClient();

		// Load or create the local manifest
		BeamoManifest = _configService.LoadDataFile<BeamoLocalManifest>(Constants.BEAMO_LOCAL_MANIFEST_FILE_NAME, () => new BeamoLocalManifest()
		{
			ServiceDefinitions = new List<BeamoServiceDefinition>(8),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>(),
			HttpMicroserviceRemoteProtocols = new BeamoRemoteProtocolMap<HttpMicroserviceRemoteProtocol>(),
			EmbeddedMongoDbLocalProtocols = new BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol>(),
			EmbeddedMongoDbRemoteProtocols = new BeamoRemoteProtocolMap<EmbeddedMongoDbRemoteProtocol>(),
		});
		// Load or create the local runtime data
		BeamoRuntime = _configService.LoadDataFile<BeamoLocalRuntime>(Constants.BEAMO_LOCAL_RUNTIME_FILE_NAME, () =>
			new BeamoLocalRuntime() { ExistingLocalServiceInstances = new List<BeamoServiceInstance>(8) });

		// Make a cancellation token source to cancel the docker event stream we listen for updates. See StartListeningToDocker.
		_dockerListeningThreadCancel = new CancellationTokenSource();
	}

	/// <summary>
	/// Persists the current state of <see cref="BeamoManifest"/> out to disk. TODO: Make this persistence part agnostic of where this is running so we can use it in Unity as well, maybe?
	/// </summary>
	public void SaveBeamoLocalManifest() => _configService.SaveDataFile(Constants.BEAMO_LOCAL_MANIFEST_FILE_NAME, BeamoManifest);
	public void SaveBeamoLocalRuntime() => _configService.SaveDataFile(Constants.BEAMO_LOCAL_RUNTIME_FILE_NAME, BeamoRuntime);

	/// <summary>
	/// Checks to see if the service id matches the <see cref="BeamoServiceIdRegex"/>.
	/// </summary>
	public static bool ValidateBeamoServiceId_ValidCharacters(string beamoServiceId) =>
		BeamoServiceIdRegex.IsMatch(beamoServiceId);

	/// <summary>
	/// Get list of <see cref="BeamoId"/>s that this service depends on.
	/// </summary>
	/// <param name="beamoServiceId">The identifier of the Beamo service.</param>
	/// <param name="projectExtension">The extension of the project files. Default is "csproj".</param>
	/// <returns>Returns a list of <see cref="BeamoId"/>s that this service depends on.</returns>
	public async Task<List<string>> GetDependencies(string beamoServiceId, string projectExtension = "csproj")
	{
		var serviceDefinition = BeamoManifest.ServiceDefinitions.FirstOrDefault(s => s.BeamoId == beamoServiceId);
		if (string.IsNullOrWhiteSpace(serviceDefinition?.ProjectDirectory))
		{
			return new List<string>();
		}
		var path = _configService.GetRelativePath(serviceDefinition!.ProjectDirectory);
		path = Path.Combine(path, $"{beamoServiceId}.{projectExtension}");
		var (cmd,builder) = await CliExtensions.RunWithOutput(_ctx.DotnetPath, $"list {path} reference");
		if (cmd.ExitCode != 0)
		{
			throw new CliException($"Getting service dependencies failed, command output: {builder}");
		}
		// TODO improve it, for now it is naive, if there is related project with same name as one of the services it will treat it as it is connected
		var dependencies = builder.ToString().Split(Environment.NewLine).Where(line => line.EndsWith(projectExtension))
			.Select(Path.GetFileNameWithoutExtension).Where(candidate => BeamoManifest.ServiceDefinitions.Any(definition => definition.BeamoId==candidate)).ToList();
		
		return dependencies;
	}

	public async Task AddProjectDependency(BeamoServiceDefinition project, BeamoServiceDefinition dependency)
	{
		if (project.Protocol != BeamoProtocolType.HttpMicroservice ||
		    dependency.Protocol != BeamoProtocolType.EmbeddedMongoDb)
		{
			throw new CliException(
				$"Currently the only supported dependencies are {nameof(BeamoProtocolType.HttpMicroservice)} depending on {nameof(BeamoProtocolType.EmbeddedMongoDb)}");
		}
		var projectPath = _configService.GetRelativePath(project.ProjectDirectory);
		var dependencyPath = _configService.GetRelativePath(dependency.ProjectDirectory);
		var command = $"add {projectPath} reference {dependencyPath}";
		var(cmd, result) = await CliExtensions.RunWithOutput(_ctx.DotnetPath, command,Directory.GetCurrentDirectory());
		if (cmd.ExitCode != 0)
		{
			throw new CliException($"Failed to add project dependency, output of \"dotnet {command}\": {result}");
		}

		var service = BeamoManifest.HttpMicroserviceLocalProtocols[project.BeamoId];
		var dockerfilePath = service.RelativeDockerfilePath;
		dockerfilePath = _configService.GetFullPath(Path.Combine(service.DockerBuildContextPath, dockerfilePath));
		var dockerfileText = await File.ReadAllTextAsync(dockerfilePath);

		const string search =
			"# <BEAM-CLI-INSERT-FLAG:COPY> do not delete this line. It is used by the beam CLI to insert custom actions";
		string toAdd =@$"WORKDIR /subsrc/{dependency.BeamoId}
COPY {dependency.BeamoId}/. .";
		var replacement = @$"{toAdd}
{search}";
		if(!dockerfileText.Replace("\r\n","\n").Contains(toAdd))
		{
			dockerfileText = dockerfileText.Replace(search, replacement);
			await File.WriteAllTextAsync(dockerfilePath, dockerfileText);
		}
		ServicesDependencies.Clear();
	}

	/// <summary>
	/// Retrieves the dependencies of each Beamo service defined in the BeamoManifest.
	/// </summary>
	/// <param name="projectExtension">The extension of the project file (default: 'csproj').</param>
	/// <returns>A dictionary where the key is a BeamoServiceDefinition and the value is a list of its dependencies.</returns>
	public async Task<Dictionary<BeamoServiceDefinition,List<string>>> GetAllBeamoIdsDependencies(string projectExtension = "csproj")
	{
		
		foreach (var definition in BeamoManifest.ServiceDefinitions)
		{
			if(!ServicesDependencies.ContainsKey(definition))
			{
				var entry = await GetDependencies(definition.BeamoId, projectExtension);
				ServicesDependencies.Add(definition, entry);
			}
		}

		return ServicesDependencies;
	}
	
	/// <summary>
	/// Checks if the given BeamO Service Id is already known in the current <see cref="BeamoManifest"/>.
	/// </summary>
	public static bool ValidateBeamoServiceId_DoesntExists(string beamoServiceId, List<BeamoServiceDefinition> serviceDefinitions) =>
		!serviceDefinitions.Contains(new BeamoServiceDefinition() { BeamoId = beamoServiceId }, new BeamoServiceDefinition.IdEquality());

	/// <summary>
	/// Verifies, by expanding the dependency DAG from root, we don't see root again until we have walked through all dependencies.
	/// </summary>
	private async Task<bool> ValidateBeamoService_NoCyclicalDependencies(BeamoServiceDefinition root, List<BeamoServiceDefinition> registeredDependencies)
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
			var deps = (await GetDependencies(checking.BeamoId)).Select(
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
	/// <param name="localConstructor">A task that will prepare the default parameters for the local protocol we are creating the service with.</param>
	/// <param name="remoteConstructor">A task that will prepare the default parameters for the remote protocol we are creating the service with.</param>
	/// <param name="cancellationToken">A cancellation token that we pass into both local and remote tasks. Can be used to cancel both tasks.</param>
	/// <param name="shouldServiceBeEnabled">Should service be enabled/disabled when adding service definition</param>
	/// <typeparam name="TLocal">The type of the <see cref="IBeamoLocalProtocol"/> that this service definition uses.</typeparam>
	/// <typeparam name="TRemote">The type of the <see cref="IBeamoRemoteProtocol"/> that this service definition uses.</typeparam>
	/// <returns>The created service definition.</returns>
	private async Task<BeamoServiceDefinition> AddServiceDefinition<TLocal, TRemote>(string beamoId, BeamoProtocolType type, 
		LocalProtocolModifier<TLocal> localConstructor, RemoteProtocolModifier<TRemote> remoteConstructor, CancellationToken cancellationToken, bool shouldServiceBeEnabled = true)
		where TLocal : class, IBeamoLocalProtocol, new() where TRemote : class, IBeamoRemoteProtocol, new() =>
		await AddServiceDefinition(BeamoManifest, beamoId, type, localConstructor, remoteConstructor, cancellationToken, shouldServiceBeEnabled);

	/// <summary>
	/// <inheritdoc cref="AddServiceDefinition{TLocal,TRemote}(string,cli.Services.BeamoProtocolType,string[],cli.Services.LocalProtocolModifier{TLocal},cli.Services.RemoteProtocolModifier{TRemote},System.Threading.CancellationToken)"/>
	/// </summary>
	private async Task<BeamoServiceDefinition> AddServiceDefinition<TLocal, TRemote>(BeamoLocalManifest beamoLocalManifest, string beamoId, BeamoProtocolType type,
		LocalProtocolModifier<TLocal> localConstructor, RemoteProtocolModifier<TRemote> remoteConstructor, CancellationToken cancellationToken, bool shouldServiceBeEnabled = true)
		where TLocal : class, IBeamoLocalProtocol, new() where TRemote : class, IBeamoRemoteProtocol, new()
	{
		// Verify that we aren't creating a non-unique beamo id.
		if (!ValidateBeamoServiceId_DoesntExists(beamoId, beamoLocalManifest.ServiceDefinitions))
			throw new ArgumentOutOfRangeException(nameof(beamoId), $"Attempting to register a service definition that's already registered [BeamoId={beamoId}]. This is not allowed.");

		// Verify that we aren't creating a non-unique beamo id. TODO: Change Comment
		if (!ValidateBeamoServiceId_ValidCharacters(beamoId))
			throw new ArgumentOutOfRangeException(nameof(beamoId), $"Attempting to register a service with an invalid [BeamoId={beamoId}]. Only alphanumeric and underscore are allowed.");

		var serviceDefinition = new BeamoServiceDefinition()
		{
			BeamoId = beamoId,
			Protocol = type,
			ImageId = string.Empty,
			ShouldBeEnabledOnRemote = shouldServiceBeEnabled,
		};

		// Register the services before initializing protocols so that the protocol initialization can know about the service.
		beamoLocalManifest.ServiceDefinitions.Add(serviceDefinition);

		// Verify that we aren't creating cyclical dependencies
		var noCyclicalDeps = await ValidateBeamoService_NoCyclicalDependencies(serviceDefinition, beamoLocalManifest.ServiceDefinitions);
		if (!noCyclicalDeps)
			throw new ArgumentOutOfRangeException(nameof(serviceDefinition), "Attempting to register a service definition with a cyclical dependency. Please make sure that is not the case.");

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
				beamoLocalManifest.HttpMicroserviceLocalProtocols.Add(beamoId, local as HttpMicroserviceLocalProtocol);
				beamoLocalManifest.HttpMicroserviceRemoteProtocols.Add(beamoId, remote as HttpMicroserviceRemoteProtocol);
				break;
			}
			case BeamoProtocolType.EmbeddedMongoDb:
			{
				beamoLocalManifest.EmbeddedMongoDbLocalProtocols.Add(beamoId, local as EmbeddedMongoDbLocalProtocol);
				beamoLocalManifest.EmbeddedMongoDbRemoteProtocols.Add(beamoId, remote as EmbeddedMongoDbRemoteProtocol);
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
	public async Task<bool> TryUpdateLocalProtocol<TLocal>(string beamoId, LocalProtocolModifier<TLocal> localProtocolModifier, CancellationToken cancellationToken)
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
	public async Task<bool> TryUpdateRemoteProtocol<TRemote>(string beamoId, RemoteProtocolModifier<TRemote> remoteProtocolModifier, CancellationToken cancellationToken)
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


/// <summary>
/// A function that takes in the <see cref="BeamoServiceDefinition"/> plus it's associated Local Protocol so that it can make changes to the protocol instance.
/// </summary>
/// <typeparam name="TLocal">The type of <see cref="IBeamoLocalProtocol"/> associated with the expected <see cref="BeamoServiceDefinition.Protocol"/>.</typeparam>
public delegate Task LocalProtocolModifier<in TLocal>(BeamoServiceDefinition owner, TLocal protocol) where TLocal : IBeamoLocalProtocol;

/// <summary>
/// A function that takes in the <see cref="BeamoServiceDefinition"/> plus it's associated Remote Protocol so that it can make changes to the protocol instance.
/// </summary>
/// <typeparam name="TRemote">The type of <see cref="IBeamoRemoteProtocol"/> associated with the given <see cref="BeamoServiceDefinition.Protocol"/>.</typeparam>
public delegate Task RemoteProtocolModifier<in TRemote>(BeamoServiceDefinition owner, TRemote protocol) where TRemote : IBeamoRemoteProtocol;


/// <summary>
/// A "typedef" around a <see cref="string"/> to <see cref="IBeamoLocalProtocol"/> <see cref="Dictionary{TKey,TValue}"/>.
/// </summary>
public class BeamoLocalProtocolMap<T> : Dictionary<string, T> where T : IBeamoLocalProtocol
{
}

/// <summary>
/// A "typedef" around a <see cref="string"/> to <see cref="IBeamoRemoteProtocol"/> <see cref="Dictionary{TKey,TValue}"/>.
/// </summary>
public class BeamoRemoteProtocolMap<T> : Dictionary<string, T> where T : IBeamoRemoteProtocol
{
}

/// <summary>
/// Our representation of <see cref="BeamoServiceDefinition"/> and the data they need to correctly enforce their defined <see cref="BeamoProtocolType"/> (stored in dictionaries.
/// </summary>
public class BeamoLocalManifest
{
	/// <summary>
	/// This list contains all the <see cref="BeamoServiceDefinition"/> that the current machine knows about. TODO: At a minimum, this list is kept in sync with already deployed services?
	/// </summary>
	public List<BeamoServiceDefinition> ServiceDefinitions;

	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol> HttpMicroserviceLocalProtocols;

	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoRemoteProtocolMap<HttpMicroserviceRemoteProtocol> HttpMicroserviceRemoteProtocols;


	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoLocalProtocolMap<EmbeddedMongoDbLocalProtocol> EmbeddedMongoDbLocalProtocols;

	/// <summary>
	/// These are map individual <see cref="BeamoServiceDefinition.BeamoId"/>s to their protocol data. Since we don't allow changing protocols we don't ever need to move the services' protocol data between these.
	/// </summary>
	public BeamoRemoteProtocolMap<EmbeddedMongoDbRemoteProtocol> EmbeddedMongoDbRemoteProtocols;
	
	public void Clear()
	{
		ServiceDefinitions.Clear();
		HttpMicroserviceLocalProtocols.Clear();
		HttpMicroserviceRemoteProtocols.Clear();
		EmbeddedMongoDbLocalProtocols.Clear();
		EmbeddedMongoDbRemoteProtocols.Clear();
	}

	/// <summary>
	/// Tries to get the definition of a Beamo service based on the provided BeamoId.
	/// </summary>
	/// <param name="beamoId">The BeamoId of the service.</param>
	/// <param name="definition">When this method returns, contains the BeamoServiceDefinition associated with the specified BeamoId, if found; otherwise, null.</param>
	/// <returns>
	/// true if the service definition is found; otherwise, false.
	/// </returns>
	public bool TryGetDefinition(string beamoId, out BeamoServiceDefinition definition)
	{
		definition = ServiceDefinitions.FirstOrDefault(definition => definition.BeamoId == beamoId);
		
		return definition != null;
	}
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

	/// <summary>
	/// Path to the directory containing project file(csproj).
	/// </summary>
	public string ProjectDirectory;

	/// <summary>
	/// Defines two services as being equal simply by using their <see cref="BeamoServiceDefinition.BeamoId"/>.
	/// </summary>
	public struct IdEquality : IEqualityComparer<BeamoServiceDefinition>
	{
		public bool Equals(BeamoServiceDefinition x, BeamoServiceDefinition y) => x.BeamoId == y.BeamoId;

		public int GetHashCode(BeamoServiceDefinition obj) => (obj.BeamoId != null ? obj.BeamoId.GetHashCode() : 0);
	}

	/// <summary>
	/// Converts a list of <see cref="DockerPortBinding"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildExposedPorts(List<DockerPortBinding> bindings, out Dictionary<string, EmptyStruct> exposedPorts) =>
		exposedPorts = bindings.ToDictionary(b => b.InContainerPort, _ => new EmptyStruct());

	/// <summary>
	/// Converts a list of <see cref="DockerPortBinding"/> to the second format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildHostPortBinding(List<DockerPortBinding> bindings, out Dictionary<string, IList<PortBinding>> boundPorts) =>
		boundPorts = bindings
			.ToDictionary(b => b.InContainerPort,
				b => (IList<PortBinding>)new List<PortBinding>() { new() { HostPort = b.LocalPort } });

	/// <summary>
	/// Converts a list of <see cref="DockerEnvironmentVariable"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildEnvVars(out List<string> envs, params List<DockerEnvironmentVariable>[] envVarMaps)
	{
		envs = new List<string>();
		foreach (var envVars in envVarMaps)
			envs.AddRange(envVars.Select(envVar => $"{envVar.VariableName}={envVar.Value}"));
	}

	/// <summary>
	/// Converts a list of <see cref="DockerVolume"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
	public static void BuildVolumes(List<DockerVolume> volumes, out List<string> boundVolumes)
	{
		boundVolumes = new List<string>(volumes.Count);
		boundVolumes.AddRange(volumes.Select(v => $"{v.VolumeName}:{v.InContainerPath}"));
	}

	/// <summary>
	/// Converts a list of <see cref="DockerBindMount"/> to the format expected by the Docker.NET API (<see cref="BeamoLocalSystem._client"/>).
	/// </summary>
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

/// <summary>
/// Data representing a Docker Port Binding.
/// </summary>
public struct DockerPortBinding
{
	/// <summary>
	/// The port in localhost.
	/// </summary>
	public string LocalPort;

	/// <summary>
	/// The port the application inside the container cares about.
	/// </summary>
	public string InContainerPort;
}

/// <summary>
/// Data representing a Docker Named Volume.
/// </summary>
public struct DockerVolume
{
	/// <summary>
	/// The name of the volume.
	/// </summary>
	public string VolumeName;

	/// <summary>
	/// The path into the container's file system where this named volume will be bound to.
	/// </summary>
	public string InContainerPath;
}

/// <summary>
/// Data representing a Docker Bind Mount.
/// </summary>
public struct DockerBindMount
{
	/// <summary>
	/// Whether the container is allowed to write to the localhost's file system.
	/// </summary>
	public bool IsReadOnly;

	/// <summary>
	/// Path in localhost.
	/// </summary>
	public string LocalPath;

	/// <summary>
	/// Path into the container's file system where the directory in <see cref="LocalPath"/> will be mounted into.
	/// </summary>
	public string InContainerPath;
}

/// <summary>
/// Data representing an Environment Variable definition.
/// </summary>
public struct DockerEnvironmentVariable
{
	public string VariableName;
	public string Value;
}


/// <summary>
/// The type of protocol a <see cref="BeamoServiceDefinition.Protocol"/> was created with. Conceptually, a protocol is just a set of algorithms and data that solve the problem of:
///  - What protocol do I follow to get this service running locally and in Beamo?
///  - What data does this protocol need to make the service run correctly in each of these places? (<see cref="IBeamoLocalProtocol"/> and <see cref="IBeamoRemoteProtocol"/>)
///  - How do I cleanup the local running Docker Engine given what I had to do to get the service running? (Some protocols may spawn multiple docker images, for example).
///
/// So, while the <see cref="BeamoServiceDefinition"/> deals with the dependencies between the services and whether or not it should be enabled when we deploy remotely to Beamo,
/// the protocol defines how Beamo (remote or local) will actually get the service to work.
/// </summary>
public enum BeamoProtocolType
{
	// Current C#MS stuff (after we remove the WebSocket stuff)
	HttpMicroservice,

	// Current Mongo-based Data Storage
	EmbeddedMongoDb,
}

public interface IBeamoLocalProtocol
{
	public bool VerifyCanBeBuiltLocally(ConfigService configService);
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
