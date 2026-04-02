using System.CommandLine;
using cli.Portal;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace cli.Commands.Project;

public class NewPortalExtensionCommandArgs : SolutionCommandArgs
{
	public string mountPage;
	public string mountSelector;
	public string mountIcon;
	public string mountGroup;
	public string mountLabel;
	public int mountGroupOrder;
	public int mountLabelOrder;
}


public class NewPortalExtensionCommand : AppCommand<NewPortalExtensionCommandArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;

	public NewPortalExtensionCommand(InitCommand initCommand) : base("portal-extension", "Creates a new Portal Extension App")
	{
		_initCommand = initCommand;
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		SolutionCommandArgs.Configure(this);

		AddOption(new Option<string>(
				aliases: new string[] { "--mount-page" },
				description: "Specify the page that the portal extension should added"),
				binder: (args, i) => args.mountPage = i);
		
		AddOption(new Option<string>(
				aliases: new string[] { "--mount-selector" },
				description: "Specify the place on the page that the portal extension should added"),
			binder: (args, i) => args.mountSelector = i);
		
		AddOption(new Option<string>(
				aliases: new string[] { "--mount-group" },
				description: "Specify the navigation group of the extension. This is only valid when the extension is a full page"),
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
	}

	public override async Task Handle(NewPortalExtensionCommandArgs args)
	{
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
			throw new CliException("Not all required dependencies exist. Aborting.");

		var config = await ListMountSitesCommand.GetRemotePortalConfig(args);
		BuildMountSiteIndex(config,
			out var customPagePrefixes,
			out var customPageConfigs,
			out var componentPages);

		RemotePortalConfiguration.MountSiteSelector resolvedSelector;

		var hasExplicitPage = !string.IsNullOrEmpty(args.mountPage);
		var hasExplicitSelector = !string.IsNullOrEmpty(args.mountSelector);

		if (hasExplicitPage && hasExplicitSelector)
		{
			resolvedSelector = ValidateMountArgs(args, customPagePrefixes, customPageConfigs, componentPages);
		}
		else if (hasExplicitPage || hasExplicitSelector)
		{
			throw new CliException("--mount-page and --mount-selector must both be provided together, or neither.");
		}
		else
		{
			if (args.Quiet) throw new CliException("Must provide --mount-page and --mount-selector when in quiet mode.");
			resolvedSelector = RunMountWizard(args, customPagePrefixes, customPageConfigs, componentPages);
		}

		if (resolvedSelector.type == "page")
		{
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

		foreach (var mount in jObj.SelectTokens("$..beamable.mount").OfType<JObject>().ToList())
		{
			mount[PortalExtensionMountProperties.KEY_PAGE] = args.mountPage;
			mount[PortalExtensionMountProperties.KEY_SELECTOR] = args.mountSelector;

			if (!string.IsNullOrEmpty(args.mountGroup))
				mount[PortalExtensionMountProperties.KEY_NAV_GROUP] = args.mountGroup;
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
		out List<string> customPagePrefixes,
		out Dictionary<string, RemotePortalConfiguration.MountSiteConfig> customPageConfigs,
		out Dictionary<string, RemotePortalConfiguration.MountSiteConfig> componentPages)
	{
		const string pathMatchSuffix = "!pathMatch";
		customPagePrefixes = new List<string>();
		customPageConfigs = new Dictionary<string, RemotePortalConfiguration.MountSiteConfig>();
		componentPages = new Dictionary<string, RemotePortalConfiguration.MountSiteConfig>();

		foreach (var site in config.mountSites)
		{
			if (site.path.EndsWith(pathMatchSuffix))
			{
				var prefix = site.path[..^pathMatchSuffix.Length];
				customPagePrefixes.Add(prefix);
				customPageConfigs[prefix] = site;
			}
			else
			{
				componentPages[site.path] = site;
			}
		}
	}

	private static RemotePortalConfiguration.MountSiteSelector ValidateMountArgs(
		NewPortalExtensionCommandArgs args,
		List<string> customPagePrefixes,
		Dictionary<string, RemotePortalConfiguration.MountSiteConfig> customPageConfigs,
		Dictionary<string, RemotePortalConfiguration.MountSiteConfig> componentPages)
	{
		// Check if --mount-page is a custom page extension (starts with a known prefix + has extra path)
		var matchingPrefix = customPagePrefixes.FirstOrDefault(prefix =>
			args.mountPage.StartsWith(prefix) && args.mountPage.Length > prefix.Length);

		if (matchingPrefix != null)
		{
			var siteConfig = customPageConfigs[matchingPrefix];
			var selector = siteConfig.selectors.FirstOrDefault(s => s.selector == args.mountSelector);
			if (selector == null)
				throw new CliException(
					$"Invalid --mount-selector '{args.mountSelector}' for page '{args.mountPage}'. " +
					$"Valid selectors: {string.Join(", ", siteConfig.selectors.Select(s => s.selector))}");
			return selector;
		}

		// Check if --mount-page is a component page
		if (componentPages.TryGetValue(args.mountPage, out var componentConfig))
		{
			var selector = componentConfig.selectors.FirstOrDefault(s => s.selector == args.mountSelector);
			if (selector == null)
				throw new CliException(
					$"Invalid --mount-selector '{args.mountSelector}' for page '{args.mountPage}'. " +
					$"Valid selectors: {string.Join(", ", componentConfig.selectors.Select(s => s.selector))}");
			return selector;
		}

		throw new CliException(
			$"Invalid --mount-page '{args.mountPage}'. " +
			$"Must be a known component page or a custom route under: " +
			string.Join(", ", customPagePrefixes.Select(p => $"{p}<route>")));
	}

	private static RemotePortalConfiguration.MountSiteSelector RunMountWizard(
		NewPortalExtensionCommandArgs args,
		List<string> customPagePrefixes,
		Dictionary<string, RemotePortalConfiguration.MountSiteConfig> customPageConfigs,
		Dictionary<string, RemotePortalConfiguration.MountSiteConfig> componentPages)
	{
		const string back = "<-- (back)";

		while (true)
		{
			var extensionType = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("What [green]type[/] of extension do you need?")
					.AddChoices("Page", "Component")
					.AddBeamHightlight());

			if (extensionType == "Page")
			{
				// Build display list: empty-string prefix shown as "/"
				const string rootDisplay = "/";
				var prefixDisplays = customPagePrefixes
					.Select(p => string.IsNullOrEmpty(p) ? rootDisplay : p)
					.ToList();

				var prefixDisplayChoice = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("Which [green]page[/] on the Portal are you extending?")
						.AddChoices(prefixDisplays.Prepend(back))
						.AddBeamHightlight());

				if (prefixDisplayChoice == back) continue;

				var prefixChoice = prefixDisplayChoice == rootDisplay ? string.Empty : prefixDisplayChoice;

				// Ask for the custom route segment to append
				var customRoute = AnsiConsole.Ask<string>("What is the new route for your page?");

				if (string.IsNullOrWhiteSpace(customRoute))
				{
					AnsiConsole.MarkupLine("[red]Route cannot be empty.[/]");
					continue;
				}

				args.mountPage = prefixChoice + customRoute.TrimStart('/');

				// Use the first selector from the !pathMatch config entry
				var siteConfig = customPageConfigs[prefixChoice];
				var selector = siteConfig.selectors[0];
				args.mountSelector = selector.selector;
				return selector;
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
