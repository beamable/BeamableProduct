using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;

namespace cli;

public class ServicesManifestsCommand : AtomicCommand<ServicesManifestsArgs, ServiceManifestOutput>
{
	private BeamoService _beamoService;

	public ServicesManifestsCommand() : base("manifests", ServicesDeletionNotice.REMOVED_PREFIX + "Outputs manifests json to console")
	{
	}
	public override void Configure()
	{
		AddOption(new LimitOption(), (args, i) => args.limit = i);
		AddOption(new SkipOption(), (args, i) => args.skip = i);
	}

	public override async Task<ServiceManifestOutput> GetResult(ServicesManifestsArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.MANIFEST_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
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
