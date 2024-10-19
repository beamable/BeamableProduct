using Beamable.Common.Api;
using cli.Services;
using cli.Utils;

namespace cli.UnityCommands;

public class ShowManifestCommandArgs : CommandArgs
{
	
}

public class ShowManifestCommandOutput
{
	public string localRoutingKey;
	public List<ManifestServiceEntry> services = new List<ManifestServiceEntry>();
	public List<ManifestStorageEntry> storages = new List<ManifestStorageEntry>();
}

public class ManifestServiceEntry
{
	public string beamoId;
	public bool shouldBeEnabledOnRemote;
	public string csprojPath;
	public List<string> storageDependencies;
	public List<UnityAssemblyReferenceData> unityReferences;
}

public class ManifestStorageEntry
{
	public string beamoId;
	public string csprojPath;
	public bool shouldBeEnabledOnRemote;
}

public class ShowManifestCommand : AtomicCommand<ShowManifestCommandArgs, ShowManifestCommandOutput>
{
	public ShowManifestCommand() : base("manifest", "Read local file state and show your local manifest information")
	{
	}

	public override void Configure()
	{
		
	}

	public override Task<ShowManifestCommandOutput> GetResult(ShowManifestCommandArgs args)
	{
		var services = new List<ManifestServiceEntry>();
		var storages = new List<ManifestStorageEntry>();
		
		var manifest = args.BeamoLocalSystem.BeamoManifest;
		
		foreach (var (beamoId, http) in manifest.HttpMicroserviceLocalProtocols)
		{
			if (!manifest.TryGetDefinition(beamoId, out var definition))
			{
				throw new InvalidOperationException($"definition must exist for beamoId=[{beamoId}]");
			}
			var service = new ManifestServiceEntry
			{
				beamoId = beamoId,
				csprojPath = definition.ProjectPath,
				shouldBeEnabledOnRemote = definition.ShouldBeEnabledOnRemote,
				storageDependencies = http.StorageDependencyBeamIds,
				unityReferences = http.UnityAssemblyDefinitionProjectReferences
			};

			services.Add(service);
		}

		foreach (var (beamoId, db) in manifest.EmbeddedMongoDbLocalProtocols)
		{
			if (!manifest.TryGetDefinition(beamoId, out var definition))
			{
				throw new InvalidOperationException($"definition must exist for beamoId=[{beamoId}]");
			}

			var storage = new ManifestStorageEntry
			{
				beamoId = beamoId, 
				csprojPath = definition.ProjectPath,
				shouldBeEnabledOnRemote = definition.ShouldBeEnabledOnRemote
			};
			storages.Add(storage);
		}
		

		return Task.FromResult(new ShowManifestCommandOutput
		{
			services = services,
			storages = storages,
			localRoutingKey = ServiceRoutingStrategyExtensions.GetDefaultRoutingKeyForMachine()
		});
	}
}
