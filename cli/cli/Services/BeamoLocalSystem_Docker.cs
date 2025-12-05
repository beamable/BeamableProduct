/**
 * This part of the class has a bunch of utility functions to handle talking to docker using the Docker client in this system.
 * It handles enforcing the way we map BeamoServiceDefinitions to Docker containers.
 */

using Beamable.Common;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using Beamable.Server;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	/// <summary>
	/// Checks if Docker is running locally.
	/// </summary>
	/// <returns></returns>
	public async Task<bool> CheckIsRunning()
	{
		try
		{
			await _client.System.PingAsync();
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Uses the given image (name or id) to create/replace the container with the given name and configurations.
	/// Returns whether or not the container successfully started. It DOES NOT guarantee the app inside the container is running correctly. 
	/// </summary>
	public async Task<bool> CreateAndRunContainer(string image, string containerName,
		string healthConfig,
		bool autoRemoveWhenStopped,
		List<DockerPortBinding> portBindings,
		List<DockerVolume> volumes,
		List<DockerBindMount> bindMounts,
		List<DockerEnvironmentVariable> environmentVars,
		CancellationToken token = default)
	{
		Log.Verbose($"creating or running container with image=[{image}] containerName=[{containerName}]");
		var existingInstance = BeamoRuntime.ExistingLocalServiceInstances.FirstOrDefault(si => si.ContainerName.Contains(containerName));
		if (existingInstance != null)
		{
			if (existingInstance.ImageId == image)
			{
				// the image is the same, so we can re-use it.
				if (existingInstance.IsRunning)
				{
					// since the exact right image is already running for this container; do nothing.
					return true;
				}

				return await RunContainer(containerName);
			}
			else
			{
				// the image is not correct, so we need to erase the old container before recreating it.
				await DeleteContainer(containerName);
			}
		}

		token.ThrowIfCancellationRequested();
		_ = await CreateContainer(image, containerName, healthConfig, autoRemoveWhenStopped, portBindings, volumes, bindMounts, environmentVars);

		token.ThrowIfCancellationRequested();
		var didRun = await RunContainer(containerName);

		token.ThrowIfCancellationRequested();
		_ = await _client.Containers.InspectContainerAsync(containerName);

		token.ThrowIfCancellationRequested();
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
		string healthcheckCmd,
		bool autoRemoveWhenStopped,
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
		if (!string.IsNullOrEmpty(healthcheckCmd))
		{
			// createParams.Healthcheck = new HealthConfig() { 
			// 	Test = new List<string>()
			// {
			// 	healthcheckCmd,
			// },
			// 	Interval = TimeSpan.FromMilliseconds(500),
			// 	Retries = 100,
			// 	Timeout = TimeSpan.FromSeconds(1),
			// 	// StartPeriod = 1500
			// };
		}

		hostConfig.AutoRemove = autoRemoveWhenStopped;

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
			foreach (var warning in response.Warnings)
			{
				Log.Warning(warning);
			}

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
	public async Task<string> PrepareBeamoServiceImage(BeamoServiceDefinition serviceDefinition, Action<string, float> messageHandler, bool forceAmdCpuArchitecture = false, CancellationToken token = default)
	{
		switch (serviceDefinition.Protocol)
		{
			case BeamoProtocolType.EmbeddedMongoDb:
			{
				var localProtocol = BeamoManifest.EmbeddedMongoDbLocalProtocols[serviceDefinition.BeamoId];
				serviceDefinition.ImageId = await PullAndCreateImage(localProtocol.BaseImage, progress =>
				{
					messageHandler?.Invoke(serviceDefinition.BeamoId, progress);
				});
				break;
			}
			case BeamoProtocolType.HttpMicroservice:
			{
				serviceDefinition.ImageId = await BuildAndCreateImage(serviceDefinition.BeamoId, prog =>
				{
					messageHandler?.Invoke(serviceDefinition.BeamoId, prog);
				}, forceCpuArch: forceAmdCpuArchitecture);
				break;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(serviceDefinition.Protocol));
		}
		token.ThrowIfCancellationRequested(); // Happens at the end so if one of the services that are being prepared finishes first, it will throw the OperationCanceledException

		return serviceDefinition.ImageId;
	}

	public struct DockerInfo
	{
		public string arch; // example: "aarch64"
		public string osType; // example: "linux"
		public string Platform => $"{osType}/{arch}";
	}

	public async Task<string> BuildAndCreateImage(string serviceId, Action<float> progressUpdateHandler, bool forceCpuArch)
	{
		var res = await ServicesBuildCommand.Build(_provider, serviceId, log =>
		{
			if (log.isFailure)
			{
				Log.Error($"({serviceId}) - {log.message}");
			}
			else
			{
				Log.Debug($"({serviceId}) - {log.message}");
			}
		}, prog =>
		{
			progressUpdateHandler?.Invoke(prog.Ratio);
		}, forceCpu: forceCpuArch);

		if (!res.success)
		{
			throw new CliException($"cannot build image=[{serviceId}] check logs.");
		}
		return res.ShortImageId;
	}
	
	/// <summary>
	/// Pulls the image with the given <paramref name="imageName"/>:<paramref name="imageTag"/> into the local docker engine from remote docker repositories.
	/// It inspects the pulled image and returns its id, after the pull is done.
	/// </summary>
	public Task<string> PullAndCreateImage(string publicImageName, Action<float> progressUpdateHandler)
	{
		return _client.PullAndCreateImage(publicImageName, progressUpdateHandler);
	}

	/// <summary>
	/// Deletes all running containers associated with the given <paramref name="beamoId"/>.
	/// </summary>
	public async Task DeleteContainers(string beamoId)
	{
		// Delete the containers and remote the service instances mappings
		var existingContainers = BeamoRuntime.ExistingLocalServiceInstances.Where(si => si.BeamoId == beamoId).ToList();
		await Task.WhenAll(existingContainers.Select(si => DeleteContainer(si.ContainerName)));
	}

	/// <summary>
	/// Deletes a container with the given <paramref name="containerId"/> (or container name).
	/// </summary>
	public Task DeleteContainer(string containerId) =>
		_client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters() { RemoveVolumes = false, Force = true, RemoveLinks = false });

	/// <summary>
	/// Stops a container with the given <paramref name="containerId"/> (or container name).
	/// </summary>
	public Task StopContainer(string containerId) =>
		_client.Containers.StopContainerAsync(containerId, new ContainerStopParameters() { });

}

public static class DockerClientHelper
{
	public static async Task<string> PullAndCreateImage(this DockerClient client, string publicImageName, Action<float> progressUpdateHandler)
	{
		// Since we get progress updates in a multi-threaded way, this needs to be a concurrent dictionary
		var progressDict = new ConcurrentDictionary<string, (float downloadProgress, float extractProgress)>();
		await client.Images.CreateImageAsync(new ImagesCreateParameters() { FromImage = publicImageName, },
			null,
			new Progress<JSONMessage>(message =>
			{
				/*
				 * A parser for this JSONMessage format that outputs a single "complete percentage" value every time a new message is received.
				 * There's some setup required for this:
				 * 1) whenever receive a new message, see if id is already in dictionary --- if not, add it with 0 percentages.
				 * 2) Check if the "status" is Downloading/Extracting and increment the percentage accordingly
				 * 3) Calculate the total percentage as the average of the 2 percentages.
				 * 4) Invoke the handler
				 *
				 * Reference of each type of message:
				 * {"status":"Pulling from library/mongo","id":"latest"}
				 * {"status":"Pulling fs layer","progressDetail":{},"id":"20cec14c8f9e"}
				 * {"status":"Waiting","progressDetail":{},"id":"38c3018eb09a"}
				 * {"status":"Downloading","progressDetail":{"current":1834,"total":1834},"progress":"[==================================================>]  1.834kB/1.834kB","id":"97ef66a8492a"}
				 * {"status":"Verifying Checksum","progressDetail":{},"id":"97ef66a8492a"}
				 * {"status":"Download complete","progressDetail":{},"id":"97ef66a8492a"}
				 * {"status":"Extracting","progressDetail":{"current":11501568,"total":28572632},"progress":"[====================>                              ]   11.5MB/28.57MB","id":"d7bfe07ed847"}
				 * {"status":"Pull complete","progressDetail":{},"id":"d7bfe07ed847"}
				 * {"status":"Digest: sha256:82302b06360729842acd27ab8a91c90e244f17e464fcfd366b7427af652c5559"}
				 * {"status":"Status: Downloaded newer image for mongo:latest"}
				 */

				var id = message.ID;
				var status = message.Status;

				// Skip messages with no ids or the pulling messages... We skip the pulling messages as one of them has an id that shouldn't be in the dictionary and the rest are redundant
				if (string.IsNullOrEmpty(id) || status.StartsWith("Pulling from"))
					return;

				// Ensures we are tracking the progress of this id
				progressDict.TryAdd(id, (0f, 0f));

				// {"status":"Downloading","progressDetail":{"current":208640380,"total":210625220},"progress":"[=================================================> ]  208.6MB/210.6MB","id":"be887b845d3f"}
				if (status == "Downloading")
				{
					var current = message.Progress.Current;
					var total = message.Progress.Total;
					(_, float extractProgress) = progressDict[id];

					// We make sure that we complete the progress only when we receive the "Download complete" status update by faking it
					var newProgress = (float)current / total;
					if (Math.Abs(newProgress - 1) < float.Epsilon)
						newProgress -= float.Epsilon;

					progressDict[id] = (newProgress, extractProgress);
				}

				// We force the status to be 1 when we get the download complete message for any given id.
				// {"status":"Download complete","progressDetail":{},"id":"be887b845d3f"}
				else if (status == "Download complete")
				{
					(_, float extractProgress) = progressDict[id];
					progressDict[id] = (1, extractProgress);
				}
				// {"status":"Extracting","progressDetail":{"current":210625220,"total":210625220},"progress":"[==================================================>]  210.6MB/210.6MB","id":"be887b845d3f"}
				else if (status == "Extracting")
				{
					var current = message.Progress.Current;
					var total = message.Progress.Total;
					(float downloadProgress, _) = progressDict[id];

					// We make sure that we complete the progress only when we receive the "Pull complete" status update by faking it
					var newProgress = (float)current / total;
					if (Math.Abs(newProgress - 1) < float.Epsilon)
						newProgress -= float.Epsilon;

					progressDict[id] = (downloadProgress, newProgress);
				}
				// {"status":"Pull complete","progressDetail":{},"id":"e5543880b183"}
				else if (status == "Pull complete")
				{
					progressDict[id] = (1, 1);
				}

				var progressAvg = 0f;
				foreach ((_, (float downloadProgress, float extractProgress)) in progressDict)
				{
					progressAvg += (downloadProgress + extractProgress) / 2f;
				}

				progressAvg /= progressDict.Count;

				progressUpdateHandler?.Invoke(progressAvg);
			}));

		// Find the image that was downloaded
		var builtImage = await client.Images.InspectImageAsync(publicImageName);

		// Notify that the image is available locally
		progressUpdateHandler?.Invoke(1f);

		// Return the image id.
		return builtImage.ID;
	}

	public static async Task<bool> HasImageWithTag(this DockerClient client, string tag)
	{
		var imagesList = await client.Images.ListImagesAsync(new ImagesListParameters());
		foreach (ImagesListResponse image in imagesList)
		{
			if (image?.RepoTags == null)
				continue;
			if (image.RepoTags.Any(s => s.Contains(tag)))
				return true;
		}

		return false;
	}
}
