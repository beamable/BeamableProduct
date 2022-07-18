using Beamable.Common;
using Beamable.Common.Api;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using Spectre.Console;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace cli;

public partial class BeamoLocalService
{
	private static readonly Regex BeamoServiceIdRegex = new Regex("^[a-zA-Z0-9_]+$");

	public readonly BeamoLocalManifest BeamoManifest;
	public readonly BeamoLocalRuntime BeamoRuntime;

	private readonly IAppContext _ctx;
	private readonly IBeamableRequester _requester;

	private readonly DockerClient _client;
	private readonly string _beamoLocalManifestFile;
	private readonly string _beamoLocalRuntimeFile;

	private Task _dockerListeningThread;
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
	public BeamoLocalService(ConfigService configService, IAppContext ctx, IBeamableRequester requester)
	{
		_ctx = ctx;
		_requester = requester;

		// We use a 30 second timeout because the Docker Daemon is VERY slow... If you ever see an "The operation was cancelled" message that happens inconsistently,
		// try changing this value before going down the rabbit hole. 
		// TODO: Read the actual timeout value from a config variable so we can update in the wild if we ever need it...
		_client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"), new AnonymousCredentials(), TimeSpan.FromSeconds(30))
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
			BeamoManifest = new BeamoLocalManifest() { ServiceDefinitions = new List<BeamoServiceDefinition>(8), LayeredDependencyGraph = null };
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
			BeamoRuntime = new BeamoLocalRuntime() { ExistingLocalServiceInstances = new List<BeamoServiceInstance>(8), BeamoIdsToTryEnableOnRemoteDeploy = new List<string>(8) };
			SaveBeamoLocalRuntime();
		}

		_dockerListeningThreadCancel = new CancellationTokenSource();
	}


	/// <summary>
	/// TODO: Move this somewhere else...
	/// </summary>
	public Task<string> GetRealmSecret()
	{
		// TODO this will only work if the current user is an admin (developer).
		return Task.Run(async () =>
		{
			var str = await _requester.Request<CustomerResponse>(Method.GET, "/basic/realms/admin/customer").Map(resp =>
			{
				var matchingProject = resp.customer.projects.FirstOrDefault(p => p.name.Equals(_ctx.Pid));
				return matchingProject?.secret ?? "";
			});

			return str;
		});
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
	private async Task<BeamoServiceDefinition> AddServiceDefinition<TLocal, TRemote>(string beamoId, BeamoProtocolType type, string projectPath, string dockerfilePath, string baseImage,
		string[] beamoIdDependencies, Func<BeamoServiceDefinition, TLocal, Task> localConstructor, Func<BeamoServiceDefinition, TRemote, Task> remoteConstructor, CancellationToken cancellationToken)
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
			BaseImage = baseImage,
			DockerBuildContextPath = projectPath,
			RelativeDockerfilePath = dockerfilePath,
			DependsOnBeamoIds = beamoIdDependencies,
			ImageId = string.Empty,
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
		serviceDefinition.LocalProtocol = local;
		serviceDefinition.RemoteProtocol = remote;


		return serviceDefinition;
	}

	/// <summary>
	/// Tries to run the given update task on the <see cref="IBeamoLocalProtocol"/> of the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/>. 
	/// </summary>
	/// <param name="cancellationToken">A token that we pass to the given task. Can be used to cancel the task, if needed.</param>
	/// <typeparam name="TLocal">The <see cref="IBeamoLocalProtocol"/> that the service definition with the given <paramref name="beamoId"/> is expected to contain.</typeparam>
	/// <returns>Whether or not the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> was found.</returns>
	private async Task<bool> TryUpdateLocalProtocol<TLocal>(string beamoId, Func<BeamoServiceDefinition, TLocal, Task> localProtocolModifier, CancellationToken cancellationToken)
		where TLocal : class, IBeamoLocalProtocol
	{
		var containerIdx = BeamoManifest.ServiceDefinitions.FindIndex(container => container.BeamoId == beamoId);
		var foundContainer = containerIdx != -1;

		if (foundContainer)
			await localProtocolModifier(BeamoManifest.ServiceDefinitions[containerIdx], BeamoManifest.ServiceDefinitions[containerIdx].LocalProtocol as TLocal).WaitAsync(cancellationToken);

		return foundContainer;
	}

	/// <summary>
	/// Tries to run the given update task on the <see cref="IBeamoRemoteProtocol"/> of the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/>. 
	/// </summary>
	/// <param name="cancellationToken">A token that we pass to the given task. Can be used to cancel the task, if needed.</param>
	/// <typeparam name="TRemote">The <see cref="IBeamoRemoteProtocol"/> that the service definition with the given <paramref name="beamoId"/> is expected to contain.</typeparam>
	/// <returns>Whether or not the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> was found.</returns>
	private async Task<bool> TryUpdateRemoteProtocol<TRemote>(string beamoId, Func<BeamoServiceDefinition, TRemote, Task> remoteProtocolModifier, CancellationToken cancellationToken)
		where TRemote : class, IBeamoRemoteProtocol
	{
		var containerIdx = BeamoManifest.ServiceDefinitions.FindIndex(container => container.BeamoId == beamoId);
		var foundContainer = containerIdx != -1;

		if (foundContainer)
			await remoteProtocolModifier(BeamoManifest.ServiceDefinitions[containerIdx], BeamoManifest.ServiceDefinitions[containerIdx].RemoteProtocol as TRemote).WaitAsync(cancellationToken);

		return foundContainer;
	}

	/// <summary>
	/// Resets the protocol data for the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> to the default settings. 
	/// </summary>
	public async Task<bool> ResetServiceToDefaultValues(string beamoId)
	{
		var localUpdated = await TryUpdateLocalProtocol<HttpMicroserviceLocalProtocol>(beamoId, PrepareDefaultLocalProtocol_HttpMicroservice, CancellationToken.None);
		var remoteUpdated = await TryUpdateRemoteProtocol<HttpMicroserviceRemoteProtocol>(beamoId, PrepareDefaultRemoteProtocol_HttpMicroservice, CancellationToken.None);
		return localUpdated && remoteUpdated;
	}


	/// <summary>
	/// Uses the given image (name or id) to create/replace the container with the given name and configurations.
	/// Returns whether or not the container successfully started. It DOES NOT guarantee the app inside the container is running correctly. 
	/// </summary>
	public async Task<bool> CreateAndRunContainer(string image, string containerName,
		DockerHealthConfig healthConfig,
		List<DockerPortBinding> portBindings,
		List<DockerVolume> volumes,
		List<DockerBindMount> bindMounts,
		List<DockerEnvironmentVariable> environmentVars)
	{
		var existingInstance = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.ContainerName.Contains(containerName));
		if (existingInstance != null)
		{
			if (existingInstance.IsRunning)
				return true;


			return await RunContainer(containerName);
		}

		_ = await CreateContainer(image, containerName, healthConfig, portBindings, volumes, bindMounts, environmentVars);
		var didRun = await RunContainer(containerName);
		_ = await _client.Containers.InspectContainerAsync(containerName);
		return didRun;
	}

	/// <summary>
	/// Given a container id, starts the container and returns whether or not the container successfully started. It DOES NOT guarantee the app inside the container is running correctly. 
	/// </summary>
	public async Task<bool> RunContainer(string containerId)
	{
		return await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
	}

	/// <summary>
	/// Runs an image (<see cref="BuildAndCreateImage"/> or <see cref="PullAndCreateImage"/>) with the given parameters. 
	/// </summary>
	/// <param name="containerName">The name for the given container (we map this to the container id).</param>
	/// <param name="portBindings">The port bindings between the container and the local machine.</param>
	/// <param name="volumes">Any volumes you wish to create/bind the container to.</param>
	/// <param name="bindMounts">Any external directories you want to make available to the container.</param>
	/// <param name="environmentVars">All Environment Variables you'll be running the container with.</param>
	public async Task<string> CreateContainer(string image, string containerName,
		DockerHealthConfig healthConfig,
		List<DockerPortBinding> portBindings,
		List<DockerVolume> volumes,
		List<DockerBindMount> bindMounts,
		List<DockerEnvironmentVariable> environmentVars)
	{
		// Prepare the host config and the create container params
		var createParams = new CreateContainerParameters();
		var hostConfig = new HostConfig();

		// Set the host config that'll be sent along
		createParams.HostConfig = hostConfig;

		// Prepares the create params header data 
		{
			createParams.Name = containerName;
			createParams.Image = image;
		}

		// Build container health check
		{
			var reqTimeout = healthConfig.HealthRequestTimeout;
			var waitRetryMax = healthConfig.MaximumSecondsBetweenRetries;
			var tries = healthConfig.NumberOfRetries;

			var port = healthConfig.InContainerPort;
			var endpoint = healthConfig.InContainerEndpoint;

			var pipeCmd = healthConfig.StopContainerWhenUnhealthy ? "kill" : "exit";

			var cmdStr = $"wget -O- -q --timeout={reqTimeout} --waitretry={waitRetryMax} --tries={tries} http://localhost:{port}/{endpoint} || {pipeCmd} 1";
			createParams.Healthcheck = new HealthConfig() { Test = new List<string>() { cmdStr } };

			hostConfig.AutoRemove = healthConfig.AutoRemoveContainerWhenStopped;
		}

		// Build env vars
		{
			BeamoServiceDefinition.BuildEnvVars(out var builtEnv, environmentVars);
			createParams.Env = builtEnv;
		}

		// Build BindMounts and Volumes: https://stackoverflow.com/a/58916037
		{
			BeamoServiceDefinition.BuildVolumes(volumes, out var boundVolumes);
			BeamoServiceDefinition.BuildBindMounts(bindMounts, out var boundMounts);
			var allBinds = new List<string>(boundVolumes.Count + boundMounts.Count);
			allBinds.AddRange(boundVolumes);
			allBinds.AddRange(boundMounts);
			hostConfig.Binds = allBinds;
		}

		// Get exposed ports and port bindings
		{
			BeamoServiceDefinition.BuildExposedPorts(portBindings, out var exposedPorts);
			BeamoServiceDefinition.BuildHostPortBinding(portBindings, out var boundPorts);
			createParams.ExposedPorts = exposedPorts;
			hostConfig.PortBindings = boundPorts;
			hostConfig.PublishAllPorts = true;
		}

		// I think this is the RM flag
		{
			hostConfig.ConsoleSize = new ulong[] { 28, 265 };
		}

		try
		{
			createParams.StopTimeout = TimeSpan.FromMilliseconds(10000);
			var response = await _client.Containers.CreateContainerAsync(createParams);
			return response.ID;
		}
		catch (Exception e)
		{
			BeamableLogger.LogError(JsonConvert.SerializeObject(e, Formatting.Indented));
			throw;
		}
	}

	/// <summary>
	/// Based on whether or not we have a base image defined in <see cref="BeamoServiceDefinition.BaseImage"/>, either pull that image or build the image locally with
	/// <see cref="BeamoServiceDefinition.DockerBuildContextPath"/> and <see cref="BeamoServiceDefinition.RelativeDockerfilePath"/>.  
	/// </summary>
	/// <returns>The image id that was created/pulled.</returns>
	public async Task<string> PrepareBeamoServiceImage(BeamoServiceDefinition serviceDefinition, Action<JSONMessage> messageHandler)
	{
		var shouldPull = !string.IsNullOrEmpty(serviceDefinition.BaseImage);

		if (shouldPull)
			serviceDefinition.ImageId = await PullAndCreateImage(serviceDefinition.BaseImage, messageHandler);
		else
			serviceDefinition.ImageId = await BuildAndCreateImage(serviceDefinition.BeamoId,
				serviceDefinition.DockerBuildContextPath,
				serviceDefinition.RelativeDockerfilePath,
				messageHandler);

		return serviceDefinition.ImageId;
	}

	/// <summary>
	/// Builds an image with the local docker engine using the given <paramref name="dockerBuildContextPath"/>, <paramref name="imageName"/> and dockerfile (<paramref name="dockerfilePathInBuildContext"/>).
	/// It inspects the created image and returns it's ID.
	/// </summary>
	public async Task<string> BuildAndCreateImage(string imageName, string dockerBuildContextPath, string dockerfilePathInBuildContext, Action<JSONMessage> progressUpdateHandler,
		string containerImageTag = "latest")
	{
		using (var stream = CreateTarballForDirectory(dockerBuildContextPath))
		{
			var tag = $"{imageName}:{containerImageTag}";
			await _client.Images.BuildImageFromDockerfileAsync(
				new ImageBuildParameters { Tags = new[] { tag }, Dockerfile = dockerfilePathInBuildContext, Labels = new Dictionary<string, string>() { { "beamoId", imageName } } },
				stream,
				null,
				new Dictionary<string, string>(),
				new Progress<JSONMessage>(message => progressUpdateHandler?.Invoke(message)));

			var builtImage = await _client.Images.InspectImageAsync(tag);
			return builtImage.ID;
		}
	}

	/// <summary>
	/// Pulls the image with the given <paramref name="imageName"/>:<paramref name="imageTag"/> into the local docker engine from remote docker repositories.
	/// It inspects the pulled image and returns its id, after the pull is done.
	/// </summary>
	public async Task<string> PullAndCreateImage(string publicImageName, Action<JSONMessage> progressUpdateHandler)
	{
		await _client.Images.CreateImageAsync(new ImagesCreateParameters() { FromImage = publicImageName, }, null, new Progress<JSONMessage>(progressUpdateHandler));
		var builtImage = await _client.Images.InspectImageAsync(publicImageName);
		return builtImage.ID;
	}

	/// <summary>
	/// Creates a tarball stream containing every file in the given <paramref name="directory"/>. 
	/// </summary>
	private static Stream CreateTarballForDirectory(string directory)
	{
		var tarball = new MemoryStream(512 * 1024);
		var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

		using var archive = new TarOutputStream(tarball, Encoding.Default)
		{
			//Prevent the TarOutputStream from closing the underlying memory stream when done
			IsStreamOwner = false
		};

		// Get every file in the given directory
		foreach (var file in files)
		{
			// Open the file we're putting into the tarball
			using var fileStream = File.OpenRead(file);

			// When creating the tar file (using SharpZipLib) if I create the tar entries from the filenames,
			// the tar will be created with the wrong slashes (\ instead of / for linux).
			// Swapping those out myself if you're doing any COPY or ADD within folders.
			var tarName = file.Substring(directory.Length).Replace('\\', '/').TrimStart('/');

			//Let's create the entry header
			var entry = TarEntry.CreateTarEntry(tarName);

			entry.Size = fileStream.Length;
			archive.PutNextEntry(entry);

			//Now write the bytes of data
			byte[] localBuffer = new byte[32 * 1024];
			while (true)
			{
				int numRead = fileStream.Read(localBuffer, 0, localBuffer.Length);
				if (numRead <= 0)
					break;

				archive.Write(localBuffer, 0, numRead);
			}

			//Nothing more to do with this entry
			archive.CloseEntry();
		}

		// Finish writing to the tarball
		archive.Close();

		//Reset the stream and return it
		tarball.Position = 0;
		return tarball;
	}

	/// <summary>
	/// Deletes all containers and images related to the given <paramref name="beamoId"/>.
	/// TODO: Track down every other service that depends on this one and shut them down before hand.
	/// </summary>
	public async Task CleanUpDocker(string beamoId)
	{
		var serviceDefinition = BeamoManifest.ServiceDefinitions.FirstOrDefault(sd => sd.BeamoId == beamoId);
		if (serviceDefinition != null) await CleanUpDocker(serviceDefinition);
	}

	/// <summary>
	/// Deletes all containers and images related to the given <see cref="BeamoServiceDefinition"/>.
	/// TODO: Track down every other service that depends on this one and shut them down before hand.
	/// </summary>
	public async Task CleanUpDocker(BeamoServiceDefinition serviceDefinition)
	{
		var beamoId = serviceDefinition.BeamoId;

		// Delete the containers and remote the service instances mappings
		var existingContainers = BeamoRuntime.ExistingLocalServiceInstances.Where(si => si.BeamoId == beamoId).ToList();
		await Task.WhenAll(existingContainers.Select(si => DeleteContainer(si.ContainerName)));

		BeamoRuntime.ExistingLocalServiceInstances.RemoveAll(si => si.BeamoId == beamoId);

		// Delete the image and update the image id
		var hasImage = !string.IsNullOrEmpty(serviceDefinition.ImageId);
		if (hasImage)
		{
			await DeleteImage(beamoId);
			serviceDefinition.ImageId = "";
		}
	}

	/// <summary>
	/// Deletes the image with the given name/tag or ImageId.
	/// </summary>
	public Task DeleteImage(string imageName) => _client.Images.DeleteImageAsync(imageName, new ImageDeleteParameters() { NoPrune = false, Force = true }, CancellationToken.None);

	/// <summary>
	/// Deletes a container with the given <paramref name="containerId"/> (or container name).
	/// </summary>
	public Task DeleteContainer(string containerId) =>
		_client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters() { RemoveVolumes = false, Force = true, RemoveLinks = false });
}

public class BeamoLocalManifest
{
	// TODO : Need to change this to work without polymorphism if we want to send this "as is" to server --- but... the more I think about this, the more I think we shouldn't...
	public List<BeamoServiceDefinition> ServiceDefinitions;

	/// <summary>
	/// Built out of the <see cref="ServiceDefinitions"/>, each sub-array contains all the image dependencies for that particular layer. 
	/// </summary>
	public int[][] LayeredDependencyGraph;
}

public class BeamoServiceDefinition
{
	public string BeamoId;
	public string[] DependsOnBeamoIds;

	/// <summary>
	/// This is for local and development things
	/// </summary>
	public string DockerBuildContextPath;

	/// <summary>
	/// This is for local and development things
	/// </summary>
	public string RelativeDockerfilePath;

	/// <summary>
	/// If this is set, we pull this image from the public docker registry instead of building locally.
	/// </summary>
	public string BaseImage;

	/// <summary>
	/// This is what we need for deployment.
	/// </summary>
	public string ImageId;


	// I want this out of here 😫
	public BeamoProtocolType Protocol;
	public IBeamoLocalProtocol LocalProtocol;
	public IBeamoRemoteProtocol RemoteProtocol;

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

public struct DockerHealthConfig
{
	/// <summary>
	/// Whether or not the container should be thrown away after being stopped.
	/// </summary>
	public bool AutoRemoveContainerWhenStopped;

	/// <summary>
	/// Whether or not the container should be kept running after a failure. 
	/// </summary>
	public bool StopContainerWhenUnhealthy;

	/// <summary>
	/// The maximum number of attempts before we'll accept that the healthcheck failed.
	/// </summary>
	public int NumberOfRetries;

	/// <summary>
	/// The number of seconds to wait between each retries (starts at 1, for each retry increases by 1 up to this value).
	/// </summary>
	public int MaximumSecondsBetweenRetries;

	/// <summary>
	/// The timeout for each individual attempt in seconds. 
	/// </summary>
	public int HealthRequestTimeout;

	/// <summary>
	/// Port for the HealthCheckConfig.
	/// </summary>
	public string InContainerPort;

	/// <summary>
	/// Route without preceding or trailing forward slashes. 
	/// </summary>
	public string InContainerEndpoint;
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
