/**
 * This part of the class has a bunch of utility functions to handle managing running containers of Local Beamo Services.
 * It handles the way we map BeamoServiceDefinitions to BeamoServiceInstances and how those instances map to individual local containers.
 */

using Beamable.Common;
using Docker.DotNet.Models;
using System.Text;
using Beamable.Server;

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
		List<BeamoServiceInstance> existingServiceInstances, CancellationToken token = default)
	{
		var serviceDefinitions = manifest.ServiceDefinitions;
		// Make sure we know about all images that match our beamo ids and make sure all image ids that we know about are still there. 
		foreach (var sd in serviceDefinitions)
		{
			token.ThrowIfCancellationRequested();
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
				.FirstOrDefault(c => dockerContainer.Names.Count > 0 && dockerContainer.Names[0].Split('_')[0].EndsWith(c.BeamoId))?.BeamoId;

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

	public async Promise<Dictionary<string, string>> GetDockerRunningServices()
	{
		var result = new Dictionary<string, string>();
		var currentInfo = await _client.Containers.ListContainersAsync(new ContainersListParameters()
		{
			All = false
		});

		foreach (var info in currentInfo)
		{
			if (info.State == "exited") continue; 
			
			var beamoId = BeamoManifest.ServiceDefinitions.FirstOrDefault(c =>
				{
					switch (c.Protocol)
					{
						case BeamoProtocolType.EmbeddedMongoDb:
							return info.Names[0] == "/" + BeamoLocalSystem.GetBeamIdAsMongoContainer(c.BeamoId);
						case BeamoProtocolType.HttpMicroservice:
							return info.Names[0] == "/" + BeamoLocalSystem.GetBeamIdAsMicroserviceContainer(c.BeamoId);
						default:
							throw new CliException("Unknown protocol type");
					}
					
				})
				?.BeamoId;
			Log.Verbose(beamoId + " found " + info.ID + " as " + info.State);


			if (!string.IsNullOrEmpty(beamoId))
			{
				result.Add(beamoId, info.ID);
			}
		}

		return result;
	}

	/// <summary>
	/// Kick off a long running task that receives updates from the docker engine.
	/// </summary>
	public async Task StartListeningToDockerRaw(Action<string, string, Message> onServiceContainerStateChange = null)
	{
		// Cancel the thread if it's already running.
		if (_dockerListeningThread != null && !_dockerListeningThread.IsCompleted)
			_dockerListeningThreadCancel.Cancel();
			
		// Start the long running task. We don't "await" this task as it never yields until it's cancelled.
		_dockerListeningThread = _client.System.MonitorEventsAsync(new ContainerEventsParameters(),
			new Progress<Message>(DockerSystemEventHandler), _dockerListeningThreadCancel.Token);
		// We await this instead for API consistency...
		await Task.CompletedTask;

		// This is the actual handler that updates the state according to the event message.
		// Since Docker API docs are not the best. I recommend using JsonConvert.SerializeObject and print out the object that their APIs return to debug or add to any of this stuff.
		async void DockerSystemEventHandler(Message message)
		{
			try
			{
				var messageType = message.Type;
				var messageAction = message.Action;
				Log.Verbose($"Docker Message type=[{messageType}] action=[{messageAction}] id=[{message.ID}]");

				switch (messageType, messageAction)
				{
					case ("container", "create"):
					{
						// Find the beamoId tied to the image that was used to create the container
						var beamoId = BeamoManifest.ServiceDefinitions
							.FirstOrDefault(c => message.Actor.Attributes["name"].Contains(c.BeamoId))
							?.BeamoId;
						if (!string.IsNullOrEmpty(beamoId))
						{
							message.ID ??= message.Actor.ID;

							var containerData = (await _client.Containers.InspectContainerAsync(message.ID));
							var containerName = containerData.Name.Substring(1);
							var ports = containerData.HostConfig.PortBindings.Select(kvp =>
							{
								var inContainerPort = kvp.Key.Split("/")[0];
								var localPort = kvp.Value.First().HostPort;
								return new DockerPortBinding
									{ LocalPort = localPort, InContainerPort = inContainerPort };
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
							BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si =>
								message.Actor.Attributes["name"].Contains(si.BeamoId));
						if (beamoServiceInstance != null)
						{
							beamoServiceInstance.IsRunning = true;
							onServiceContainerStateChange?.Invoke(beamoServiceInstance.BeamoId, messageAction, message);
						}

						// TODO: Detect when people containers are running but their dependencies are not. Output a list of warnings that people can then print out.
						break;
					}

					case ("container", "stop"):
					{
						var beamoServiceInstance =
							BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si =>
								message.Actor.Attributes["name"].Contains(si.BeamoId));
						if (beamoServiceInstance != null)
						{
							beamoServiceInstance.IsRunning = false;
							onServiceContainerStateChange?.Invoke(beamoServiceInstance.BeamoId, messageAction, message);
						}

						// TODO: Detect when people containers are running but their dependencies are not. Output a list of warnings that people can then print out.
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to listen to docker");
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
		if (!toCheck.IsLocal) return false;

		switch (toCheck.Protocol)
		{
			case BeamoProtocolType.HttpMicroservice:

				var dockerfile = Path.Combine(toCheck.ProjectDirectory, "Dockerfile");
				var relativePath = _configService.GetRelativeToExecutionPath(dockerfile);
				var hasDockerfile = File.Exists(relativePath);
				if (!hasDockerfile) return false;

				return true;
			
			case BeamoProtocolType.EmbeddedMongoDb:
				return true; // always pull down a version of mongo.
			default:
				throw new CliException($"Unknown protocol=[{toCheck.Protocol}] inside method=[{nameof(VerifyCanBeBuiltLocally)}]");
		}
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
		Action<string> onServiceDeployCompleted = null, bool autoDeleteContainers = true, 
		CancellationToken token = default)
	{
		var localManifest = localSystem.BeamoManifest;

		// Get all services that must be deployed (and that are not just known remotely --- as in, have their local protocols correctly configured).
		var serviceDefinitionsToDeploy = GetServiceDefinitionsThatCanBeDeployed(localManifest, deployBeamoIds);
		token.ThrowIfCancellationRequested();

		// Guarantee they each don't have cyclical dependencies.
		{
			var dependencyChecksForServicesToDeploy = await Task.WhenAll(serviceDefinitionsToDeploy.Select(sd =>
			{
				return Task.Run(() =>
					ValidateBeamoService_NoCyclicalDependencies(sd, localManifest.ServiceDefinitions));
			}));

			token.ThrowIfCancellationRequested();
			var indexOfServiceWithCyclicalDependency = dependencyChecksForServicesToDeploy.ToList().IndexOf(false);
			if (indexOfServiceWithCyclicalDependency != -1)
				throw new Exception(
					$"{serviceDefinitionsToDeploy[indexOfServiceWithCyclicalDependency].BeamoId} has cyclical dependencies!");
		}


		// Builds all images for all services that are defined and can be built locally.
		
		var prepareImages = new List<Task>(serviceDefinitionsToDeploy.Select(c => PrepareBeamoServiceImage(c, buildPullImageProgress, forceAmdCpuArchitecture, token)));
		await Task.WhenAll(prepareImages);

		// Build dependency layers split by protocol type.
		SplitDefinitionsByProtocolType(serviceDefinitionsToDeploy, out Dictionary<BeamoProtocolType, List<BeamoServiceDefinition>> builtDefinitions);

		// For each layer, run through the containers of each type on that layer and do what is needed to deploy them
		// We already know that all containers are properly built here, so we just need to create the containers and run them.
		var runContainerTasks = new List<Task>();

		token.ThrowIfCancellationRequested();
		// Kick off all the run container tasks for the Embedded MongoDatabases in this layer
		if (builtDefinitions.TryGetValue(BeamoProtocolType.EmbeddedMongoDb, out var microStorageContainers))
			runContainerTasks.AddRange(microStorageContainers.Select(async sd =>
			{
				Log.Debug("Started deploying service: " + sd.BeamoId);
				await RunLocalEmbeddedMongoDb(sd, localManifest.EmbeddedMongoDbLocalProtocols[sd.BeamoId]);
				Log.Debug("Finished deploying service: " + sd.BeamoId);
				onServiceDeployCompleted?.Invoke(sd.BeamoId);
				token.ThrowIfCancellationRequested(); //The first service to be deployed locally would throw the exception and prevent the others from continuing
			}));

		// Kick off all the run container tasks for the HTTP Microservices in this layer
		if (builtDefinitions.TryGetValue(BeamoProtocolType.HttpMicroservice, out var microserviceContainers))
			runContainerTasks.AddRange(microserviceContainers.Select(async sd =>
			{
				await RunLocalHttpMicroservice(sd, localManifest.HttpMicroserviceLocalProtocols[sd.BeamoId],
					localSystem, autoDeleteContainers, token);
				onServiceDeployCompleted?.Invoke(sd.BeamoId);
			}));


		// Wait for all container tasks in this layer to finish before starting the next one.
		await Task.WhenAll(runContainerTasks);
		token.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Given a Directed Acyclic Graph of <paramref name="serviceDefinitions"/>, builds a dictionary for each of the graph's layers. This dictionary splits the services in each layer by their
	/// <see cref="BeamoProtocolType"/>. 
	/// </summary>
	private static void SplitDefinitionsByProtocolType(List<BeamoServiceDefinition> serviceDefinitions,
		out Dictionary<BeamoProtocolType, List<BeamoServiceDefinition>> splitContainers)
	{
		// Split each definition by their BeamoProtocolType

			splitContainers = serviceDefinitions.GroupBy(i => i.Protocol)
				.ToDictionary(g => g.Key, g => g.ToList());
	}

	public async IAsyncEnumerable<string> TailLogs(string containerId, CancellationTokenSource cts)
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

		while (true)
		{
			if (cts.IsCancellationRequested) break;

			string line = null;
			try
			{
				line = await reader.ReadLineAsync(cts.Token);
				if (line == null)
				{
					Log.Verbose("Storage log returned null line, which likely means it has shutdown");
					break;
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}

			if (line?.Length > 8 && line[0] < 'a')
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
