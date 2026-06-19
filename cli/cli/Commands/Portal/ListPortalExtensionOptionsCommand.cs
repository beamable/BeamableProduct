namespace cli.Portal;

public class ListPortalExtensionOptionsCommandArgs : CommandArgs { }

public class PortalExtensionOptionsResult
{
    public List<PageExtensionOption> pageExtensions;
    public List<ComponentExtensionOption> componentExtensions;
    public List<ExtensionMountSiteOption> extensionMountSites;
}

/// <summary>
/// Represents a mount slot exposed by another local extension via <c>&lt;BeamExtensionSite&gt;</c>.
/// Set --mount-page to extensionName and --mount-selector to one of the entries in selectors;
/// the child extension then renders inside the host extension wherever it is mounted.
/// </summary>
public class ExtensionMountSiteOption
{
    public string extensionName;
    public List<ComponentSelectorOption> selectors;
}

/// <summary>
/// Represents a full-page mount slot. Set --mount-page to routePrefix + your custom route segment.
/// The --mount-selector is not required; it is automatically assigned to autoSelector.
/// </summary>
public class PageExtensionOption
{
    public string routePrefix;
    public string autoSelector;
}

/// <summary>
/// Represents a component slot inside an existing Portal page.
/// Set --mount-page to path and --mount-selector to one of the entries in selectors.
/// </summary>
public class ComponentExtensionOption
{
    public string path;
    public List<ComponentSelectorOption> selectors;
}

public class ComponentSelectorOption
{
    public string selector;
    public string type;
}

public class ListPortalExtensionOptionsCommand
    : AtomicCommand<ListPortalExtensionOptionsCommandArgs, PortalExtensionOptionsResult>
{
    public ListPortalExtensionOptionsCommand()
        : base("list-extension-options", "List all valid portal extension mount points — pages and component slots — so you can pick the right --mount-page and --mount-selector values")
    {
    }

    public override void Configure() { }

    public override async Task<PortalExtensionOptionsResult> GetResult(ListPortalExtensionOptionsCommandArgs args)
    {
        var configService = args.DependencyProvider.GetService<IRemotePortalConfigService>();
        var config = await configService.GetRemotePortalConfig(args);

        const string pathMatchSuffix = "!pathMatch";
        var pageExtensions = new List<PageExtensionOption>();
        var componentExtensions = new List<ComponentExtensionOption>();
        var extensionMountSites = new List<ExtensionMountSiteOption>();

        foreach (var site in config.mountSites)
        {
            if (site.path.EndsWith(pathMatchSuffix))
            {
                var prefix = site.path[..^pathMatchSuffix.Length];
                pageExtensions.Add(new PageExtensionOption
                {
                    routePrefix = prefix,
                    autoSelector = site.selectors.Count > 0 ? site.selectors[0].selector : string.Empty
                });
            }
            else if (site.selectors.Any(s => s.type == RemotePortalConfigService.ExtensionMountType))
            {
                // Slots contributed by a local extension's BeamExtensionSite declarations; keyed by
                // the host extension's name rather than a Portal route.
                extensionMountSites.Add(new ExtensionMountSiteOption
                {
                    extensionName = site.path,
                    selectors = site.selectors
                        .Select(s => new ComponentSelectorOption { selector = s.selector, type = s.type })
                        .ToList()
                });
            }
            else
            {
                componentExtensions.Add(new ComponentExtensionOption
                {
                    path = site.path,
                    selectors = site.selectors
                        .Select(s => new ComponentSelectorOption { selector = s.selector, type = s.type })
                        .ToList()
                });
            }
        }

        return new PortalExtensionOptionsResult
        {
            pageExtensions = pageExtensions,
            componentExtensions = componentExtensions,
            extensionMountSites = extensionMountSites
        };
    }
}
