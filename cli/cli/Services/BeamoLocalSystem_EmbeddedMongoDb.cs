﻿/**
 * This part of the class defines how we manage Beamo Services that have the protocol: EmbeddedMongoDb.
 * It handles default values, how to start the container and other utility functions around this protocol.
 * TODO: Always run the mongo-express data-explorer tool as part of the local deployment protocol. 
 */

using Beamable.Server.Common;
using Newtonsoft.Json;
using Serilog;

namespace cli.Services;

public partial class BeamoLocalSystem
{
	public static string GetBeamIdAsMongoContainer(string beamoId) => $"{beamoId}_mongoDb";

	public static string GetDataVolumeName(string beamoId) => $"{beamoId}_data";
	public static string GetFilesVolumeName(string beamoId) => $"{beamoId}_files";
	
	public const string MONGO_DATA_CONTAINER_PORT = "27017";

	/// <summary>
	/// Runs a service locally, enforcing the <see cref="BeamoProtocolType.EmbeddedMongoDb"/> protocol.
	/// </summary>
	public async Task RunLocalEmbeddedMongoDb(BeamoServiceDefinition serviceDefinition, EmbeddedMongoDbLocalProtocol localProtocol)
	{
		const string ENV_MONGO_ROOT_USERNAME = "MONGO_INITDB_ROOT_USERNAME";
		const string ENV_MONGO_ROOT_PASSWORD = "MONGO_INITDB_ROOT_PASSWORD";
		var imageId = localProtocol.BaseImage;
		var containerName = GetBeamIdAsMongoContainer(serviceDefinition.BeamoId);

		var portBindings = new List<DockerPortBinding>();
		if (!string.IsNullOrEmpty(localProtocol.MongoLocalPort))
			portBindings.Add(new DockerPortBinding() { LocalPort = localProtocol.MongoLocalPort, InContainerPort = MONGO_DATA_CONTAINER_PORT });

		var volumes = new List<DockerVolume>();
		volumes.Add(new DockerVolume { VolumeName = localProtocol.DataVolumeName, InContainerPath = localProtocol.DataVolumeInContainerPath });
		volumes.Add(new DockerVolume { VolumeName = localProtocol.FilesVolumeName, InContainerPath = localProtocol.FilesVolumeInContainerPath });

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


}

[Serializable]
public class EmbeddedMongoDbLocalProtocol : IBeamoLocalProtocol
{
	public string BaseImage;

	public string RootUsername;
	public string RootPassword;

	public string MongoLocalPort;

	public string DataVolumeName;
	public string FilesVolumeName;
	
	public string DataVolumeInContainerPath;
	public string FilesVolumeInContainerPath;
	
	[System.Text.Json.Serialization.JsonIgnore]
	[JsonIgnore]
	public CsharpProjectMetadata Metadata;
	
	/// <summary>
	/// A list of beamo ids for dependencies on storage projects
	/// </summary>
	public List<string> GeneralDependencyProjectPaths = new List<string> { };


	public bool VerifyCanBeBuiltLocally(ConfigService _)
	{
		if (!BaseImage.Contains("mongo:"))
			throw new Exception($"Base Image [{BaseImage}] must be a version of mongo.");

		return !string.IsNullOrWhiteSpace(BaseImage);
	}
}

public class EmbeddedMongoDbRemoteProtocol : IBeamoRemoteProtocol
{
}
