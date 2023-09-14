/**
 * This part of the class defines how we manage Beamo Services that have the protocol: EmbeddedMongoDb.
 * It handles default values, how to start the container and other utility functions around this protocol.
 * TODO: Always run the mongo-express data-explorer tool as part of the local deployment protocol. 
 */

using Serilog;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	/// <summary>
	/// Registers a <see cref="BeamoServiceDefinition"/> of with the <see cref="BeamoProtocolType"/> of <see cref="BeamoProtocolType.HttpMicroservice"/>.
	/// </summary>
	/// <param name="beamId">The service's unique id.</param>
	/// <param name="baseImage">A valid MongoDB image that is at least based on the official MongoDB image and exposes the same healthcheck.</param>
	/// <param name="dependencyBeamIds">Other existing services that this depends on. Any dependency is guaranteed to be running by the time this service attempts to start up.</param>
	/// <param name="cancellationToken">A cancellation token to stop the registration.</param>
	/// <returns>A valid <see cref="BeamoServiceDefinition"/> with the default values of the protocol.</returns>
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

	public string GetBeamIdAsMongoContainer(string beamoId) => $"{beamoId}_mongoDb";

	const string MONGO_DATA_CONTAINER_PORT = "27017";

	/// <summary>
	/// Runs a service locally, enforcing the <see cref="BeamoProtocolType.EmbeddedMongoDb"/> protocol.
	/// </summary>
	public async Task RunLocalEmbeddedMongoDb(BeamoServiceDefinition serviceDefinition, EmbeddedMongoDbLocalProtocol localProtocol)
	{
		const string ENV_MONGO_ROOT_USERNAME = "MONGO_INITDB_ROOT_USERNAME";
		const string ENV_MONGO_ROOT_PASSWORD = "MONGO_INITDB_ROOT_PASSWORD";
		const string VOL_NAME_DATA = "{0}_data";
		const string VOL_NAME_FILES = "{0}_files";

		var imageId = serviceDefinition.ImageId;
		var containerName = GetBeamIdAsMongoContainer(serviceDefinition.BeamoId);

		var portBindings = new List<DockerPortBinding>();
		if (!string.IsNullOrEmpty(localProtocol.MongoLocalPort))
			portBindings.Add(new DockerPortBinding() { LocalPort = localProtocol.MongoLocalPort, InContainerPort = MONGO_DATA_CONTAINER_PORT });

		var volumes = new List<DockerVolume>();
		volumes.Add(new DockerVolume { VolumeName = string.Format(VOL_NAME_DATA, serviceDefinition.BeamoId), InContainerPath = localProtocol.DataVolumeInContainerPath });
		volumes.Add(new DockerVolume { VolumeName = string.Format(VOL_NAME_FILES, serviceDefinition.BeamoId), InContainerPath = localProtocol.FilesVolumeInContainerPath });

		var bindMounts = new List<DockerBindMount>();

		var environmentVariables = new List<DockerEnvironmentVariable>()
		{
			new() { VariableName = ENV_MONGO_ROOT_USERNAME, Value = localProtocol.RootUsername }, new() { VariableName = ENV_MONGO_ROOT_PASSWORD, Value = localProtocol.RootPassword },
		};

		// Configures the default mongo image's health check. 
		var cmdStr = $"--interval=5s --timeout=3s CMD /etc/init.d/mongodb status || exit 1";

		try
		{
			// Creates and runs the container. This container will auto destroy when it stops.
			// TODO: Make the auto destruction optional to help CS identify issues in the wild.
			await CreateAndRunContainer(imageId, containerName, cmdStr, true, portBindings, volumes, bindMounts,
				environmentVariables);
		}
		catch (Exception e)
		{
			Log.Error("An error occured while deploying service: " + serviceDefinition.BeamoId);
			Log.Error(e.Message);
		}
	}

	/// <summary>
	/// Resets the protocol data for the <see cref="BeamoServiceDefinition"/> with the given <paramref name="beamoId"/> to the default settings. 
	/// </summary>
	public async Task<bool> ResetToDefaultValues_EmbeddedMongoDb(string beamoId)
	{
		var localUpdated = await ResetLocalProtocol_EmbeddedMongoDb(beamoId, CancellationToken.None);
		var remoteUpdated = await ResetRemoteProtocol_EmbeddedMongoDb(beamoId, CancellationToken.None);
		return localUpdated && remoteUpdated;
	}

	/// <summary>
	/// Short-hand to restore the <see cref="EmbeddedMongoDbLocalProtocol"/> of a given <paramref name="beamoId"/> to default parameters. Returns false if the update fails or if the given <paramref name="beamoId"/>'s service is
	/// not set to the <see cref="BeamoProtocolType.EmbeddedMongoDb"/>. 
	/// </summary>
	public async Task<bool> ResetLocalProtocol_EmbeddedMongoDb(string beamoId, CancellationToken cancellationToken) =>
		await TryUpdateLocalProtocol<EmbeddedMongoDbLocalProtocol>(beamoId, PrepareDefaultLocalProtocol_EmbeddedMongoDb, cancellationToken);

	/// <summary>
	/// Short-hand to restore the <see cref="EmbeddedMongoDbRemoteProtocol"/> of a given <paramref name="beamoId"/> to default parameters. Returns false if the update fails or if the given <paramref name="beamoId"/>'s service is
	/// not set to the <see cref="BeamoProtocolType.EmbeddedMongoDb"/>. 
	/// </summary>
	public async Task<bool> ResetRemoteProtocol_EmbeddedMongoDb(string beamoId, CancellationToken cancellationToken) =>
		await TryUpdateRemoteProtocol<EmbeddedMongoDbRemoteProtocol>(beamoId, PrepareDefaultRemoteProtocol_EmbeddedMongoDb, cancellationToken);

	/// <summary>
	/// Implementation of <see cref="RemoteProtocolModifier{TRemote}"/> that applies the default values of the <see cref="EmbeddedMongoDbLocalProtocol"/>.
	/// <see cref="AddServiceDefinition{TLocal,TRemote}"/> and <see cref="TryUpdateRemoteProtocol{TRemote}"/> to understand how this gets called. 
	/// </summary>
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
	/// Implementation of <see cref="RemoteProtocolModifier{TRemote}"/> that applies the default values of the <see cref="EmbeddedMongoDbRemoteProtocol"/>.
	/// <see cref="AddServiceDefinition{TLocal,TRemote}"/> and <see cref="TryUpdateRemoteProtocol{TRemote}"/> to understand how this gets called. 
	/// </summary>
	private async Task PrepareDefaultRemoteProtocol_EmbeddedMongoDb(BeamoServiceDefinition owner, EmbeddedMongoDbRemoteProtocol remote)
	{
		await Task.CompletedTask;
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

	public bool VerifyCanBeBuiltLocally(ConfigService _)
	{
		if (!BaseImage.Contains("mongo:"))
			throw new Exception($"Base Image [{BaseImage}] must be a version of mongo.");

		return !string.IsNullOrEmpty(BaseImage);
	}
}

public class EmbeddedMongoDbRemoteProtocol : IBeamoRemoteProtocol
{
}
