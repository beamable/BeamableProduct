/**
 * This part of the class defines how we manage Beamo Services that have the protocol: EmbeddedMongoDb.
 * It handles default values, how to start the container and other utility functions around this protocol.
 * TODO: Always run the mongo-express data-explorer tool as part of the local deployment protocol. 
 */

namespace cli;

public partial class BeamoLocalSystem
{
	public async Task<BeamoServiceDefinition> AddDefinition_EmbeddedMongoDb(string beamId, string baseImage, string[] dependencyBeamIds, CancellationToken cancellationToken)
	{
		dependencyBeamIds ??= Array.Empty<string>();
		baseImage ??= "mongo:latest";
		return await AddServiceDefinition<EmbeddedMongoDbLocalProtocol, EmbeddedMongoDbRemoteProtocol>(
			beamId,
			BeamoProtocolType.EmbeddedMongoDb,
			dependencyBeamIds,
			async (definition, protocol) =>
			{
				await PrepareDefaultLocalProtocol_EmbeddedMongoDb(definition, protocol);
				if (!string.IsNullOrEmpty(baseImage))
					protocol.BaseImage = baseImage;
			},
			PrepareDefaultRemoteProtocol_EmbeddedMongoDb,
			cancellationToken);
	}

	public async Task RunLocalEmbeddedMongoDb(BeamoServiceDefinition serviceDefinition, EmbeddedMongoDbLocalProtocol localProtocol)
	{
		const string ENV_MONGO_ROOT_USERNAME = "MONGO_INITDB_ROOT_USERNAME";
		const string ENV_MONGO_ROOT_PASSWORD = "MONGO_INITDB_ROOT_PASSWORD";
		const string VOL_NAME_DATA = "{0}_data";
		const string VOL_NAME_FILES = "{0}_files";
		const string IN_CONTAINER_PORT = "27017";

		var imageId = serviceDefinition.ImageId;
		var containerName = $"{serviceDefinition.BeamoId}_mongoDb";

		var portBindings = new List<DockerPortBinding>();
		if (!string.IsNullOrEmpty(localProtocol.MongoLocalPort))
			portBindings.Add(new DockerPortBinding() { LocalPort = localProtocol.MongoLocalPort, InContainerPort = IN_CONTAINER_PORT });

		var volumes = new List<DockerVolume>();
		volumes.Add(new DockerVolume { VolumeName = string.Format(VOL_NAME_DATA, serviceDefinition.BeamoId), InContainerPath = localProtocol.DataVolumeInContainerPath });
		volumes.Add(new DockerVolume { VolumeName = string.Format(VOL_NAME_FILES, serviceDefinition.BeamoId), InContainerPath = localProtocol.FilesVolumeInContainerPath });

		var bindMounts = new List<DockerBindMount>();

		var environmentVariables = new List<DockerEnvironmentVariable>()
		{
			new() { VariableName = ENV_MONGO_ROOT_USERNAME, Value = localProtocol.RootUsername }, new() { VariableName = ENV_MONGO_ROOT_PASSWORD, Value = localProtocol.RootPassword },
		};

		var cmdStr = $"--interval=5s --timeout=3s CMD /etc/init.d/mongodb status || exit 1";
		await CreateAndRunContainer(imageId, containerName, cmdStr, true, portBindings, volumes, bindMounts, environmentVariables);
	}

	private async Task PrepareDefaultRemoteProtocol_EmbeddedMongoDb(BeamoServiceDefinition owner, EmbeddedMongoDbRemoteProtocol remote)
	{
		await Task.CompletedTask;
	}

	private async Task PrepareDefaultLocalProtocol_EmbeddedMongoDb(BeamoServiceDefinition owner, EmbeddedMongoDbLocalProtocol local)
	{
		local.BaseImage = "mongo:latest";

		local.RootUsername = "beamable";
		local.RootPassword = "beamable";

		local.MongoLocalPort = "";

		local.DataVolumeInContainerPath = "/data/db";
		local.FilesVolumeInContainerPath = "/beamable";

		await Task.CompletedTask;
	}

	/// <summary>
	/// Resets the protocol data for the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> to the default settings. 
	/// </summary>
	public async Task<bool> ResetToDefaultValues_EmbeddedMongoDb(string beamoId)
	{
		var localUpdated = await TryUpdateLocalProtocol<EmbeddedMongoDbLocalProtocol>(beamoId, PrepareDefaultLocalProtocol_EmbeddedMongoDb, CancellationToken.None);
		var remoteUpdated = await TryUpdateRemoteProtocol<EmbeddedMongoDbRemoteProtocol>(beamoId, PrepareDefaultRemoteProtocol_EmbeddedMongoDb, CancellationToken.None);
		return localUpdated && remoteUpdated;
	}
}

public class EmbeddedMongoDbLocalProtocol : IBeamoLocalProtocol
{
	public string BaseImage;

	public string RootUsername;
	public string RootPassword;

	public string MongoLocalPort;

	public string DataVolumeInContainerPath;
	public string FilesVolumeInContainerPath;

	public bool VerifyCanBeBuiltLocally()
	{
		if (!BaseImage.Contains("mongo:"))
			throw new Exception($"Base Image [{BaseImage}] must be a version of mongo.");

		return !string.IsNullOrEmpty(BaseImage);
	}
}

public class EmbeddedMongoDbRemoteProtocol : IBeamoRemoteProtocol
{
}
