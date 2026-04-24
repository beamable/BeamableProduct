using System.CommandLine;
using System.CommandLine.Binding;
using Beamable.Common.Api;
using Beamable.Server;
using cli.Services;
using cli.Utils;

namespace cli.Portal;

public class PortalCommandArgs : CommandArgs
{
	public string extension;
}


public class PortalCommand : AppCommand<PortalCommandArgs>
{
	public PortalCommand() : base("portal", "Open the Beamable Portal in a browser, auto-logged in with the current CID, PID and account credentials")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>(
				aliases: new[] { "--extension" },
				description: "Open a specific portal extension by name instead of the main portal page"),
			binder: (args, i) => args.extension = i);
	}

	public override async Task Handle(PortalCommandArgs args)
	{
		GetPortalRealmUrl(args, out var realmUrl, out var qb);

		if (!string.IsNullOrEmpty(args.extension))
		{
			await args.BeamoLocalSystem.InitManifest();
			var ext = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
				.FirstOrDefault(x =>
					(x.PortalExtensionDefinition?.Properties?.IsPortalExtension ?? false) &&
					x.PortalExtensionDefinition.Name == args.extension);

			if (ext == null)
				throw new CliException($"Portal extension '{args.extension}' not found in the local manifest.");

			var mountPage = BeamoLocalSystem.NormalizePortalExtensionMountPage(
				ext.PortalExtensionDefinition.Properties?.Mount?.Page);

			if (string.IsNullOrEmpty(mountPage))
				throw new CliException($"Portal extension '{args.extension}' has no mount page configured.");

			MachineHelper.OpenBrowser($"{realmUrl}/{mountPage}{qb}");
		}
		else
		{
			MachineHelper.OpenBrowser($"{realmUrl}/{qb}");
		}
	}

	public static string GetPortalBaseUrl(CommandArgs args)
	{
		var binding = args.DependencyProvider.GetService<BindingContext>();
		var portalUrl = binding.ParseResult.GetValueForOption(args.DependencyProvider.GetService<PortalUrlOption>());
		if (string.IsNullOrEmpty(portalUrl))
		{
			portalUrl = args.AppContext.Host.Replace("dev.", "dev-").Replace("api", "portal");
		}
		else
		{
			Log.Debug($"Using portal override=[{portalUrl}]");
		}

		return portalUrl;
	}

	public static void GetPortalRealmUrl(CommandArgs args, out string url, out QueryBuilder qb)
	{
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		qb = new QueryBuilder(args.Requester, new Dictionary<string, string>
		{
			["refresh_token"] = args.AppContext.RefreshToken
		});

		var portalUrl = GetPortalBaseUrl(args);
		url = $"{portalUrl}/{cid}/games/{pid}/realms/{pid}";
	}
}
