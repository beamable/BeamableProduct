using System.CommandLine;
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
	public string mountSelector;
	public string mountIcon;
	public string mountLabel;
	public int mountGroupOrder;
	public int mountLabelOrder;
	public string template;
}

public static class PortalExtensionTemplates
{
	public const string React = "react";

	public static readonly string[] All = { React };

	public static string ToDotnetTemplateShortName(string template) => template switch
	{
		React => "portalextensionreactapp",
		_ => throw new CliException($"Unknown portal-extension template '{template}'. Valid values: {string.Join(", ", All)}")
	};
}


public class NewPortalExtensionCommand : AppCommand<NewPortalExtensionCommandArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;

	public NewPortalExtensionCommand(InitCommand initCommand) : base("portal-extension",
			"Creates a new Portal Extension App. Before calling this, run 'portal extension list-extension-options' to discover valid --mount-page and --mount-selector values")
	{
		_initCommand = initCommand;
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		SolutionCommandArgs.Configure(this);

		AddOption(new Option<string>(
				aliases: new string[] { "--mount-page" },
				description: "The portal page to mount on. For page extensions use routePrefix + your custom route; for component extensions use the page path. Run 'portal extension list-extension-options' to see all valid values"),
				binder: (args, i) => args.mountPage = i);

		AddOption(new Option<string>(
				aliases: new string[] { "--mount-selector" },
				description: "The mount slot on the page. Required for component extensions; omit for page extensions (auto-assigned). Run 'portal extension list-extension-options' to see valid selectors per page"),
			binder: (args, i) => args.mountSelector = i);
		
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
	}

	public override async Task Handle(NewPortalExtensionCommandArgs args)
	{
		// Validate required arg pairs before any expensive I/O so errors surface immediately
		// (dependency checks and the remote portal config fetch can take many seconds).
		var hasExplicitPage = !string.IsNullOrEmpty(args.mountPage);
		var hasExplicitSelector = !string.IsNullOrEmpty(args.mountSelector);

		if (!hasExplicitPage && hasExplicitSelector)
			throw new CliException("--mount-selector requires --mount-page to also be specified.");

		if (!hasExplicitPage && args.Quiet)
			throw new CliException("Must provide --mount-page when running with -q / --quiet. Run 'portal extension list-extension-options' to discover valid pages and selectors.");

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
		BuildMountSiteIndex(config,
			out var pageSelector,
			out var componentPages);

		RemotePortalConfiguration.MountSiteSelector resolvedSelector;

		if (hasExplicitPage)
		{
			resolvedSelector = ValidateMountArgs(args, pageSelector, componentPages);
		}
		else
		{
			resolvedSelector = RunMountWizard(args, pageSelector, componentPages);
		}

		if (resolvedSelector.type == "page")
		{
			// Full-page (hub) extensions need a display label; the hub hierarchy comes from the
			// page path itself (e.g. "cars" vs "cars/ferrari"), so there is no separate nav group.
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
			mount[PortalExtensionMountProperties.KEY_PAGE] = args.mountPage;
			mount[PortalExtensionMountProperties.KEY_SELECTOR] = args.mountSelector;

			if (!string.IsNullOrEmpty(args.mountLabel))
				mount[PortalExtensionMountProperties.KEY_NAV_LABEL] = args.mountLabel;
			if (!string.IsNullOrEmpty(args.mountIcon))
				mount[PortalExtensionMountProperties.KEY_NAV_ICON] = args.mountIcon;
			if (args.mountGroupOrder > 0)
				mount[PortalExtensionMountProperties.KEY_NAV_GROUP_ORDER] = args.mountGroupOrder;
			if (args.mountLabelOrder > 0)
				mount[PortalExtensionMountProperties.KEY_NAV_LABEL_ORDER] = args.mountLabelOrder;
		}

		File.WriteAllText(def.AbsolutePackageJsonPath, jObj.ToString(Newtonsoft.Json.Formatting.Indented));
	}

	private static void BuildMountSiteIndex(
		RemotePortalConfiguration config,
		out RemotePortalConfiguration.MountSiteSelector pageSelector,
		out Dictionary<string, RemotePortalConfiguration.MountSiteConfig> componentPages)
	{
		const string pathMatchSuffix = "!pathMatch";
		pageSelector = null;
		componentPages = new Dictionary<string, RemotePortalConfiguration.MountSiteConfig>();

		foreach (var site in config.mountSites)
		{
			if (site.path.EndsWith(pathMatchSuffix))
			{
				// The full-page slot (e.g. "!hub/!pathMatch" -> "#extension-page"). The page path a
				// user supplies is passed through verbatim — the Portal parses the hub hierarchy from
				// it (e.g. "cars" is a hub, "cars/ferrari" nests under it) — so we only need this entry
				// to source the page selector to auto-assign. Every such slot uses the same selector.
				if (pageSelector == null && site.selectors.Count > 0)
					pageSelector = site.selectors[0];
			}
			else
			{
				// Includes slots contributed by other running extensions' BeamExtensionSite
				// declarations — they are ordinary, uniquely-named component selectors at a URL.
				componentPages[site.path] = site;
			}
		}
	}

	private static RemotePortalConfiguration.MountSiteSelector ValidateMountArgs(
		NewPortalExtensionCommandArgs args,
		RemotePortalConfiguration.MountSiteSelector pageSelector,
		Dictionary<string, RemotePortalConfiguration.MountSiteConfig> componentPages)
	{
		// A known component page → mount as a component into one of its slots (selector required).
		if (componentPages.TryGetValue(args.mountPage, out var componentConfig))
		{
			if (string.IsNullOrEmpty(args.mountSelector))
				throw new CliException(
					$"--mount-selector is required for component pages. " +
					$"Valid selectors for '{args.mountPage}': {string.Join(", ", componentConfig.selectors.Select(s => s.selector))}");
			var selector = componentConfig.selectors.FirstOrDefault(s => s.selector == args.mountSelector);
			if (selector == null)
				throw new CliException(
					$"Invalid --mount-selector '{args.mountSelector}' for page '{args.mountPage}'. " +
					$"Valid selectors: {string.Join(", ", componentConfig.selectors.Select(s => s.selector))}");
			return selector;
		}

		// Anything else is a full-page extension. The page path is passed through verbatim — the
		// Portal parses the hub hierarchy from it ("cars" is a hub; "cars/ferrari" nests under the
		// "cars" hub) — and the page slot selector is auto-assigned.
		if (pageSelector == null)
			throw new CliException(
				"No page mount slot is available from the Portal config, so a full-page extension cannot be created. " +
				"Run 'portal extension list-extension-options' to see valid component pages and selectors.");
		args.mountSelector = pageSelector.selector;
		return pageSelector;
	}

	private static RemotePortalConfiguration.MountSiteSelector RunMountWizard(
		NewPortalExtensionCommandArgs args,
		RemotePortalConfiguration.MountSiteSelector pageSelector,
		Dictionary<string, RemotePortalConfiguration.MountSiteConfig> componentPages)
	{
		const string back = "<-- (back)";

		while (true)
		{
			var typeChoices = new List<string>();
			// Only offer a full page when the Portal actually exposes a page slot.
			if (pageSelector != null) typeChoices.Add("Page");
			typeChoices.Add("Component");

			var extensionType = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("What [green]type[/] of extension do you need?")
					.AddChoices(typeChoices)
					.AddBeamHightlight());

			if (extensionType == "Page")
			{
				// The page path is passed through verbatim — the Portal parses the hub hierarchy
				var pagePath = AnsiConsole.Ask<string>(
					"What is the [green]page path[/]?");

				if (string.IsNullOrWhiteSpace(pagePath))
				{
					AnsiConsole.MarkupLine("[red]Page path cannot be empty.[/]");
					continue;
				}

				args.mountPage = pagePath.Trim().TrimStart('/');
				args.mountSelector = pageSelector.selector;
				return pageSelector;
			}
			else // Component
			{
				while (true)
				{
					var pageChoice = AnsiConsole.Prompt(
						new SelectionPrompt<string>()
							.Title("Which [green]page[/] on the Portal are you extending?")
							.AddChoices(componentPages.Keys.Prepend(back))
							.AddBeamHightlight());

					if (pageChoice == back) break; // back to type selection

					var siteConfig = componentPages[pageChoice];
					args.mountPage = pageChoice;

					// Auto-select if there is only one selector
					if (siteConfig.selectors.Count == 1)
					{
						var onlySelector = siteConfig.selectors[0];
						args.mountSelector = onlySelector.selector;
						return onlySelector;
					}

					// Build display list keyed to original selectors
					var selectorDisplays = siteConfig.selectors
						.Select(s => $"({s.type}) {s.selector}")
						.ToList();

					var selectorChoice = AnsiConsole.Prompt(
						new SelectionPrompt<string>()
							.Title("Which [green]selector[/] on the page?")
							.AddChoices(selectorDisplays.Prepend(back))
							.AddBeamHightlight());

					if (selectorChoice == back) continue; // back to page selection

					var chosenSelector = siteConfig.selectors[selectorDisplays.IndexOf(selectorChoice)];
					args.mountSelector = chosenSelector.selector;
					return chosenSelector;
				}
			}
		}
	}
}
