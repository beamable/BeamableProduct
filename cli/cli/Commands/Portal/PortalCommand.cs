using System.CommandLine.Binding;
using Beamable.Common.Api;
using Beamable.Server;
using cli.Utils;

namespace cli.Portal;

public class PortalCommandArgs : CommandArgs
{
	
}


public class PortalCommand : AppCommand<PortalCommandArgs>
{
	public PortalCommand() : base("portal", "Open the Beamable Portal in a browser, auto-logged in with the current CID, PID and account credentials")
	{
	}

	public override void Configure()
	{
		
	}

	public override Task Handle(PortalCommandArgs args)
	{

		GetPortalRealmUrl(args, out var url, out var qb);
		url = $"{url}/{qb}";
		MachineHelper.OpenBrowser(url);

		return Task.CompletedTask;
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
