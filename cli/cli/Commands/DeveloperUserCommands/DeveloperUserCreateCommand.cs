using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserCreateCommand : AtomicCommand<DeveloperUserCreateArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserCreateCommand() : base("create-user", "Create a single user that can be from a template or just create new empty user")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableIntOption("rolling-buffer-size", "The max amount of captured users that you can have before starting to delete the oldest (0 means infinity)"), (args, s) => { args.RollingBufferSize = s; });
		AddOption(new ConfigurableOption("alias", "The alias is a chosen name for this player which is not the same as the player name in the backend"), (args, s) => { args.Alias = s; });
		AddOption(new ConfigurableOption("template", "A gamer tag to a template that will be used to copy the stats and inventory to the created player"), (args, s) => { args.TemplateGamerTag = s; });
		AddOption(new ConfigurableOption("description", "A shortly description of this new player"), (args, s) => { args.Description = s; });
		AddOption(new ConfigurableIntOption("user-type", "The user type of this player 0 - Captured 1 - Local 2 - Shared"), ((args, number) => { args.DeveloperUserType = (DeveloperUserType)number; }));
		AddOption(new ConfigurableOptionList("tags", "A list of tags to set in this new player (only locally)"), (args, s) =>
		{
			foreach (var tag in s)
			{
				args.Tags.Add(tag);
			}
		});
	}
	
	public override async Task<DeveloperUserResult> GetResult(DeveloperUserCreateArgs args)
	{
		var developerUser = await args.DeveloperUserManagerService.CreateUser(args.TemplateGamerTag, args.Alias, args.Description, args.Tags, args.DeveloperUserType, args.RollingBufferSize);
		return new DeveloperUserResult
		{
			CreatedUsers = new List<DeveloperUserData>
			{
				DeveloperUserManagerService.DeveloperUserToDeveloperUserData(developerUser)
			}
		};
	}
}

public class DeveloperUserCreateArgs : ContentCommandArgs
{
	public string TemplateGamerTag;
	public string Alias;
	public string Description;
	public DeveloperUserType DeveloperUserType;
	public readonly List<string> Tags = new List<string>();
	
	public int RollingBufferSize;
}
