using Beamable.Common;
using Beamable.Common.Api;

namespace cli;

public class BeamoService { 
	public const string SERVICE = "/basic/beamo";
	public CliRequester Requester { get; }
	
	public BeamoService(CliRequester requester)
	{
		Requester = requester;
	}
	
	public Promise<ServiceManifest> GetCurrentManifest()
	{
		return Requester.Request<GetManifestResponse>(Method.GET, $"{SERVICE}/manifest/current", "{}")
		                .Map(res => res.manifest)
		                .RecoverFrom40x(ex => new ServiceManifest());
	}
	
	public Promise<GetStatusResponse> GetStatus()
	{
		return Requester.Request<GetStatusResponse>(Method.GET, $"{SERVICE}/status")
		                .RecoverFrom40x(err => new GetStatusResponse
		                {
			                isCurrent = false,
			                services = new List<ServiceStatus>()
		                });
	}

	public Promise<List<ServiceManifest>> GetManifests()
	{
		return Requester.Request<GetManifestsResponse>(Method.GET, $"{SERVICE}/manifests")
		                .Map(res => res.manifests)
		                .RecoverFrom40x(err => new List<ServiceManifest>());
	}
}

[System.Serializable]
public class GetManifestResponse
{
	public ServiceManifest manifest;
}

[System.Serializable]
public class GetManifestsResponse
{
	public List<ServiceManifest> manifests;
}

[System.Serializable]
public class PostManifestRequest
{
	public string comments;
	public List<ServiceReference> manifest;
	public List<ServiceStorageReference> storageReferences;
}

[System.Serializable]
public class ServiceManifest
{
	public string id;
	public long created;
	public List<ServiceReference> manifest = new List<ServiceReference>();
	public List<ServiceStorageReference> storageReference = new List<ServiceStorageReference>();
	public long createdByAccountId;
	public string comments;
}

[System.Serializable]
public class ServiceReference
{
	public string serviceName;
	public string checksum;
	public bool enabled;
	public string imageId;
	public string templateId;
	public string comments;
	public List<ServiceDependency> dependencies;
	public long containerHealthCheckPort = 6565;
}

[System.Serializable]
public class ServiceStorageReference
{
	public string id;
	public string storageType;
	public bool enabled;
	public string templateId;
	public string checksum;
}

[System.Serializable]
public class ServiceDependency
{
	public string storageType;
	public string id;
}

[System.Serializable]
public class GetStatusResponse
{
	public bool isCurrent;
	public List<ServiceStatus> services;
}

[System.Serializable]
public class ServiceStatus
{
	public string serviceName;
	public string imageId;
	public bool running;
	public bool isCurrent;
}

[System.Serializable]
public class GetLogsResponse
{
	public string serviceName;
	public List<LogMessage> logs;
}

[System.Serializable]
public class LogMessage
{
	public string level;
	public long timestamp;
	public string message;
}
