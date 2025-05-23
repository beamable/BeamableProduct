﻿/**
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
	public async Task<DockerInfo> GetBuildPlatform()
	{
		var info = await _client.System.GetSystemInfoAsync();
		
		var plugins = await _client.Plugin.ListPluginsAsync(new PluginListParameters { });
		var buildx = await _client.Plugin.InspectPluginAsync("buildx");
		// info.Plugins
		return new DockerInfo { 
			arch = info.Architecture, 
			osType = info.OSType
		};
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

	public static List<string> ParseDockerfile(ConfigService configService, string dockerFilePath)
	{
		var paths = new List<string>();

		var fileContents = File.ReadAllLines(dockerFilePath);

		foreach (string line in fileContents)
		{
			// Handles escaping stuff from copy statements that we don't need (to enforce pattern of COPY SOURCE DESTINATION).
			string escapedLine = line;
			
			//This needs to have the follow pattern: "COPY SOURCE DESTINATION", if not just continues reading file
			if (escapedLine.Contains("--"))
				escapedLine = "";

			if (escapedLine.StartsWith("COPY"))
			{
				var parts = escapedLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
				var probablePath = parts[1];

				if (probablePath.Contains("*"))
					probablePath = Path.GetDirectoryName(probablePath);
				
				try
				{
					var result = Path.GetFullPath(configService.BeamableRelativeToExecutionRelative(probablePath));
					FileAttributes attr = File.GetAttributes(result); //If it's not a valid path, this is going to throw an exception
					paths.Add(result);
				}
				catch (Exception e)
				{
					// If the exception was an IO one, then throw it, otherwise just continue looking for paths
					if (e is PathTooLongException || e is FileNotFoundException || e is DirectoryNotFoundException || e is IOException)
					{
						throw new CliException($"Dockerfile has invalid source path to copy. Docker path: [{dockerFilePath}] Error: [{e.Message}] Stack: [{e.StackTrace}]");
					}
				}
			}
		}

		return paths.Distinct().ToList();
	}

	/// <summary>
	/// Creates a tarball stream containing every file in the given <paramref name="directory"/>. 
	/// </summary>
	public static Stream CreateTarballForDirectory(ConfigService configService, List<string> paths)
	{
		var tarball = new MemoryStream(512 * 1024);
		var allFiles = new List<string>();

		foreach (var path in paths)
		{
			FileAttributes attr = File.GetAttributes(path);

			if (attr.HasFlag(FileAttributes.Directory))
			{
				var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
				allFiles.AddRange(files.ToList());
			}
			else
			{
				allFiles.Add(path);
			}
		}

		using var archive = new TarOutputStream(tarball, Encoding.Default)
		{
			//Prevent the TarOutputStream from closing the underlying memory stream when done
			IsStreamOwner = false,
		};

		// Get every file in the given directory
		foreach (var file in allFiles)
		{
			if (file.Contains("/bin/") || file.Contains("/obj/"))
			{
				continue;
			}

			// Open the file we're putting into the tarball
			using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			// When creating the tar file (using SharpZipLib) if I create the tar entries from the filenames,
			// the tar will be created with the wrong slashes (\ instead of / for linux).
			// Swapping those out myself if you're doing any COPY or ADD within folders.

			var localPath = configService.GetRelativeToBeamableFolderPath(file);
			var tarName = localPath.Replace('\\', '/').TrimStart('/');

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
	/// </summary>
	public async Task CleanUpDocker(BeamoServiceDefinition serviceDefinition)
	{
		var beamoId = serviceDefinition.BeamoId;

		// Delete the containers and remote the service instances mappings
		await Task.WhenAll(DeleteContainers(beamoId));

		BeamoRuntime.ExistingLocalServiceInstances.RemoveAll(si => si.BeamoId == beamoId);

		// Delete the image and update the image id
		var hasImage = !string.IsNullOrEmpty(serviceDefinition.ImageId);
		if (hasImage)
		{
			// Check if this image is configured to be built locally
			var canBeBuiltLocally = VerifyCanBeBuiltLocally(serviceDefinition);

			// Handle deletion based on the protocol
			switch (serviceDefinition.Protocol)
			{
				case BeamoProtocolType.HttpMicroservice:
				{
					// For HttpMicroservices we delete using the tag to guarantee 
					try
					{
						await DeleteImage(beamoId.ToLower());
					}
					catch (Exception e)
					{
						// We can ignore "no such image" exceptions if we can't be built locally and we don't have a matching image.
						// In all other cases, exceptions here are problematic and should be investigated.
						if (!canBeBuiltLocally)
						{
							//Docker API responded with status code=NotFound, response={"message":"No such image: newmicroservice:latest"}
							if (!e.Message.Contains("No such image"))
								throw;
						}
						else
						{
							throw;
						}
					}

					break;
				}
				case BeamoProtocolType.EmbeddedMongoDb:
				{
					// We only delete the image if no other running container are using it
					var otherRunningMongoInstances = BeamoRuntime.ExistingLocalServiceInstances
						.Any(si => BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == si.BeamoId).Protocol == BeamoProtocolType.EmbeddedMongoDb);

					if (!otherRunningMongoInstances)
					{
						// For StorageObjects we delete using the image id of the mongo image  
						try
						{
							await DeleteImage(serviceDefinition.ImageId);
						}
						catch (Exception e)
						{
							// We can ignore "reference does not exist" exceptions if we ever get them. These happen if/when the image has already been deleted by a previous pass of through this code. 
							// This happens when you have multiple EmbeddedMongo services that were running and stop them via the Docker for Windows UI or some external case.
							// Basically, this means that, for each registered mongo service, we'll try to delete the same mongo image and get the following error:
							// Docker API responded with status code=NotFound, response={"message":"reference does not exist"}
							// As such, we can essentially ignore this.
							// TODO: A more robust algorithm for this is to make sure that we don't have repeating image ids tied to BeamoIds when running this stop loop.
							if (!e.Message.Contains("reference does not exist") &&

							    // Because we run this in-parallel, we can also get this error:
							    // Docker API responded with status code=InternalServerError, response={"message":"unrecognized image ID sha256:c8b57c4bf7e3a88daf948d5d17bc7145db05771e928b3b3095ca4590719b5469"}    
							    !e.Message.Contains("unrecognized image ID"))
								throw;
						}
					}

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			// Only delete the image id if we are set up to build the image locally.
			// If we aren't, it means this is a service that was deployed at some point and we no longer have the resources to modify it.
			// As such, we should keep the image id that is registered with it. See SyncLocalManifestWithRemote.
			if (canBeBuiltLocally)
				serviceDefinition.ImageId = "";
		}
	}

	/// <summary>
	/// Deletes the image with the given name/tag or ImageId.
	/// </summary>
	public Task DeleteImage(string imageName) => _client.Images.DeleteImageAsync(imageName, new ImageDeleteParameters() { NoPrune = false, Force = true }, CancellationToken.None);

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

	/// <summary>
	/// Exports the contents of the image with the given <paramref name="beamoId"/> to a <see cref="Stream"/>.
	/// The contents of the stream can be placed into an in-memory tarball and then extracted. <see cref="UploadContainers"/>.
	/// </summary>
	public async Task<Stream> SaveImage(string beamoId) =>
		await SaveImage(BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == beamoId));

	/// <summary>
	/// Exports the contents of the image associated with the given <paramref name="serviceDefinition"/> to a <see cref="Stream"/>.
	/// The contents of the stream can be placed into an in-memory tarball and then extracted. <see cref="UploadContainers"/>. 
	/// </summary>
	public async Task<Stream> SaveImage(BeamoServiceDefinition serviceDefinition) =>
		await _client.Images.SaveImageAsync(serviceDefinition.ImageId, CancellationToken.None);
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
