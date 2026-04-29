
using System.Diagnostics;
using System.Text.Json;
namespace cli.Portal;

public class ListMountSitesCommandArgs : CommandArgs
{
    
}
public class ListMountSitesCommandResults
{
    public RemotePortalConfiguration config;
}

/// <summary>
/// this is the data structure representing https://portal.beamable.com/extension-pages.json
/// </summary>
[Serializable]
public class RemotePortalConfiguration
{
    public List<MountSiteConfig> mountSites = new List<MountSiteConfig>();
    
    [Serializable]
    [DebuggerDisplay("{path}")]
    public class MountSiteConfig
    {
        public string path;
        public List<MountSiteSelector> selectors = new List<MountSiteSelector>();
        public List<string> navContext = new List<string>();
    }

    [Serializable]
    [DebuggerDisplay("{type}: {selector}")]
    public class MountSiteSelector
    {
        public string selector;
        public string type;
    }
}

public class ListMountSitesCommand : AtomicCommand<ListMountSitesCommandArgs, ListMountSitesCommandResults>
{
    public ListMountSitesCommand() : base("list-mount-sites", "List the available mount sites on the Portal")
    {
    }

    public override void Configure()
    {
    }

    public static async Task<RemotePortalConfiguration> GetRemotePortalConfig(CommandArgs args)
    {
        var url = PortalCommand.GetPortalBaseUrl(args) + "/extension-pages.json";

        var client = new HttpClient();
        var json = await client.GetStringAsync(url);
        var config = JsonSerializer.Deserialize<RemotePortalConfiguration>(json, new JsonSerializerOptions
        {
            IncludeFields = true
        });
        
        // transform out the common prefix 
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
    public override async Task<ListMountSitesCommandResults> GetResult(ListMountSitesCommandArgs args)
    {
        var config = await GetRemotePortalConfig(args);
        return new ListMountSitesCommandResults
        {
            config = config
        };
    }
}
