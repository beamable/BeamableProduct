namespace cli;

public partial class BeamoLocalService
{
	/// <summary>
	/// Synchronizes the given <see cref="localManifest"/> instance with the given <see cref="remoteManifest"/>. This will:
	/// <list type="bullet">
	/// <item>Ensures all remote embedded databases (storage objects) are defined locally. Since these can always be built </item>
	/// <item></item>
	/// </list>
	/// </summary>
	/// <param name="remoteManifest"></param>
	/// <param name="localManifest"></param>
	public async Task SyncLocalManifestWithRemote(ServiceManifest remoteManifest, BeamoLocalManifest localManifest)
	{
		var existingMongoServices = localManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.EmbeddedMongoDb).ToList();
		foreach (var storageReference
		         in remoteManifest.storageReference)
		{
			// If we don't have a service storage with that id stored locally, let's make one
			if (existingMongoServices.All(sd => sd.BeamoId != storageReference.id))
				await AddDefinition_EmbeddedMongoDb(storageReference.id, null, Array.Empty<string>(), CancellationToken.None);
		}

		var existingHttpServices = localManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice).ToList();
		foreach (var httpReference in remoteManifest.manifest)
		{
			var foundDefinition = existingHttpServices.FirstOrDefault(sd => sd.BeamoId == httpReference.serviceName);
			// If we don't have an http service with that id locally defined, let's make a remote only service defined locally.
			if (foundDefinition == null)
			{
				var sd = await AddDefinition_HttpMicroservice(httpReference.serviceName,
					null,
					null,
					httpReference.dependencies.Select(d => d.id).ToArray(),
					CancellationToken.None);

				sd.ImageId = httpReference.imageId;
			}
			else
			{
				// If we can't build the image locally, we keep the reference to the image id that existed in the remote.
				if (!VerifyCanBeBuiltLocally(foundDefinition))
					foundDefinition.ImageId = httpReference.imageId;
			}
		}
	}

	public static void WriteServiceManifestFromLocal(BeamoLocalManifest localManifest, string comments, Dictionary<string, string> perIdComments,
		ServiceManifest remoteManifest)
	{
		// TODO: When we change ShouldEnableOnRemoteDeploy, we should auto-enable/disable all dependencies... makes this algorithm much simpler if we have that guarantee.

		// Setup comments
		remoteManifest.comments = comments;

		// Build list of service storage references
		{
			var allMongoServices = localManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.EmbeddedMongoDb).ToList();
			remoteManifest.storageReference = allMongoServices.Select(mongoSd => new ServiceStorageReference()
			{
				id = mongoSd.BeamoId, 
				// Tied to Embedded Mongo DB --- if we add other embedded db types, they each get their const.
				storageType = "mongov1", templateId = "small", enabled = mongoSd.ShouldBeEnabledOnRemote,
			}).ToList();
		}

		// Build list of service references
		{
			var allHttpMicroservices = localManifest.ServiceDefinitions.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice).ToList();

			remoteManifest.manifest = allHttpMicroservices.Select(httpSd =>
			{
				var remoteProtocol = localManifest.HttpMicroserviceRemoteProtocols[httpSd.BeamoId];
				if (!perIdComments.TryGetValue(httpSd.BeamoId, out var httpSdComments))
					httpSdComments = string.Empty;

				return new ServiceReference()
				{
					serviceName = httpSd.BeamoId,
					enabled = httpSd.ShouldBeEnabledOnRemote,
					templateId = "small",
					containerHealthCheckPort = long.Parse(remoteProtocol.HealthCheckPort),
					imageId = httpSd.ImageId,
					dependencies = httpSd.DependsOnBeamoIds.Select(beamoId =>
					{
						var storageType = "";
						switch (localManifest.ServiceDefinitions.First(sd => sd.BeamoId == beamoId).Protocol)
						{
							case BeamoProtocolType.EmbeddedMongoDb:
							{
								// Same const as the comment above...
								storageType = "mongov1";
								break;
							}
							case BeamoProtocolType.HttpMicroservice:
							default:
								throw new ArgumentOutOfRangeException();
						}

						return new ServiceDependency() { id = beamoId, storageType = storageType };
					}).ToList(),
					comments = httpSdComments,
				};
			}).ToList();
		}
	}
}
