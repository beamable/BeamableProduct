using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserRemoveCommand : AtomicCommand<DeveloperUserRemoveArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserRemoveCommand() : base("remove-user", "Remove a user from the local files, it will not remove it from the portal")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("gamer-tag", "The gamer tag of the player that you would like to remove, it will not remove from the portal"), (args, s) => { args.GamerTag = s; });
	}
	

	public override Task<DeveloperUserResult> GetResult(DeveloperUserRemoveArgs args)
	{
		args.DeveloperUserManagerService.DeleteUser(args.GamerTag);
		
		return Task.FromResult(new DeveloperUserResult
		{
			DeletedUsers = new List<DeveloperUserData>()
			{
				new DeveloperUserData()
				{
					GamerTag = long.Parse(args.GamerTag)
				}
			}
		});
	}
}

public class DeveloperUserRemoveArgs : ContentCommandArgs
{
	public string GamerTag;
}
