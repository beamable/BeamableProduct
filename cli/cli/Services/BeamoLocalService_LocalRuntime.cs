using Beamable.Common;
using Docker.DotNet.Models;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace cli;

public partial class BeamoLocalService
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
	public async Task SynchronizeInstanceStatusWithDocker(List<BeamoServiceDefinition> serviceDefinitions, List<BeamoServiceInstance> existingServiceInstances)
	{
		// Make sure we know about all images that match our beamo ids and make sure all image ids that we know about are still there. 
		foreach (var sd in serviceDefinitions)
		{
			try
			{
				var inspectResponse = await _client.Images.InspectImageAsync(sd.BeamoId);
				sd.ImageId = inspectResponse.ID;
			}
			catch
			{
				sd.ImageId = "";
			}
		}

		var allLocalContainers = await _client.Containers.ListContainersAsync(new ContainersListParameters() { All = true });

		// Remove all service instances that no longer exist
		existingServiceInstances.RemoveAll(si => allLocalContainers.Count(dc => dc.Names.Contains(si.ContainerName)) < 1);

		// For all containers that still exist and any new ones
		foreach (var dockerContainer in allLocalContainers)
		{
			// Check to see if it is a container using a BeamoService image --- skip if not.
			var beamoId = serviceDefinitions.FirstOrDefault(c => c.ImageId == dockerContainer.ImageID)?.BeamoId;
			if (string.IsNullOrEmpty(beamoId))
			{
				BeamableLogger.LogWarning($"Skipping container [{dockerContainer.ID}] because no ImageId matched this containers Image={dockerContainer.Image} or ImageId={dockerContainer.ImageID}");
				continue;
			}

			var containerName = dockerContainer.Names[0].Substring(1);
			var containerId = dockerContainer.ID;
			var isRunning = dockerContainer.State == "running";
			var ports = dockerContainer.Ports.Select(p => new DockerPortBinding() { InContainerPort = p.PrivatePort.ToString(), LocalPort = p.PublicPort.ToString() }).ToList();

			// If the container NOT already mapped to an existing BeamoServiceInstance, we map it. 
			var existingInstance = existingServiceInstances.FirstOrDefault(si => si.ContainerId == containerId);
			if (existingInstance == null)
			{
				existingServiceInstances.Add(new BeamoServiceInstance
				{
					BeamoId = beamoId,
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
	}

	/// <summary>
	/// Kick off a long running task that receives updates from the docker engine.
	/// </summary>
	public async Task StartListeningToDocker()
	{
		// Cancel the thread if it's already running.
		if (_dockerListeningThread != null && !_dockerListeningThread.IsCompleted)
			_dockerListeningThreadCancel.Cancel();

		// Start the long running task. We don't "await" this task as it never yields until it's cancelled.
		_dockerListeningThread = _client.System.MonitorEventsAsync(new ContainerEventsParameters(), new Progress<Message>(DockerSystemEventHandler), _dockerListeningThreadCancel.Token);
		// We await this instead for API consistency...
		await Task.CompletedTask;

		// This is the actual handler that updates the state according to the event message.
		// Since Docker API docs are not the best. I recommend using JsonConvert.SerializeObject and print out the object that their APIs return to debug or add to any of this stuff.
		async void DockerSystemEventHandler(Message message)
		{
			var messageType = message.Type;
			var messageAction = message.Action;

			switch (messageType, messageAction)
			{
				case ("container", "create"):
				{
					// Find the beamoId tied to the image that was used to create the container
					var beamoId = BeamoManifest.ServiceDefinitions.FirstOrDefault(c => c.ImageId == message.From)?.BeamoId;
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
							BeamoId = beamoId,
							ContainerId = message.ID,
							ContainerName = containerName,
							ActivePortBindings = ports,
							IsRunning = false
						};
						BeamoRuntime.ExistingLocalServiceInstances.Add(newServiceInstance);
					}

					break;
				}

				case ("container", "destroy"):
				{
					BeamoRuntime.ExistingLocalServiceInstances.RemoveAll(si => si.ContainerId == message.ID);
					break;
				}

				case ("container", "start"):
				{
					var beamoServiceInstance = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.ContainerId == message.ID);
					if (beamoServiceInstance != null) beamoServiceInstance.IsRunning = true;
					break;
				}

				case ("container", "stop"):
				{
					var beamoServiceInstance = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.ContainerId == message.ID);
					if (beamoServiceInstance != null) beamoServiceInstance.IsRunning = false;
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
	public bool IsBeamoServiceRunning(string beamoId)
	{
		var si = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.BeamoId == beamoId);
		return si != null && si.IsRunning;
	}

	/// <summary>
	/// Short hand to check if a specific service's container is running or not.
	/// </summary>
	public bool IsBeamoServiceRunning(string beamoId, string containerName)
	{
		var si = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.BeamoId == beamoId && si.ContainerName == containerName);
		return si != null && si.IsRunning;
	}

	/// <summary>
	/// Using the given <paramref name="localManifest"/>, builds and deploys all services with the given <paramref name="deployBeamoIds"/> to the local docker engine.
	/// If <paramref name="deployBeamoIds"/> is null, will deploy ALL services. Also, this does check for cyclical dependencies before running the deployment.
	/// </summary>
	public async Task DeployToLocalClient(BeamoLocalManifest localManifest, string[] deployBeamoIds = null, Action<string> onServiceDeployCompleted = null, Action<JSONMessage> handler = null)
	{
		deployBeamoIds ??= localManifest.ServiceDefinitions.Select(c => c.BeamoId).ToArray();

		// Get all services that must be deployed
		var serviceDefinitionsToDeploy = deployBeamoIds.Select(reqId => localManifest.ServiceDefinitions.First(sd => sd.BeamoId == reqId)).ToList();

		// Guarantee they each don't have cyclical dependencies.
		{
			var dependencyChecksForServicesToDeploy = await Task.WhenAll(serviceDefinitionsToDeploy.Select(sd =>
			{
				return Task.Run(() => ValidateBeamoService_NoCyclicalDependencies(sd, localManifest.ServiceDefinitions));
			}));


			var indexOfServiceWithCyclicalDependency = dependencyChecksForServicesToDeploy.ToList().IndexOf(false);
			if (indexOfServiceWithCyclicalDependency != -1)
				throw new CliException($"{serviceDefinitionsToDeploy[indexOfServiceWithCyclicalDependency].BeamoId} has cyclical dependencies!");
		}


		// Builds all images for all services that are defined.
		{
			var prepareImages = new List<Task>();
			prepareImages.AddRange(localManifest.ServiceDefinitions.Select(c => PrepareBeamoServiceImage(c, handler)));
			await Task.WhenAll(prepareImages);
		}


		// Build dependency layers split by protocol type.
		SplitLayersByProtocolType(serviceDefinitionsToDeploy, out var builtLayers);

		// For each layer, run through the containers of each type on that layer and do what is needed to deploy them
		// We already know that all containers are properly built here, so we just need to create the containers and run them.
		foreach (var builtLayer in builtLayers)
		{
			var runContainerTasks = new List<Task>();

			// Kick off all the run container tasks for the HTTP Microservices in this layer
			if (builtLayer.TryGetValue(BeamoProtocolType.HttpMicroservice, out var microserviceContainers))
				runContainerTasks.AddRange(microserviceContainers.Select(async sd =>
				{
					await RunLocalHttpMicroservice(sd);
					onServiceDeployCompleted?.Invoke(sd.BeamoId);
				}));


			// Kick off all the run container tasks for the Embedded MongoDatabases in this layer
			if (builtLayer.TryGetValue(BeamoProtocolType.EmbeddedMongoDb, out var microStorageContainers))
			{
				foreach (var microStorageContainer in microStorageContainers)
				{
					// TODO Prepare Image to Run
				}
			}

			// Wait for all container tasks in this layer to finish before starting the next one.
			await Task.WhenAll(runContainerTasks);
		}
	}

	/// <summary>
	/// Given a Directed Acyclic Graph of <paramref name="serviceDefinitions"/>, builds a dictionary for each of the graph's layers. This dictionary splits the services in each layer by their
	/// <see cref="BeamoProtocolType"/>. 
	/// </summary>
	private static void SplitLayersByProtocolType(List<BeamoServiceDefinition> serviceDefinitions, out Dictionary<BeamoProtocolType, List<BeamoServiceDefinition>>[] splitContainers)
	{
		// Builds the dependency layers
		BuildLayeredDependencies(serviceDefinitions, out var layers);

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
	/// Topological sorting of the dependency graph. Basically, this returns layers of dependency counts. In acyclic graphs, which the service definitions must be, this
	/// guarantee's not dependency conflicts.
	/// </summary>
	/// <param name="serviceDefinitions">The Directed Acyclic Graph of <see cref="BeamoServiceDefinition"/>s.</param>
	/// <param name="builtLayers">An array of layers, each containing indices into <paramref name="serviceDefinitions"/> for services in that layer.</param>
	private static void BuildLayeredDependencies(List<BeamoServiceDefinition> serviceDefinitions, out int[][] builtLayers)
	{
		// Find the layers of dependency counts
		var layers = serviceDefinitions
			.GroupBy(c => c.DependsOnBeamoIds.Length).ToList();

		// Sort the layers by count (smallest number of dependencies first)
		layers.Sort((g1, g2) => g1.Key.CompareTo(g2.Key));

		// Build them into a list of indices into the unsorted list of containers (the parameter list)
		builtLayers = layers.Select(g => g.Select(c => serviceDefinitions.IndexOf(c)).ToArray()).ToArray();
	}
}

public class BeamoLocalRuntime
{
	/// <summary>
	/// BeamO Service Ids+Container Ids for all currently running BeamO services. Stored in the form: "beamoId₢containerId".
	/// Running means: the container is up AND the docker health check is responsive. 
	/// </summary>
	public List<BeamoServiceInstance> ExistingLocalServiceInstances;

	/// <summary>
	/// List of beam ids that the 
	/// </summary>
	public List<string> BeamoIdsToTryEnableOnRemoteDeploy;
}

public class BeamoServiceInstance : IEquatable<BeamoServiceInstance>
{
	public string BeamoId;
	public string ContainerId;
	public string ContainerName;
	public List<DockerPortBinding> ActivePortBindings;

	public bool IsRunning;

	public bool Equals(BeamoServiceInstance other) => ContainerId == other.ContainerId;

	public override bool Equals(object obj) => obj is BeamoServiceInstance other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(ContainerId);
}
