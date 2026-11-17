namespace cli.Portal;

public class ListPortalExtensionOptionsCommandArgs : CommandArgs { }

public class PortalExtensionOptionsResult
{
    public List<PageExtensionOption> pageExtensions;
    public List<ComponentExtensionOption> componentExtensions;
    /// <summary>Every site in the Host, once each, with who renders it.</summary>
    public List<SiteOption> sites;
}

/// <summary>
/// Represents a full-page mount slot. Set --mount-page to routePrefix + your custom route segment.
/// No --site is needed: a page mount lists the siteIds here (["extension-page"]).
/// </summary>
public class PageExtensionOption
{
    public string routePrefix;
    public List<string> siteIds;
}

/// <summary>
/// The sites rendered at a page of the Portal, including those other running extensions declare
/// (rendered at the URL where that extension renders). Pass one of the siteIds as --site.
/// </summary>
public class ComponentExtensionOption
{
    public string path;
    public List<string> siteIds;
}

/// <summary>One site, listed once: pass its id as --site to mount into every instance of it.</summary>
public class SiteOption
{
    public string id;
    public string presentation;
    public string description;
    /// <summary>The extensions that render the site, or <c>portal</c> for a portal site.</summary>
    public List<string> renderedBy;
}

public class ListPortalExtensionOptionsCommand
    : AtomicCommand<ListPortalExtensionOptionsCommandArgs, PortalExtensionOptionsResult>
{
    public ListPortalExtensionOptionsCommand()
        : base("list-extension-options", "List all valid portal extension mount points — pages and sites — so you can pick the right --mount-page and --site values")
    {
    }

    public override void Configure() { }

    public override async Task<PortalExtensionOptionsResult> GetResult(ListPortalExtensionOptionsCommandArgs args)
    {
        var configService = args.DependencyProvider.GetService<IRemotePortalConfigService>();
        var config = await configService.GetRemotePortalConfig(args);
        return BuildOptions(config);
    }

    public static PortalExtensionOptionsResult BuildOptions(RemotePortalConfiguration config)
    {
        const string pathMatchSuffix = "!pathMatch";
        var pageExtensions = new List<PageExtensionOption>();
        var componentExtensions = new List<ComponentExtensionOption>();

        foreach (var site in config.mountSites)
        {
            // A page site and a component site can share one route (extension-page and nav-left-low
            // both render at !hub/!pathMatch), so split by each entry's type, not by the path.
            var entries = site.selectors.Where(s => !string.IsNullOrEmpty(s.selector)).ToList();
            var pageIds = entries.Where(s => s.type == "page").Select(s => s.selector).ToList();
            var componentIds = entries.Where(s => s.type != "page").Select(s => s.selector).ToList();

            if (pageIds.Count > 0)
            {
                pageExtensions.Add(new PageExtensionOption
                {
                    routePrefix = site.path.EndsWith(pathMatchSuffix) ? site.path[..^pathMatchSuffix.Length] : site.path,
                    siteIds = pageIds
                });
            }

            if (componentIds.Count > 0)
            {
                componentExtensions.Add(new ComponentExtensionOption
                {
                    path = site.path,
                    siteIds = componentIds
                });
            }
        }

        return new PortalExtensionOptionsResult
        {
            pageExtensions = pageExtensions,
            componentExtensions = componentExtensions,
            sites = (config.sites ?? new List<RemotePortalConfiguration.SiteConfig>())
                .Select(s => new SiteOption
                {
                    id = s.id,
                    presentation = s.presentation,
                    description = s.description,
                    renderedBy = s.renderedBy?.ToList() ?? new List<string>()
                })
                .ToList()
        };
    }
}
