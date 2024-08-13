using Beamable.Common.Api;
using cli.Utils;

namespace cli.Portal;

public class PortalCommandArgs : CommandArgs
{
	
}


public class PortalCommand : AppCommand<PortalCommandArgs>
{
	public PortalCommand() : base("portal", "Open portal")
	{
	}

	public override void Configure()
	{
		
	}

	public override Task Handle(PortalCommandArgs args)
	{

		GetPortalBaseUrl(args, out var url, out var qb);
		url = $"{url}/{qb}";
		MachineHelper.OpenBrowser(url);

		return Task.CompletedTask;
	}

	public static void GetPortalBaseUrl(CommandArgs args, out string url, out QueryBuilder qb)
	{
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		qb = new QueryBuilder(args.Requester, new Dictionary<string, string>
		{
			["refresh_token"] = args.AppContext.RefreshToken
		});
		url = $"{args.AppContext.Host.Replace("dev.", "dev-").Replace("api", "portal")}/{cid}/games/{pid}/realms/{pid}";
	}
}
