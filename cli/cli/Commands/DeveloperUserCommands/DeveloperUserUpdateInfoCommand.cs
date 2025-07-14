using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserUpdateInfoCommand : AtomicCommand<DeveloperUserUpdateInfoArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserUpdateInfoCommand() : base("update-info", "Update the info of the developer user (Alias, Description etc)")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("alias", "A alias (Name) of the user, which is not the same name as in the portal"), (args, s) => { args.Alias = s; });
		AddOption(new ConfigurableOption("gamer-tag", "The gamer tag of the user to be updated"), (args, s) => { args.GamerTag = s; });
		AddOption(new ConfigurableOption("description", "A new description for this user"), (args, s) => { args.Description = s; });
		AddOption(new ConfigurableOptionList("tags", "The tags to set in the local user data"), (args, s) =>
		{
			foreach (var tag in s)
			{
				args.Tags.Add(tag);
			}
		});
	}
	
	public override async Task<DeveloperUserResult> GetResult(DeveloperUserUpdateInfoArgs args)
	{
		DeveloperUser resultDeveloperUser = await args.DeveloperUserManagerService.UpdateDeveloperUserInfo(args.GamerTag, args.Alias, args.Description, args.Tags);

		return new DeveloperUserResult() {
			UpdatedUsers = new List<DeveloperUserData>()
			{
				DeveloperUserManagerService.DeveloperUserToDeveloperUserData(resultDeveloperUser)
			} 
		};
	}
}

public class DeveloperUserUpdateInfoArgs : ContentCommandArgs
{
	public string GamerTag;
	public string Alias;
	public string Description;
	public readonly List<string> Tags = new List<string>();
}
