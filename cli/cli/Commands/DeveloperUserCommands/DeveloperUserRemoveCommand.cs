using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserRemoveCommand : AtomicCommand<DeveloperUserRemoveArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserRemoveCommand() : base("remove-user", "Remove a user or a list from the local files, it will not remove it from the portal")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOptionList("gamer-tag", "The gamer tag of the player that you would like to remove, it will not remove from the portal"), (args, enumerable) =>
		{
			args.GamerTags = new List<string>();
			foreach (string gamerTag in enumerable)
			{
				args.GamerTags.Add(gamerTag);
			}
		});
	}
	

	public override Task<DeveloperUserResult> GetResult(DeveloperUserRemoveArgs args)
	{
		args.DeveloperUserManagerService.DeleteUsers(args.GamerTags);

		var deletedUsers = new List<DeveloperUserData>();

		foreach (string gamerTag in args.GamerTags)
		{
			deletedUsers.Add(new DeveloperUserData() { GamerTag = long.Parse(gamerTag) });
		}
		
		return Task.FromResult(new DeveloperUserResult
		{
			DeletedUsers = deletedUsers
		});
	}
}

public class DeveloperUserRemoveArgs : ContentCommandArgs
{
	public List<string> GamerTags;
}
