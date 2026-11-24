using System.CommandLine;
using Beamable.Server;
using cli.Portal;
using cli.Services;
using cli.Services.PortalExtension;
using cli.Utils;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace cli.Commands.Project;

public class NewPortalExtensionCommandArgs : SolutionCommandArgs
{
	public string mountPage;
	public string[] sites;
	public string mountIcon;
	public string mountLabel;
	public string mountGroup;
	public int mountGroupOrder;
	public int mountLabelOrder;
	public string template;
	public bool IsZone;
	public string[] filters;
}

public static class PortalExtensionTemplates
{
	public const string React = "react";

	public static readonly string[] All = { React };

	/// <summary>
	/// Resolves the dotnet template short name for a given UI framework template. When
	/// <paramref name="isZone"/> is true, the zone-scoped variant is used — it scaffolds the
	/// front end against the zone registration APIs (<c>registerReactZoneExtension</c> /
	/// <c>useZoneBeam</c> / <c>ZoneExtensionContext</c>) so the extension receives a
	/// <c>BeamZoneSdk</c> (cid.zid) instead of a realm-scoped <c>Beam</c>.
	/// </summary>
	public static string ToDotnetTemplateShortName(string template, bool isZone = false) => template switch
	{
		React => isZone ? "portalextensionreactappzone" : "portalextensionreactapp",
		_ => throw new CliException($"Unknown portal-extension template '{template}'. Valid values: {string.Join(", ", All)}")
	};
}


public class NewPortalExtensionCommand : AppCommand<NewPortalExtensionCommandArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;

	public NewPortalExtensionCommand(InitCommand initCommand) : base("portal-extension",
			"Creates a new Portal Extension App. Before calling this, run 'portal extension list-extension-options' to discover valid --mount-page and --site values")
	{
		_initCommand = initCommand;
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		SolutionCommandArgs.Configure(this);

		AddOption(new Option<string>(
				aliases: new string[] { "--mount-page" },
				description: "The portal page to mount on. Required for page extensions: routePrefix + your custom route. Optional with --site: for a route-tabs site, the tab's page relative to the route the site renders at; usually omitted for stack and tabs sites. A --zone extension is declared with a zone-relative page path and renders inside the portal's zone view (/zones/:zid/) — the portal owns the prefix. Run 'portal extension list-extension-options' to see all valid values"),
				binder: (args, i) => args.mountPage = i);

		AddOption(new Option<string[]>(
				aliases: new string[] { "--site" },
				getDefaultValue: Array.Empty<string>,
				description: "The id of a site to mount into (repeat for several), e.g. --site campaign-offer. The mount shows in every instance of the site. Omit for page extensions (they mount into extension-page). Run 'portal extension list-extension-options' to see the sites"),
			binder: (args, i) => args.sites = i);

		AddOption(new Option<string>(
				aliases: new string[] { "--mount-group" },
				description: "Specify the navigation group of the extension, used to organize extensions within a hub. This is required when the extension is a full page"),
			binder: (args, i) => args.mountGroup = i);

		AddOption(new Option<string>(
				aliases: new string[] { "--mount-label" },
				description: "Specify the navigation label of the extension. This is only valid when the extension is a full page"),
			binder: (args, i) => args.mountLabel = i);
		
		AddOption(new Option<string>(
				aliases: new string[] { "--mount-icon" },
				description: "Specify the Material Design Icon (mdi) that will be used for the extension's navigation. This is only valid when the extension is a full page"),
			binder: (args, i) => args.mountIcon = i);
		
		AddOption(new Option<int>(
				aliases: new string[] { "--mount-group-order" },
				description: "Specify the order of the mount group"),
			binder: (args, i) => args.mountGroupOrder = i);
		
		AddOption(new Option<int>(
				aliases: new string[] { "--mount-label-order" },
				description: "Specify the order of the mount label"),
			binder: (args, i) => args.mountLabelOrder = i);

		AddOption(new Option<string>(
				aliases: new string[] { "--template" },
				getDefaultValue: () => PortalExtensionTemplates.React,
				description: "UI framework template to scaffold the extension with. Allowed values: react"),
			binder: (args, i) => args.template = i);

		AddOption(new Option<bool>(
				name: "--zone",
				description: "If passed, creates a zone-scoped portal extension (its backing service runs as a ZoneMicroservice, per cid.zid) instead of a realm-scoped one"),
			(args, i) => args.IsZone = i);

		AddOption(new Option<string[]>(
				aliases: new string[] { "--filters" },
				getDefaultValue: Array.Empty<string>,
				description: "Conditional mount filters as key=value pairs (e.g. --filters account.role=developer|admin account.tier=default). Values are regex-matched by the portal against the viewer's context, so | alternation is valid; quote entries containing | so your shell doesn't interpret it. Merged over the template defaults, so unspecified keys keep their defaults")
			{
				AllowMultipleArgumentsPerToken = true
			},
			binder: (args, i) => args.filters = i);
	}

	// Parse --filters "key=value" entries. Split on the first '=' only so regex
	// values (which may contain '=' or '|') survive verbatim; the portal treats
	// each value as a regex, so the CLI must not interpret it.
	private static Dictionary<string, string> ParseFilterOverrides(string[] filters)
	{
		var result = new Dictionary<string, string>();
		if (filters == null)
		{
			return result;
		}

		foreach (var entry in filters)
		{
			var separatorIndex = entry.IndexOf('=');
			if (separatorIndex <= 0)
			{
				throw new CliException($"Invalid --filters entry '{entry}'. Expected format key=value, e.g. account.role=developer|admin");
			}

			var key = entry.Substring(0, separatorIndex).Trim();
			var value = entry.Substring(separatorIndex + 1);
			if (string.IsNullOrEmpty(key))
			{
				throw new CliException($"Invalid --filters entry '{entry}'. The key must not be empty, e.g. account.role=developer|admin");
			}

			result[key] = value;
		}

		return result;
	}

	public override async Task Handle(NewPortalExtensionCommandArgs args)
	{
		// Validate required arg pairs before any expensive I/O so errors surface immediately
		// (dependency checks and the remote portal config fetch can take many seconds).
		var siteIds = ValidateMountArgs(args.sites, args.mountPage, args.Quiet);

		// Validate --filters formatting up front so a typo fails before scaffolding.
		var filterOverrides = ParseFilterOverrides(args.filters);

		// Validate the chosen name against existing local microservices, storages, and portal extensions
		// up front — before any interactive prompts or the remote portal config fetch — so the user
		// isn't asked for mount details only to have creation fail. The manifest is already initialized
		// by the framework before Handle runs; deployed names are checked at deploy time.
		if (PortalExtensionNameValidator.TryGetConflictForNewName(args.BeamoLocalSystem.BeamoManifest, args.ProjectName.Value, out var nameConflict))
			throw new CliException(nameConflict);

		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
			throw new CliException("Not all required dependencies exist. Aborting.");

		args.template = args.template.ToLowerInvariant();
		if (!PortalExtensionTemplates.All.Contains(args.template))
			throw new CliException($"Invalid --template value '{args.template}'. Allowed values: {string.Join(", ", PortalExtensionTemplates.All)}");

		var configService = args.DependencyProvider.GetService<IRemotePortalConfigService>();
		var config = await configService.GetRemotePortalConfig(args);

		if (siteIds.Count == 0)
			siteIds = RunMountWizard(args, config);
		else if (siteIds.Contains(PortalExtensionSiteDeclaration.ExtensionPageSiteId) && string.IsNullOrEmpty(args.mountPage))
			args.mountPage = AskPagePath();

		WarnUnknownSites(siteIds, config);

		if (siteIds.Contains(PortalExtensionSiteDeclaration.ExtensionPageSiteId))
		{
			// A zone extension's mount page is stored zone-relative and verbatim — the portal
			// auto-assumes the full `:cid/zones/:zid/` prefix when rendering it in the zone view,
			// so the CLI must not prepend anything here.

			// Full-page (hub) extensions need a nav group and a display label. The hub hierarchy comes
			// from the page path itself (e.g. "cars" vs "cars/ferrari"); the nav group is a separate way
			// to organize extensions within a hub, and is required for page extensions.
			if (string.IsNullOrEmpty(args.mountGroup))
			{
				if (args.Quiet) throw new CliException("Must provide --mount-group when in quiet mode.");
				args.mountGroup = AnsiConsole.Ask<string>("Nav Group:");
			}
			if (string.IsNullOrEmpty(args.mountLabel))
			{
				if (args.Quiet) throw new CliException("Must provide --mount-label when in quiet mode.");
				args.mountLabel = AnsiConsole.Ask<string>("Nav Label:");
			}
		}

		await args.CreateConfigIfNeeded(_initCommand);
		var newPortalExtensionInfo = await args.ProjectService.CreateNewPortalExtension(args);

		await args.BeamoLocalSystem.InitManifest();

		var extension = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(x =>
			(x.PortalExtensionDefinition?.Properties?.IsPortalExtension ?? false) &&
			x.PortalExtensionDefinition.Name == args.ProjectName.Value);
		if (extension == null)
			throw new CliException("Unable to verify that package.json was created");

		var def = extension.PortalExtensionDefinition;
		var packageJson = File.ReadAllText(def.AbsolutePackageJsonPath);
		var jObj = JObject.Parse(packageJson);

		// The template writes a single-entry `mounts` array; the scaffold-time
		// CLI flags patch entry [0] of that array. If multi-mount scaffolding
		// is ever needed, add an explicit --mount-index flag or accept JSON.
		foreach (var mount in jObj.SelectTokens("$..beamable.mounts[0]").OfType<JObject>().ToList())
		{
			mount[PortalExtensionMountProperties.KEY_SITE_IDS] = new JArray(siteIds);
			if (string.IsNullOrEmpty(args.mountPage))
				mount.Remove(PortalExtensionMountProperties.KEY_PAGE);
			else
				mount[PortalExtensionMountProperties.KEY_PAGE] = args.mountPage;
			// The selector model is gone; never leave one behind.
			mount.Remove("selector");

			if (!string.IsNullOrEmpty(args.mountLabel))
				mount[PortalExtensionMountProperties.KEY_NAV_LABEL] = args.mountLabel;
			if (!string.IsNullOrEmpty(args.mountGroup))
				mount[PortalExtensionMountProperties.KEY_NAV_GROUP] = args.mountGroup;
			if (!string.IsNullOrEmpty(args.mountIcon))
				mount[PortalExtensionMountProperties.KEY_NAV_ICON] = args.mountIcon;
			if (args.mountGroupOrder > 0)
				mount[PortalExtensionMountProperties.KEY_NAV_GROUP_ORDER] = args.mountGroupOrder;
			if (args.mountLabelOrder > 0)
				mount[PortalExtensionMountProperties.KEY_NAV_LABEL_ORDER] = args.mountLabelOrder;

			// Merge over the template's default filters so unspecified keys keep
			// their defaults (e.g. supplying only account.role leaves account.tier intact).
			if (filterOverrides.Count > 0)
			{
				var filtersObj = mount[PortalExtensionMountProperties.KEY_FILTERS] as JObject ?? new JObject();
				foreach (var kvp in filterOverrides)
				{
					filtersObj[kvp.Key] = kvp.Value;
				}
				mount[PortalExtensionMountProperties.KEY_FILTERS] = filtersObj;
			}
		}

		File.WriteAllText(def.AbsolutePackageJsonPath, jObj.ToString(Newtonsoft.Json.Formatting.Indented));
	}

	/// <summary>
	/// Checks the mount flags before any I/O and returns the site ids to write, once each. A site id is
	/// bare (no <c>#</c>, no <c>@</c>). A page is required only for <c>extension-page</c>; with no
	/// <c>--site</c>, a <c>--mount-page</c> makes a page extension. Quiet mode needs one or the other.
	/// An empty result means the wizard picks the mount.
	/// </summary>
	public static List<string> ValidateMountArgs(string[] sites, string mountPage, bool quiet)
	{
		var siteIds = (sites ?? Array.Empty<string>())
			.Select(id => id?.Trim())
			.Where(id => !string.IsNullOrEmpty(id))
			.Distinct()
			.ToList();

		foreach (var id in siteIds)
		{
			if (id.IndexOfAny(new[] { '#', '@' }) >= 0)
				throw new CliException(
					$"Invalid --site '{id}': a site id is a bare name, with no '#' and no '@' (e.g. --site {id.TrimStart('#').Split('@')[0]}).");
		}

		var hasPage = !string.IsNullOrEmpty(mountPage);
		if (siteIds.Count == 0 && hasPage)
			return new List<string> { PortalExtensionSiteDeclaration.ExtensionPageSiteId };

		if (quiet)
		{
			if (siteIds.Count == 0)
				throw new CliException("Must provide --mount-page (a page) or --site (a site) when running with -q / --quiet. Run 'portal extension list-extension-options' to discover valid pages and sites.");
			if (!hasPage && siteIds.Contains(PortalExtensionSiteDeclaration.ExtensionPageSiteId))
				throw new CliException($"--site {PortalExtensionSiteDeclaration.ExtensionPageSiteId} requires --mount-page.");
		}

		return siteIds;
	}

	private static string AskPagePath()
	{
		while (true)
		{
			// The page path is passed through verbatim — the Portal parses the hub hierarchy from it.
			var pagePath = AnsiConsole.Ask<string>("What is the [green]page path[/]?");
			if (!string.IsNullOrWhiteSpace(pagePath))
				return pagePath.Trim().TrimStart('/');
			AnsiConsole.MarkupLine("[red]Page path cannot be empty.[/]");
		}
	}

	// A site nobody in the Host declares is dropped by the portal at load; say so, but let the author
	// scaffold against an extension that isn't running here.
	private static void WarnUnknownSites(List<string> siteIds, RemotePortalConfiguration config)
	{
		var known = new HashSet<string>(config.sites.Select(s => s.id)) { PortalExtensionSiteDeclaration.ExtensionPageSiteId };
		foreach (var id in siteIds.Where(id => !known.Contains(id)))
		{
			Log.Warning(
				"Site '{siteId}' isn't declared by the portal or any running extension. The portal ignores it until an extension that declares it is loaded. Run 'portal extension list-extension-options' to see the sites.",
				id);
		}
	}

	private static List<string> RunMountWizard(NewPortalExtensionCommandArgs args, RemotePortalConfiguration config)
	{
		const string back = "<-- (back)";

		var sites = config.sites
			.Where(s => s.id != PortalExtensionSiteDeclaration.ExtensionPageSiteId)
			.ToList();

		while (true)
		{
			var typeChoices = new List<string> { "Page" };
			// Only offer a site when the Host has one besides the page site.
			if (sites.Count > 0) typeChoices.Add("Site");

			var extensionType = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("What [green]type[/] of extension do you need?")
					.AddChoices(typeChoices)
					.AddBeamHightlight());

			if (extensionType == "Page")
			{
				args.mountPage = AskPagePath();
				return new List<string> { PortalExtensionSiteDeclaration.ExtensionPageSiteId };
			}

			var displays = sites
				.Select(s => Markup.Escape(
					$"{s.id} ({s.presentation ?? "?"}) — rendered by {string.Join(", ", s.renderedBy)}" +
					(string.IsNullOrEmpty(s.description) ? "" : $": {s.description}")))
				.ToList();

			var choice = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("Which [green]site[/] does it mount into?")
					.AddChoices(displays.Prepend(back))
					.AddBeamHightlight());

			if (choice == back) continue; // back to type selection

			var site = sites[displays.IndexOf(choice)];
			if (site.presentation == PortalExtensionSiteDeclaration.PRESENTATION_ROUTE_TABS)
			{
				// A route-tabs mount brings the tab's page, relative to the route the site renders at.
				var tabPage = AnsiConsole.Ask<string>(
					"What is the tab's [green]page[/], relative to the route the site renders at (e.g. stats)?");
				args.mountPage = string.IsNullOrWhiteSpace(tabPage) ? null : tabPage.Trim().Trim('/');
			}
			else
			{
				args.mountPage = null;
			}

			return new List<string> { site.id };
		}
	}
}
