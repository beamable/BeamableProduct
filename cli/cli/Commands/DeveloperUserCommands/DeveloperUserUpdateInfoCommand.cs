using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserUpdateInfoCommand : AtomicCommand<DeveloperUserUpdateInfoArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserUpdateInfoCommand() : base("update-info", "")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("alias", ""), (args, s) => { args.Alias = s; });
		AddOption(new ConfigurableOption("identifier", ""), (args, s) => { args.Identifier = s; });
		AddOption(new ConfigurableOption("description", ""), (args, s) => { args.Description = s; });
		AddOption(new ConfigurableOptionFlag("create-copy-on-start", ""), (args, b) => { args.CreateCopyOnStart = b; });
		AddOption(new ConfigurableOptionList("tags", ""), (args, s) =>
		{
			foreach (var tag in s)
			{
				args.Tags.Add(tag);
			}
		});
	}
	
	public override async Task<DeveloperUserResult> GetResult(DeveloperUserUpdateInfoArgs args)
	{
		DeveloperUserInfo result = await args.DeveloperUserManagerService.UpdateUserInfo(args.Identifier, args.Alias, args.Description, args.CreateCopyOnStart, args.Tags);
		var resultDeveloperUser = result.DeveloperUser;
		return new DeveloperUserResult() {
			UpdatedUsers = new List<DeveloperUserData>()
			{
				new DeveloperUserData()
				{
					Alias = resultDeveloperUser.Alias,
					CreateByGamerTag = resultDeveloperUser.CreatedByGamerTag,
					Description = resultDeveloperUser.Description,
					CreateCopyOnStart = resultDeveloperUser.CreateCopyOnStart,
					DeveloperUserType = (int)result.UserType,
					GamerTag = resultDeveloperUser.GamerTag,
					TemplatedGamerTag = resultDeveloperUser.TemplateGamerTag,
					RefreshToken = resultDeveloperUser.RefreshToken,
					AccessToken = resultDeveloperUser.AccessToken,
					Cid = resultDeveloperUser.Cid,
					Pid = resultDeveloperUser.Pid,
					ExpiresIn = resultDeveloperUser.ExpiresIn,
					Tags = resultDeveloperUser.Tags
				}
			} 
		};
	}
}

public class DeveloperUserUpdateInfoArgs : ContentCommandArgs
{
	public string Identifier;
	public string Alias;
	public string Description;
	public bool CreateCopyOnStart;
	public readonly List<string> Tags = new List<string>();
}
