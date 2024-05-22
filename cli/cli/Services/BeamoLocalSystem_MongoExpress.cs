using Beamable.Common;
using Beamable.Common.Runtime.Collections;
using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using Spectre.Console;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	private const string MONGO_EXPRESS_IMAGE = "mongo-express:latest";
	public const string ENV_CODE_THEME = "ME_CONFIG_OPTIONS_EDITORTHEME";
	public const string ENV_MONGO_SERVER = "ME_CONFIG_MONGODB_URL";
	public const string ENV_ME_CONFIG_MONGODB_ENABLE_ADMIN = "ME_CONFIG_MONGODB_ENABLE_ADMIN";
	public const string ENV_ME_CONFIG_SITE_COOKIESECRET = "ME_CONFIG_SITE_COOKIESECRET";
	public const string ENV_ME_CONFIG_SITE_SESSIONSECRET = "ME_CONFIG_SITE_SESSIONSECRET";
	public const string ENV_ME_CONFIG_BASIC_AUTH = "ME_CONFIG_BASICAUTH";
	public string GetMongoExpressContainerNameFromStorageId(string storageId) => $"tool_{storageId}_storage";

	public async Task<ContainerInspectResponse> GetOrCreateMongoExpress(string storageId, string connectionString)
	{
		var hasImage = await _client.HasImageWithTag(MONGO_EXPRESS_IMAGE);
		if (!hasImage)
		{
			BeamableLogger.Log($"Need to download {MONGO_EXPRESS_IMAGE}");
			await AnsiConsole.Progress()
				.StartAsync(async ctx =>
				{
					// Define tasks
					var task = ctx.AddTask($"[green]Pulling {MONGO_EXPRESS_IMAGE}[/]");

					await _client.PullAndCreateImage(MONGO_EXPRESS_IMAGE, f => task.Increment((f * 99) - task.Value));
				});
		}
		var containerId = GetMongoExpressContainerNameFromStorageId(storageId);

		try
		{
			Log.Information("Cleaning any old container for mongo-express");
			await _client.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
			await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { });
		}
		catch (DockerContainerNotFoundException)
		{
			Log.Information("Could not remove");
		}

		// the container isn't running, so we need to start it!
		Log.Information($"Creating container {MONGO_EXPRESS_IMAGE} {containerId}");
		var container = await CreateContainer(
			image: MONGO_EXPRESS_IMAGE,
			containerName: containerId,
			healthcheckCmd: null,
			autoRemoveWhenStopped: false,
			portBindings: new List<DockerPortBinding>(),
			volumes: new List<DockerVolume>(),
			bindMounts: new List<DockerBindMount>(),
			environmentVars: new List<DockerEnvironmentVariable>
			{
				new()
				{
					VariableName = ENV_MONGO_SERVER,
					Value = connectionString
				},
				new ()
				{
					VariableName = ENV_CODE_THEME,
					Value = "rubyblue"
				},
				new()
				{
					VariableName = ENV_ME_CONFIG_MONGODB_ENABLE_ADMIN,
					Value = "true"
				},
				new()
				{
					VariableName = ENV_ME_CONFIG_SITE_COOKIESECRET,
					Value = Guid.NewGuid().ToString()
				},
				new()
				{
					VariableName = ENV_ME_CONFIG_SITE_SESSIONSECRET,
					Value = Guid.NewGuid().ToString()
				},
				new()
				{
					VariableName = ENV_ME_CONFIG_BASIC_AUTH,
					Value = "false"
				}
			}
		);
		var success = await _client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
		var res = await _client.Containers.InspectContainerAsync(containerId);

		if (!res.NetworkSettings.Ports.TryGetValue("8081/tcp", out var boundPort))
		{
			Log.Warning("Port binding is not found");
		}
		Log.Information($"Started: success=[{success}] name=[{res.Name}] container=[{container}]");
		return res;
	}
}
