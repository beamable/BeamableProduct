using Beamable.Server;

namespace cli.Services.PortalExtension;

public class PortalExtensionDiscoveryService : Microservice
{
	[ClientCallable]
	public string RequestPortalExtensionData()
	{
		Log.Information("Trying to request portal extension data");
		return "look some data";
	}
}
