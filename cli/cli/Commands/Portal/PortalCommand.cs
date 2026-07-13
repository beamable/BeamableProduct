using System.CommandLine.Binding;
using Beamable.Common.Api;
using Beamable.Server;
using cli.Utils;

namespace cli.Portal;

public class PortalCommandArgs : CommandArgs
{
}

public enum PortalType
{
	LegacyPortal, 
	Console
}

public class PortalCommand : AppCommand<PortalCommandArgs>
{
	public PortalCommand() : base("portal", "Open the Beamable Portal in a browser, auto-logged in with the current CID, PID and account credentials")
	{
	}

	public override void Configure()
	{
	}

	public override async Task Handle(PortalCommandArgs args)
	{
		GetPortalRealmUrl(args, out var realmUrl, out var qb);
		MachineHelper.OpenBrowser($"{realmUrl}/{qb}");
	}

	public static string GetPortalBaseUrl(CommandArgs args, PortalType isNewPortal = PortalType.LegacyPortal)
	{
		var binding = args.DependencyProvider.GetService<BindingContext>();
		return GetPortalBaseUrl(binding, args.AppContext, isNewPortal);
	}

	public static string GetPortalBaseUrl(BindingContext binding, IAppContext appContext, PortalType isNewPortal = PortalType.LegacyPortal)
	{
		var portalUrl = binding.ParseResult.GetValueForOption(PortalUrlOption.Instance);
		if (string.IsNullOrEmpty(portalUrl))
		{
			if (isNewPortal == PortalType.Console)
			{
				portalUrl = appContext.Host.Replace("api", "console");
			}
			else
			{
				portalUrl = appContext.Host.Replace("dev.", "dev-").Replace("api", "portal");
			}

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
