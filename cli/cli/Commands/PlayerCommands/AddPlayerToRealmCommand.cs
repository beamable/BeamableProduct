namespace cli.PlayerCommands;

public class AddPlayerToRealmCommandArgs : CommandArgs
{
	public long playerId;
	public string token;

	public string realm;
}

public class AddPlayerToRealmCommandOutput
{
	
}

public class AddPlayerToRealmCommand : AtomicCommand<AddPlayerToRealmCommandArgs, AddPlayerToRealmCommandOutput>
{
	public AddPlayerToRealmCommand() : base("add-to-realm", "add a player to a realm and find their gamertag in the realm")
	{
	}

	public override void Configure()
	{
		PlayerCommand.AddPlayerSpecifierArgs(this, (args, playerId, token) =>
		{
			args.playerId = playerId;
			args.token = token;
		});
	}

	public override Task<AddPlayerToRealmCommandOutput> GetResult(AddPlayerToRealmCommandArgs args)
	{
		var id = PlayerCommand.ResolvePlayerId(args, args.playerId, args.token);

		return Task.FromResult(new AddPlayerToRealmCommandOutput());
	}
}
