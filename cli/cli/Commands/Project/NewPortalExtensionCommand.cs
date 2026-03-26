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
				description: "Specify the page that the portal extension should added."),
				binder: (args, i) => args.mountPage = i);
		
		AddOption(new Option<string>(
				aliases: new string[] { "--mount-selector" },
				description: "Specify the place on the page that the portal extension should added."),
			binder: (args, i) => args.mountSelector = i);
		
		AddOption(new Option<string>(
				aliases: new string[] { "--mount-group" },
				description: "Specify the navigation group of the extension. This is only valid when the extension is a full page."),
			binder: (args, i) => args.mountGroup = i);
		
		AddOption(new Option<string>(
				aliases: new string[] { "--mount-label" },
				description: "Specify the navigation label of the extension. This is only valid when the extension is a full page."),
			binder: (args, i) => args.mountLabel = i);
		
		AddOption(new Option<string>(
				aliases: new string[] { "--mount-icon" },
				description: "Specify the Material Design Icon (mdi) that will be used for the extension's navigation. This is only valid when the extension is a full page."),
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
		{
			throw new CliException("Not all required dependencies exist. Aborting.");
		}

		var config = await ListMountSitesCommand.GetRemotePortalConfig(args);
		var pages = config.mountSites
			.ToDictionary(c => c.path);

		var customPages = new HashSet<string>();
		var customComponents = new Dictionary<string, RemotePortalConfiguration.MountSiteConfig>();
		foreach (var c in config.mountSites)
		{
			if (c.path.EndsWith("!pathMatch"))
			{
				// remove the custom pathMatch part, so we can prompt the user for a page later...
				customPages.Add(c.path.Substring(0, c.path.Length - "!pathMatch".Length));
			}
			else
			{
				customComponents[c.path] = c;
			}
		}
		
		
		var validPage = false;
		var validSelector = false;

		RemotePortalConfiguration.MountSiteConfig mountPage = null;
		RemotePortalConfiguration.MountSiteSelector selector = null;
		while (!validPage || !validSelector)
		{
			if (!string.IsNullOrEmpty(args.mountPage))
			{
				// check if it is a custom page...
				var match = customPages.FirstOrDefault(c =>
				{
					return
						// needs to be a prefix
						args.mountPage.StartsWith(c)
						// needs to have extra path information.
						&& args.mountPage.Length > c.Length + 1;
				});
				
				if (!string.IsNullOrEmpty(match))
				{
					// we found the custom page!
					mountPage = pages[match];
					args.mountPage = match;
					
					// use the default selector for this page.
					args.mountSelector = mountPage.selectors[0].selector;
				}
			}
			
			
			if (string.IsNullOrEmpty(args.mountPage) || !pages.TryGetValue(args.mountPage, out mountPage))
			{
				if (args.Quiet) throw new CliException("Must provider valid --mount-page when in quiet mode.");

				// does the user want to create a page extension, or a component?
				var typeChoice = AnsiConsole.Prompt(new SelectionPrompt<string>()
					.Title("What [green]type[/] of extension do you need?")
					.AddChoices(new string[]{"Page", "Component"})
					.AddBeamHightlight());

				if (typeChoice == "Page")
				{
					// show the pages that are full page extension.
					var firstPart = AnsiConsole.Prompt(
						new SelectionPrompt<string>()
							.Title("Which [green]page[/] on the Portal are you extending?")
							.AddChoices(customPages)
							.AddBeamHightlight()
					);
					
					// get the actual route name
					var secondPart = AnsiConsole.Ask<string>("What the new route for your page?");

					args.mountPage = firstPart + secondPart;
					continue; // skip back to the validation
				}
				else
				{
					// show the pages that have component extensions.
					var pageChoices = customComponents.Select(x => x.Key).ToList();
					args.mountPage = AnsiConsole.Prompt(
						new SelectionPrompt<string>()
							.Title("Which [green]page[/] on the Portal are you extending?")
							.AddChoices(pageChoices)
							.AddBeamHightlight()
					);
					if (!pages.TryGetValue(args.mountPage, out mountPage))
					{
						throw new CliException("Invalid mount page. Please contact a Beamable Staff");
					}
				}
			}

			validPage = true;

			var selectorTable = mountPage.selectors.ToDictionary(x => x.selector);
			if (string.IsNullOrEmpty(args.mountSelector) ||!selectorTable.TryGetValue(args.mountSelector, out selector))
			{
				if (args.Quiet) throw new CliException("Must provider valid --mount-selector when in quiet mode.");

				var selectorChoices = mountPage.selectors.Select(x => $"({x.type}) - {x.selector}").ToList();
				// TODO: add fast-forward if there is only one option.
				selectorChoices.Insert(0, "<-- (back)");

				var choice = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("Which [green]selector[/] on the page?")
						.AddChoices(selectorChoices)
						.AddBeamHightlight()
				);
				var choiceIndex = selectorChoices.IndexOf(choice);
				if (choiceIndex == 0)
				{
					args.mountPage = null;
					validPage = false;
					continue;
				}

				args.mountSelector = selectorChoices[choiceIndex];
				
				if (!selectorTable.TryGetValue(args.mountSelector, out selector))
				{
					throw new CliException("Invalid mount selector. Please contact a Beamable Staff");
				}
			}

			validSelector = true;
		}

		// TODO: if it is a page, we need group/label
		if (selector.type == "page")
		{
			if (string.IsNullOrEmpty(args.mountGroup))
			{
				if (args.Quiet) throw new CliException("Must provider valid --mount-group when in quiet mode.");
				args.mountGroup = AnsiConsole.Ask<string>("Nav Group:");
			}
			if (string.IsNullOrEmpty(args.mountLabel))
			{
				if (args.Quiet) throw new CliException("Must provider valid --mount-label when in quiet mode.");
				args.mountLabel = AnsiConsole.Ask<string>("Nav Label:");
			}
		}

		await args.CreateConfigIfNeeded(_initCommand);
		var newPortalExtensionInfo = await args.ProjectService.CreateNewPortalExtension(args);
		
		await args.BeamoLocalSystem.InitManifest();
		
		var extension = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions.FirstOrDefault(x =>
			(x.PortalExtensionDefinition?.Properties?.IsPortalExtension ?? false) && x.PortalExtensionDefinition.Name == args.ProjectName.Value);
		if (extension == null)
		{
			throw new CliException("Unable to verify that package.json was created");
		}
		
		var def = extension.PortalExtensionDefinition;
		var packageJson = File.ReadAllText(def.AbsolutePackageJsonPath);
		var jObj = JObject.Parse(packageJson);
		
		// pull out the beamable.mount options...
		var mounts = jObj.SelectTokens("$..beamable.mount").ToList();
		foreach (var mount in mounts)
		{
			if (mount is JObject obj)
			{
				obj[PortalExtensionMountProperties.KEY_PAGE] = args.mountPage;
				obj[PortalExtensionMountProperties.KEY_SELECTOR] = args.mountSelector;
				
				if (!string.IsNullOrEmpty(args.mountGroup))
					obj[PortalExtensionMountProperties.KEY_NAV_GROUP] = args.mountGroup;
				if (!string.IsNullOrEmpty(args.mountLabel))
					obj[PortalExtensionMountProperties.KEY_NAV_LABEL] = args.mountLabel;
				if (!string.IsNullOrEmpty(args.mountIcon))
					obj[PortalExtensionMountProperties.KEY_NAV_ICON] = args.mountIcon;
				if (args.mountGroupOrder > 0)
					obj[PortalExtensionMountProperties.KEY_NAV_GROUP_ORDER] = args.mountGroupOrder;
				if (args.mountLabelOrder > 0)
					obj[PortalExtensionMountProperties.KEY_NAV_LABEL_ORDER] = args.mountLabelOrder;
			}
		}
		File.WriteAllText(def.AbsolutePath, jObj.ToString(Newtonsoft.Json.Formatting.Indented));
	}
}
