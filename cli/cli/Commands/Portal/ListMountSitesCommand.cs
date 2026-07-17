
using System.Diagnostics;
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

    public override async Task<ListMountSitesCommandResults> GetResult(ListMountSitesCommandArgs args)
    {
        var configService = args.DependencyProvider.GetService<IRemotePortalConfigService>();
        var config = await configService.GetRemotePortalConfig(args);
        return new ListMountSitesCommandResults
        {
            config = config
        };
    }
}
