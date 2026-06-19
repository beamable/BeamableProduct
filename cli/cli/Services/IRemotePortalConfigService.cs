using System.Text.Json;

namespace cli.Portal;

public interface IRemotePortalConfigService
{
	Task<RemotePortalConfiguration> GetRemotePortalConfig(CommandArgs args);
}

public class RemotePortalConfigService : IRemotePortalConfigService
{
	public async Task<RemotePortalConfiguration> GetRemotePortalConfig(CommandArgs args)
	{
		var url = PortalCommand.GetPortalBaseUrl(args, true) + "/extension-pages.json";

		var client = new HttpClient();
		var json = await client.GetStringAsync(url);
		var config = JsonSerializer.Deserialize<RemotePortalConfiguration>(json, new JsonSerializerOptions
		{
			IncludeFields = true
		});

		var commonPref = "/:customerId/games/:gameId/realms/:realmId/";
		foreach (var mountSite in config.mountSites)
		{
			if (mountSite.path.StartsWith(commonPref))
			{
				mountSite.path = mountSite.path.Substring(commonPref.Length);
			}
		}

		return config;
	}
}
