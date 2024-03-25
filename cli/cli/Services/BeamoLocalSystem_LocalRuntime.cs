/**
 * This part of the class has a bunch of utility functions to handle managing running containers of Local Beamo Services.
 * It handles the way we map BeamoServiceDefinitions to BeamoServiceInstances and how those instances map to individual local containers.
 */

using Beamable.Common;
using Docker.DotNet.Models;
using Serilog;
using System.Text;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	/// <summary>
	/// Forces a synchronization between the current Docker Daemon state and the given BeamoServiceInstance lists. It:
	/// <list type="bullet">
	/// <item>Tries to access each image corresponding to a <see cref="BeamoServiceDefinition"/>, if none exists, clear <see cref="BeamoServiceDefinition.ImageId"/>. If exists, ensures it matches it.</item>
	/// <item>Removes all <see cref="BeamoServiceInstance"/>s whose <see cref="BeamoServiceInstance.ContainerName"/>s don't exist in the list of existing containers we get from docker.</item>
	/// <item>Adds a <see cref="BeamoServiceInstance"/> to the list for each container using an Image whose image id matches a <see cref="BeamoServiceDefinition.ImageId"/>.</item>
	/// <item>Updates the running/not running state of each <see cref="BeamoServiceInstance"/>.</item>
	/// </list> 
	/// </summary>
	/// <param name="serviceDefinitions">The list of all service definitions we care about synchronizing.</param>
	/// <param name="existingServiceInstances">The list it should update with the running <see cref="BeamoServiceInstance"/>.</param>
	public async Task SynchronizeInstanceStatusWithDocker(BeamoLocalManifest manifest,
		List<BeamoServiceInstance> existingServiceInstances)
	{
		var serviceDefinitions = manifest.ServiceDefinitions;
		// Make sure we know about all images that match our beamo ids and make sure all image ids that we know about are still there. 
		foreach (var sd in serviceDefinitions)
		{
			try
			{
				string imageToInspect;
				switch (sd.Protocol)
				{
					case BeamoProtocolType.EmbeddedMongoDb:
						imageToInspect = manifest.EmbeddedMongoDbLocalProtocols[sd.BeamoId].BaseImage;
						break;
					case BeamoProtocolType.HttpMicroservice:
						imageToInspect = sd.BeamoId;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				var inspectResponse = await _client.Images.InspectImageAsync(imageToInspect.ToLower());
				sd.ImageId = inspectResponse.ID;
			}
			catch
			{
				// We only clear image ids if we know we can rebuild them.
				if (VerifyCanBeBuiltLocally(manifest, sd))
					sd.ImageId = "";
			}
		}

		IList<ContainerListResponse> allLocalContainers;

		try
		{
			allLocalContainers =
				await _client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });
		}
		catch (Exception e)
		{
			BeamableLogger.LogError($"Failed, Docker message: {e.Message}");
			throw;
		}

		// Remove all service instances that no longer exist
		existingServiceInstances.RemoveAll(
			si => allLocalContainers.Count(dc => dc.Names.Contains(si.ContainerName)) < 1);

		// For all containers that still exist and any new ones
		foreach (var dockerContainer in allLocalContainers)
		{
			// First, try to match the container by name...
			var beamoId = serviceDefinitions
				.FirstOrDefault(c => dockerContainer.Names[0].Split('_')[0].EndsWith(c.BeamoId))?.BeamoId;

			// If failed to fine, match it by image id.
			if (string.IsNullOrEmpty(beamoId))
				beamoId = serviceDefinitions.FirstOrDefault(c => c.ImageId == dockerContainer.ImageID)?.BeamoId;

			if (string.IsNullOrEmpty(beamoId))
			{
				// BeamableLogger.LogWarning($"Skipping container [{dockerContainer.ID}] because no ImageId matched this containers Image={dockerContainer.Image} or ImageId={dockerContainer.ImageID}");
				continue;
			}

			var containerName = dockerContainer.Names[0].Substring(1);
			var containerId = dockerContainer.ID;
			var isRunning = dockerContainer.State == "running";
			var ports = dockerContainer.Ports.Select(p =>
				new DockerPortBinding()
				{
					InContainerPort = p.PrivatePort.ToString(),
					LocalPort = p.PublicPort.ToString()
				}).ToList();

			// If the container NOT already mapped to an existing BeamoServiceInstance, we map it. 
			var existingInstance = existingServiceInstances.FirstOrDefault(si => si.ContainerId == containerId);
			if (existingInstance == null)
			{
				existingServiceInstances.Add(new BeamoServiceInstance
				{
					BeamoId = beamoId,
					ImageId = dockerContainer.ImageID,
					ContainerId = containerId,
					ContainerName = containerName,
					ActivePortBindings = ports,
					IsRunning = isRunning
				});
			}
			// If it is already mapped, we update the BeamoServiceInstance.
			else
			{
				existingInstance.BeamoId = beamoId;
				existingInstance.ContainerName = containerName;
				existingInstance.IsRunning = isRunning;
				existingInstance.ActivePortBindings = ports;
			}
		}

		// TODO: Detect when people containers are running but their dependencies are not. Output a list of warnings that people can then print out.
	}

	/// <summary>
	/// Kick off a long running task that receives updates from the docker engine.
	/// </summary>
	public Task StartListeningToDocker(Action<string, string> onServiceContainerStateChange = null)
	{
		return StartListeningToDockerRaw((beamoId, action, _) =>
			onServiceContainerStateChange?.Invoke(beamoId, action));
	}

	/// <summary>
	/// Kick off a long running task that receives updates from the docker engine.
	/// </summary>
	public async Task StartListeningToDockerRaw(Action<string, string, Message> onServiceContainerStateChange = null)
	{
		// Cancel the thread if it's already running.
		if (_dockerListeningThread != null && !_dockerListeningThread.IsCompleted)
			_dockerListeningThreadCancel.Cancel();

		
		var currentInfo = await _client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });

		foreach (var info in currentInfo)
		{
			var beamoId = BeamoManifest.ServiceDefinitions.FirstOrDefault(c => info.Names[0].Contains(c.BeamoId))
				?.BeamoId;

			if (!string.IsNullOrEmpty(beamoId))
			{
				var beamoServiceInstance =
					BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.BeamoId == beamoId);
				
				if (beamoServiceInstance != null)
				{
					beamoServiceInstance.IsRunning = false;
					var message = new Message() { ID = info.ID };
					onServiceContainerStateChange?.Invoke(beamoId, "start", message);
				}
			}
		}	
			
		// Start the long running task. We don't "await" this task as it never yields until it's cancelled.
		_dockerListeningThread = _client.System.MonitorEventsAsync(new ContainerEventsParameters(),
			new Progress<Message>(DockerSystemEventHandler), _dockerListeningThreadCancel.Token);
		// We await this instead for API consistency...
		await Task.CompletedTask;

		// This is the actual handler that updates the state according to the event message.
		// Since Docker API docs are not the best. I recommend using JsonConvert.SerializeObject and print out the object that their APIs return to debug or add to any of this stuff.
		async void DockerSystemEventHandler(Message message)
		{
			var messageType = message.Type;
			var messageAction = message.Action;
			Log.Verbose($"Docker Message type=[{messageType}] action=[{messageAction}] id=[{message.ID}]");

			switch (messageType, messageAction)
			{
				case ("container", "create"):
				{
					// Find the beamoId tied to the image that was used to create the container
					var beamoId = BeamoManifest.ServiceDefinitions.FirstOrDefault(c => message.Actor.Attributes["name"].Contains(c.BeamoId))
						?.BeamoId;
					if (!string.IsNullOrEmpty(beamoId))
					{
						var containerData = (await _client.Containers.InspectContainerAsync(message.ID));
						var containerName = containerData.Name.Substring(1);
						var ports = containerData.HostConfig.PortBindings.Select(kvp =>
						{
							var inContainerPort = kvp.Key.Split("/")[0];
							var localPort = kvp.Value.First().HostPort;
							return new DockerPortBinding { LocalPort = localPort, InContainerPort = inContainerPort };
						}).ToList();

						var newServiceInstance = new BeamoServiceInstance()
						{
							ImageId = containerData.Image,
							BeamoId = beamoId,
							ContainerId = message.ID,
							ContainerName = containerName,
							ActivePortBindings = ports,
							IsRunning = false
						};
						BeamoRuntime.ExistingLocalServiceInstances.Add(newServiceInstance);
						onServiceContainerStateChange?.Invoke(beamoId, messageAction, message);
					}

					break;
				}

				case ("container", "destroy"):
				{
					BeamoRuntime.ExistingLocalServiceInstances.RemoveAll(si =>
					{
						var wasDestroyed = message.Actor.Attributes["name"].Contains(si.BeamoId);
						if (wasDestroyed)
						{
							si.IsRunning = false;
							onServiceContainerStateChange?.Invoke(si.BeamoId, messageAction, message);
						}

						return wasDestroyed;
					});
					break;
				}

				case ("container", "start"):
				{
					var beamoServiceInstance =
						BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => message.Actor.Attributes["name"].Contains(si.BeamoId));
					if (beamoServiceInstance != null)
					{
						beamoServiceInstance.IsRunning = true;
						onServiceContainerStateChange?.Invoke(beamoServiceInstance.BeamoId, messageAction, message);
					}

					// TODO: Detect when people containers are running but their dependencies are not. Output a list of warnings that people can then print out.
					break;
				}
			}
		}
	}

	/// <summary>
	/// Cancel the long-running thread created by <see cref="StartListeningToDocker"/>.
	/// </summary>
	public async Task StopListeningToDocker()
	{
		_dockerListeningThreadCancel.Cancel();
		await Task.CompletedTask;
	}

	/// <summary>
	/// Short hand to check if a service is running or not.
	/// </summary>
	public bool IsBeamoServiceRunningLocally(string beamoId)
	{
		var si = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.BeamoId == beamoId);
		return si != null && si.IsRunning;
	}

	/// <summary>
	/// Short hand to check if a specific service's container is running or not.
	/// </summary>
	public bool IsBeamoServiceRunningLocally(string beamoId, string containerName)
	{
		var si = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si =>
			si.BeamoId == beamoId && si.ContainerName == containerName);
		return si != null && si.IsRunning;
	}

	/// <summary>
	/// Checks if we have the data we need to build the given Beam-O service. Returns true if we can; false, otherwise.
	/// </summary>
	public bool VerifyCanBeBuiltLocally(BeamoServiceDefinition beamoServiceDefinition) =>
		VerifyCanBeBuiltLocally(BeamoManifest, beamoServiceDefinition);

	/// <summary>
	/// <inheritdoc cref="VerifyCanBeBuiltLocally(cli.Services.BeamoServiceDefinition)"/>.
	/// </summary>
	public bool VerifyCanBeBuiltLocally(string beamoServiceId) => VerifyCanBeBuiltLocally(BeamoManifest,
		BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == beamoServiceId));

	/// <summary>
	/// <inheritdoc cref="VerifyCanBeBuiltLocally(cli.BeamoServiceDefinition)"/>.
	/// </summary>
	// public static bool VerifyCanBeBuiltLocally(BeamoLocalManifest manifest, string beamoId)
	// {
	// 	var toCheck = manifest.ServiceDefinitions.First(sd => sd.BeamoId == beamoId);
	// 	return VerifyCanBeBuiltLocally(manifest, toCheck);
	// }

	/// <summary>
	/// <inheritdoc cref="VerifyCanBeBuiltLocally(BeamoServiceDefinition)"/>.
	/// </summary>
	private bool VerifyCanBeBuiltLocally(BeamoLocalManifest manifest, BeamoServiceDefinition toCheck)
	{
		IBeamoLocalProtocol protocol = toCheck.Protocol switch
		{
			BeamoProtocolType.HttpMicroservice => manifest.HttpMicroserviceLocalProtocols[toCheck.BeamoId],
			BeamoProtocolType.EmbeddedMongoDb => manifest.EmbeddedMongoDbLocalProtocols[toCheck.BeamoId],
			_ => throw new ArgumentOutOfRangeException()
		};

		return protocol.VerifyCanBeBuiltLocally(_configService);
	}

	/// <summary>
	/// Check which service definitions can be deployed, those that are enabled and still exists.
	/// </summary>
	/// <param name="localManifest">The current local manifest with both http and storage services definitions</param>
	/// <param name="beamoIds">A list of services names that are going to be checked, if null all the services defined in the local manifest
	/// will be checked instead.</param>
	/// <returns></returns>
	public List<BeamoServiceDefinition> GetServiceDefinitionsThatCanBeDeployed(BeamoLocalManifest localManifest,
		string[] beamoIds = null)
	{
		beamoIds ??= localManifest.ServiceDefinitions.Select(c => c.BeamoId).ToArray();

		return beamoIds
			.Select(reqId => localManifest.ServiceDefinitions.First(sd => sd.BeamoId == reqId))
			.Where(VerifyCanBeBuiltLocally)
			.ToList();
	}

	/// <summary>
	/// Using the given <paramref name="localSystem"/>, builds and deploys all services with the given <paramref name="deployBeamoIds"/> to the local docker engine.
	/// If <paramref name="deployBeamoIds"/> is null, will deploy ALL services. Also, this does check for cyclical dependencies before running the deployment.
	/// </summary>
	public async Task DeployToLocal(BeamoLocalSystem localSystem, string[] deployBeamoIds = null,
		bool forceAmdCpuArchitecture = false, Action<string, float> buildPullImageProgress = null,
		Action<string> onServiceDeployCompleted = null)
	{
		var localManifest = localSystem.BeamoManifest;
		// Get all services that must be deployed (and that are not just known remotely --- as in, have their local protocols correctly configured).
		var serviceDefinitionsToDeploy = GetServiceDefinitionsThatCanBeDeployed(localManifest, deployBeamoIds);

		// Guarantee they each don't have cyclical dependencies.
		{
			var dependencyChecksForServicesToDeploy = await Task.WhenAll(serviceDefinitionsToDeploy.Select(sd =>
			{
				return Task.Run(() =>
					ValidateBeamoService_NoCyclicalDependencies(sd, localManifest.ServiceDefinitions));
			}));


			var indexOfServiceWithCyclicalDependency = dependencyChecksForServicesToDeploy.ToList().IndexOf(false);
			if (indexOfServiceWithCyclicalDependency != -1)
				throw new Exception(
					$"{serviceDefinitionsToDeploy[indexOfServiceWithCyclicalDependency].BeamoId} has cyclical dependencies!");
		}


		// Builds all images for all services that are defined and can be built locally.

		var prepareImages = new List<Task>(serviceDefinitionsToDeploy.Select(c => PrepareBeamoServiceImage(c, buildPullImageProgress, forceAmdCpuArchitecture)));
		await Task.WhenAll(prepareImages);

		var servicesDependencies = await localSystem.GetAllBeamoIdsDependencies();
		// Build dependency layers split by protocol type.
		SplitLayersByProtocolType(serviceDefinitionsToDeploy, servicesDependencies, out var builtLayers);

		// For each layer, run through the containers of each type on that layer and do what is needed to deploy them
		// We already know that all containers are properly built here, so we just need to create the containers and run them.
		foreach (var builtLayer in builtLayers)
		{
			var runContainerTasks = new List<Task>();

			// Kick off all the run container tasks for the Embedded MongoDatabases in this layer
			if (builtLayer.TryGetValue(BeamoProtocolType.EmbeddedMongoDb, out var microStorageContainers))
				runContainerTasks.AddRange(microStorageContainers.Select(async sd =>
				{
					Log.Information("Started deploying service: " + sd.BeamoId);
					await RunLocalEmbeddedMongoDb(sd, localManifest.EmbeddedMongoDbLocalProtocols[sd.BeamoId]);
					Log.Information("Finished deploying service: " + sd.BeamoId);
					onServiceDeployCompleted?.Invoke(sd.BeamoId);
				}));

			// Kick off all the run container tasks for the HTTP Microservices in this layer
			if (builtLayer.TryGetValue(BeamoProtocolType.HttpMicroservice, out var microserviceContainers))
				runContainerTasks.AddRange(microserviceContainers.Select(async sd =>
				{
					await RunLocalHttpMicroservice(sd, localManifest.HttpMicroserviceLocalProtocols[sd.BeamoId],
						localSystem);
					onServiceDeployCompleted?.Invoke(sd.BeamoId);
				}));


			// Wait for all container tasks in this layer to finish before starting the next one.
			await Task.WhenAll(runContainerTasks);
		}
	}

	/// <summary>
	/// Given a Directed Acyclic Graph of <paramref name="serviceDefinitions"/>, builds a dictionary for each of the graph's layers. This dictionary splits the services in each layer by their
	/// <see cref="BeamoProtocolType"/>. 
	/// </summary>
	private static void SplitLayersByProtocolType(List<BeamoServiceDefinition> serviceDefinitions,
		Dictionary<BeamoServiceDefinition, List<DependencyData>> serviceDependencies,
		out Dictionary<BeamoProtocolType, List<BeamoServiceDefinition>>[] splitContainers)
	{
		// Builds the dependency layers
		BuildLayeredDependencies(serviceDefinitions, serviceDependencies, out var layers);

		// Split each layer by their BeamoProtocolType
		splitContainers = new Dictionary<BeamoProtocolType, List<BeamoServiceDefinition>>[layers.Length];
		for (var layerIdx = 0; layerIdx < layers.Length; layerIdx++)
		{
			var builtLayer = layers[layerIdx];
			var perProtocolSplit = builtLayer
				.GroupBy(i => serviceDefinitions[i].Protocol)
				.ToDictionary(g => g.Key, g => g.Select(i => serviceDefinitions[i]).ToList());

			splitContainers[layerIdx] = perProtocolSplit;
		}
	}

	/// <summary>
	/// Topological sorting of the dependency graph. Basically, this returns layers of dependencies. Generates layers by finding all 0-dependency services and expanding from them.
	/// </summary>
	/// <param name="serviceDefinitions">The Directed Acyclic Graph of <see cref="BeamoServiceDefinition"/>s.</param>
	/// <param name="builtLayers">An array of layers, each containing indices into <paramref name="serviceDefinitions"/> for services in that layer.</param>
	private static void BuildLayeredDependencies(List<BeamoServiceDefinition> serviceDefinitions,
		Dictionary<BeamoServiceDefinition, List<DependencyData>> serviceDependencies, out int[][] builtLayers)
	{
		// Find the layers with 0 dependencies
		var currentLayerDefinitions = serviceDefinitions.Where(c => serviceDependencies[c].Count == 0).ToList();
		var seen = new HashSet<BeamoServiceDefinition>();

		var layers = new List<int[]>();
		// While we haven't seen everything
		var allCompletedDependencies = new HashSet<int>();
		while (!serviceDefinitions.TrueForAll(sd => seen.Contains(sd)))
		{
			// Makes a layer out of all the nodes in the current layer --- when we start, this is all the nodes with 0 dependencies.
			// In further iterations of this loop, it'll contain all service definitions whose dependencies are in previous layers AND haven't been added before. 
			var currLayer = currentLayerDefinitions.Select(sd => serviceDefinitions.IndexOf(sd)).ToArray();
			layers.Add(currLayer);

			// Updates the set of seen service definitions so we know when to break out of this loop
			seen.UnionWith(currentLayerDefinitions);

			// Updates the list of all completed dependencies so that we can search for nodes in the next layer.
			allCompletedDependencies.UnionWith(layers.SelectMany(ints => ints));

			// Go through all the service definitions and find the next layer of service definitions based on all the completed dependencies.
			currentLayerDefinitions.Clear();
			foreach (var sd in serviceDefinitions)
			{
				// Check that all the dependencies are in previous layers
				var isInNextLayer = serviceDependencies[sd].TrueForAll(depBeamoId =>
				{
					var dependencyIdx = serviceDefinitions.FindIndex(sd2 => sd2.BeamoId == depBeamoId.name);
					return allCompletedDependencies.Contains(dependencyIdx);
				});


				// If they are, and we haven't expanded them already, we can have them in the next layer.  
				if (isInNextLayer && !seen.Contains(sd))
					currentLayerDefinitions.Add(sd);
			}
		}

		// Build them into a list of indices into the unsorted list of containers (the parameter list)
		builtLayers = layers.ToArray();
	}

	public async IAsyncEnumerable<string> TailLogs(string containerId)
	{
		// _client.Containers.GetContainerLogsAsync()
#pragma warning disable CS0618
		var stream = await _client.Containers.GetContainerLogsAsync(containerId,
			new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Follow = true, });
#pragma warning restore CS0618

		// stream.
		if (stream == null)
		{
			yield break;
		}

		using var reader = new StreamReader(stream, Encoding.UTF8);
		while (!reader.EndOfStream)
		{
			var line = await reader.ReadLineAsync();
			if (line?.Length > 8)
				yield return line[8..]; // substring 8 because of a strange encoding issue in docker dotnet.
		}
	}
}

public class BeamoLocalRuntime
{
	public List<BeamoServiceInstance> ExistingLocalServiceInstances;
}

public class BeamoServiceInstance : IEquatable<BeamoServiceInstance>
{
	public string BeamoId;
	public string ContainerId;
	public string ContainerName;
	public string ImageId;
	public List<DockerPortBinding> ActivePortBindings;

	public bool IsRunning;

	public bool Equals(BeamoServiceInstance other) => ContainerId == other.ContainerId;

	public override bool Equals(object obj) => obj is BeamoServiceInstance other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(ContainerId);
}
