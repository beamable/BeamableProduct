using Beamable.Common;
using Docker.DotNet.Models;

namespace cli;

public partial class BeamoLocalService
{
	/// <summary>
	/// Forces a synchronization between the current Docker Daemon state and the given BeamoServiceInstance lists. It:
	/// <list type="bullet">
	/// <item>Removes all service instances whose <see cref="BeamoServiceInstance.ContainerId"/> have no corresponding container id in the list of existing containers we get from docker.</item>
	/// <item>Adds a <see cref="BeamoServiceInstance"/> to the list for each container using an Image whose image id matches a <see cref="BeamoServiceDefinition.ImageId"/>.</item>
	/// <item>Updates the running/not running state of each <see cref="BeamoServiceInstance"/>.</item>
	/// </list> 
	/// </summary>
	/// <param name="existingServiceInstances"></param>
	public async Task UpdateContainerStatusFromDocker(List<BeamoServiceInstance> existingServiceInstances)
	{
		// Make sure we know about all images that match our beamo ids and make sure all image ids that we know about are still there. 
		foreach (var sd in BeamoManifest.ServiceDefinitions)
		{
			try
			{
				BeamableLogger.LogWarning("aisdjoiajsdoiajodjasjdoias");
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
		existingServiceInstances.RemoveAll(si => allLocalContainers.Count(dc => dc.ID == si.ContainerId) < 1);

		// For all containers that still exist and any new ones
		foreach (var dockerContainer in allLocalContainers)
		{
			// Check to see if it is a container using a BeamoService image --- skip if not.
			var beamoId = BeamoManifest.ServiceDefinitions.FirstOrDefault(c => c.ImageId == dockerContainer.ImageID)?.BeamoId;
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
	/// 
	/// </summary>
	public async Task BeginListeningToDocker()
	{
		// Don't await this as the thread this spawns never yields until it's cancelled.
		_client.System.MonitorEventsAsync(new ContainerEventsParameters(), new Progress<Message>(DockerSystemEventHandler), _dockerListeningThreadCancel.Token);
		await Task.CompletedTask;

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

	public async Task StopListeningToDocker()
	{
		_dockerListeningThreadCancel.Cancel();
		await Task.CompletedTask;
	}


	public bool IsBeamoServiceRunning(string beamoId)
	{
		var si = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.BeamoId == beamoId);
		return si != null && si.IsRunning;
	}

	public bool IsBeamoServiceRunning(string beamoId, string containerName)
	{
		var si = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.BeamoId == beamoId && si.ContainerName == containerName);
		return si != null && si.IsRunning;
	}

	public async Task ResetServiceToDefaultValues(string beamoId)
	{
		var serviceDefinition = BeamoManifest.ServiceDefinitions.FirstOrDefault(sd => sd.BeamoId == beamoId);
		if (serviceDefinition != null)
		{
			await CleanUpDocker(serviceDefinition);

			switch (serviceDefinition.Protocol)
			{
				case BeamoProtocolType.HttpMicroservice:
					await PrepareDefaultLocalProtocol_HttpMicroservice(serviceDefinition, serviceDefinition.LocalProtocol as HttpMicroserviceLocalProtocol);
					await PrepareDefaultRemoteProtocol_HttpMicroservice(serviceDefinition, serviceDefinition.RemoteProtocol as HttpMicroserviceRemoteProtocol);
					break;
				case BeamoProtocolType.EmbeddedMongoDb:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		await Task.CompletedTask;
	}


	public async Task DeployToLocalClient(BeamoLocalManifest localManifest, string[] deployBeamoIds = null, Action<string> onServiceDeployCompleted = null, Action<JSONMessage> handler = null)
	{
		deployBeamoIds ??= localManifest.ServiceDefinitions.Select(c => c.BeamoId).ToArray();

		var serviceDefinitionsToDeploy = deployBeamoIds.Select(reqId => localManifest.ServiceDefinitions.First(sd => sd.BeamoId == reqId)).ToList();
		SplitLayersByProtocolType(serviceDefinitionsToDeploy, out var builtLayers);

		var prepareImages = new List<Task>();
		prepareImages.AddRange(localManifest.ServiceDefinitions.Select(c => PrepareBeamoServiceImage(c, handler)));

		try
		{
			await Task.WhenAll(prepareImages);
		}
		catch (Exception e)
		{
			// TODO: Add actual exception handling here for when an image fails to build.
			throw;
		}

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

	private static void SplitLayersByProtocolType(IEnumerable<BeamoServiceDefinition> containers, out Dictionary<BeamoProtocolType, List<BeamoServiceDefinition>>[] splitContainers)
	{
		var containersToSplit = containers.ToList();
		BuildLayeredDependencies(containersToSplit, out var layers);

		splitContainers = new Dictionary<BeamoProtocolType, List<BeamoServiceDefinition>>[layers.Length];
		for (var layerIdx = 0; layerIdx < layers.Length; layerIdx++)
		{
			var builtLayer = layers[layerIdx];
			var perProtocolSplit = builtLayer
				.GroupBy(i => containersToSplit[i].Protocol)
				.ToDictionary(g => g.Key, g => g.Select(i => containersToSplit[i]).ToList());

			splitContainers[layerIdx] = perProtocolSplit;
		}
	}

	private static void BuildLayeredDependencies(IEnumerable<BeamoServiceDefinition> containers, out int[][] builtLayers)
	{
		// TODO: Assert no cyclical dependencies exist:
		// TODO   - For each, container just expend dependencies into a queue until we find the original container or we run out of expanded nodes. 

		var containerList = containers.ToList();

		// Find the layers of dependency counts
		var layers = containerList
			.GroupBy(c => c.DependsOnBeamoIds.Length).ToList();

		// Sort the layers by count (smallest number of dependencies first)
		layers.Sort((g1, g2) => g1.Key.CompareTo(g2.Key));

		// Build them into a list of indices into the unsorted list of containers (the parameter list)
		builtLayers = layers.Select(g => g.Select(c => containerList.IndexOf(c)).ToArray()).ToArray();
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
