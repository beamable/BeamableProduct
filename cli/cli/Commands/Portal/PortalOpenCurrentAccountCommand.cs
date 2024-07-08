using Beamable.Common;
using cli.TokenCommands;
using cli.Utils;
using System.CommandLine;

namespace cli.Portal;

public class PortalOpenCurrentAccountCommandArgs : CommandArgs
{
	public long playerId;
	public string token;
}
public class PortalOpenCurrentAccountCommand : AppCommand<PortalOpenCurrentAccountCommandArgs>
{
	public PortalOpenCurrentAccountCommand() : base("player", "open portal to a player page")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<long>("--player-id", "the playerId (gamerTag) for the player to open Portal."),
			(args, i) => args.playerId = i, new string[] { "-i", "-p", "-gt" });
		
		AddOption(new Option<string>("--token", "the token for the player to open Portal. Cannot be specified when --player-id is set."),
			(args, i) => args.token = i, new string[] { "-t" });
	}

	async Promise<long> ResolvePlayerId(PortalOpenCurrentAccountCommandArgs args)
	{
		var hasId = args.playerId != 0;
		var hasToken = !string.IsNullOrEmpty(args.token);
		if (hasId && hasToken)
		{
			throw new CliException("Cannot pass both player id and token");
		}
		
		
		if (hasId) 
			return args.playerId; // always use explicit id

		if (hasToken)
		{
			// resolve the token
			var (token, _) = await GetTokenDetailsCommand.ResolveToken(args, true, args.token);
			return token.gamerTag;
		}
		
		// use the default token...
		var user = await args.AuthApi.GetUser();
		return user.id;
	}
	
	public override async Task Handle(PortalOpenCurrentAccountCommandArgs args)
	{
		var playerId = await ResolvePlayerId(args);
		
		// players/1651742188351488
		PortalCommand.GetPortalBaseUrl(args, out var url, out var qb);
		url = $"{url}/players/{playerId}/{qb}";
		MachineHelper.OpenBrowser(url);

	}
}
