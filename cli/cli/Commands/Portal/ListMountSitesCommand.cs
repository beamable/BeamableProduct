
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
/// this is the data structure representing https://console.beamable.com/extension-pages.json
/// </summary>
[Serializable]
public class RemotePortalConfiguration
{
    public List<MountSiteConfig> mountSites = new List<MountSiteConfig>();

    /// <summary>
    /// Every site known in the Host, once per id: the portal's own (from extension-pages.json) and the
    /// ones running extensions declare in <c>beamable.sites</c>. Not part of extension-pages.json.
    /// </summary>
    public List<SiteConfig> sites = new List<SiteConfig>();

    [Serializable]
    [DebuggerDisplay("{id} ({presentation})")]
    public class SiteConfig
    {
        public string id;
        public string presentation;
        public string description;
        /// <summary>Who renders the site: extension names, or <c>portal</c> for a portal site.</summary>
        public List<string> renderedBy = new List<string>();
    }

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
        /// <summary>A bare site id (e.g. <c>extension-page</c>); the field keeps its old name.</summary>
        public string selector;
        public string type;
        /// <summary>Optional: the site's presentation and description, when the portal publishes them.</summary>
        public string presentation;
        public string description;
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
