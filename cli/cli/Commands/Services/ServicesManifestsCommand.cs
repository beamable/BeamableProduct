using cli.Services;
using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class ServicesManifestsCommand : AtomicCommand<ServicesManifestsArgs, ServiceManifestOutput>
{
	private BeamoService _beamoService;

	public ServicesManifestsCommand() : base("manifests", "Outputs manifests json to console")
	{
	}
	public override void Configure()
	{
		AddOption(new LimitOption(), (args, i) => args.limit = i);
		AddOption(new SkipOption(), (args, i) => args.skip = i);
	}

	public override async Task<ServiceManifestOutput> GetResult(ServicesManifestsArgs args)
	{
		_beamoService = args.BeamoService;

		List<CliServiceManifest> response = await AnsiConsole.Status()
										.Spinner(Spinner.Known.Default)
										.StartAsync("Sending Request...", async ctx =>
										{
											var manifests = await _beamoService.GetManifests();
											return manifests?.Select(x => new CliServiceManifest
												{
													comments = x.comments,
													created = x.created,
													createdByAccountId = x.createdByAccountId,
													id = x.id,
													manifest = x.manifest?.Select(m => new CliServiceReference
													{
														checksum = m.checksum,
														comments = m.comments,
														containerHealthCheckPort = m.containerHealthCheckPort,
														enabled = m.enabled,
														imageId = m.imageId,
														templateId = m.templateId,
														components = m.components?.Select(c => new CliServiceComponent
														{
															name = c.name
														}).ToList(),
														dependencies = m.dependencies?.Select(d => new CliServiceDependency
														{
															id = d.id,
															storageType = d.storageType
														}).ToList()
													}).ToList(),
													storageReference = x.storageReference?.Select(m => new CliServiceStorageReference
													{
														id = m.id,
														enabled = m.enabled,
														storageType = m.storageType,
														checksum = m.checksum,
														templateId = m.templateId,
													}).ToList()
												})
												.ToList();
										});
		response = response.Skip(args.skip).Take(args.limit > 0 ? args.limit : int.MaxValue).ToList();

		var result = new ServiceManifestOutput { manifests = response };
		return result;
	}
}

public class ServicesManifestsArgs : CommandArgs
{
	public int limit = 0;
	public int skip = 0;
}

public class ServiceManifestOutput
{
	public List<CliServiceManifest> manifests;
}


[Serializable]
public class CliServiceManifest
{
	public string id;
	public long created;
	public List<CliServiceReference> manifest = new List<CliServiceReference>();
	public List<CliServiceStorageReference> storageReference = new List<CliServiceStorageReference>();
	public long createdByAccountId;
	public string comments;
}

[Serializable]
public class CliServiceReference
{
	public string serviceName;
	public string checksum;
	public bool enabled;
	public string imageId;
	public string templateId;
	public string comments;
	public List<CliServiceDependency> dependencies;
	public long containerHealthCheckPort = 6565;
	public List<CliServiceComponent> components;
}

[Serializable]
public class CliServiceStorageReference
{
	public string id;
	public string storageType;
	public bool enabled;
	public string templateId;
	public string checksum;
}

[Serializable]
public class CliServiceDependency
{
	public string storageType;
	public string id;
}

[Serializable]
public class CliServiceComponent 
{
	public string name;
}
