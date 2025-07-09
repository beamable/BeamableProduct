using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserSaveCommand : AtomicCommand<DeveloperUserSaveArgs, DeveloperUserResult>
{
	public DeveloperUserSaveCommand() : base("save-user", "")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOptionList("access-token", ""), (args, s) => { args.AccessToken = s.ToList(); } );
		AddOption(new ConfigurableOptionList("refresh-token", ""), (args, s) => { args.RefreshToken = s.ToList(); } );
		AddOption(new ConfigurableOptionList("pid", ""), (args, s) => { args.Pid = s.ToList(); } );
		AddOption(new ConfigurableOptionList("cid", ""), (args, s) => { args.Cid = s.ToList(); } );
		AddOption(new ConfigurableOptionList("gamer-tag", ""), (args, s) => { args.GamerTag = s.ToList(); } );
	}
	
	public override async Task<DeveloperUserResult> GetResult(DeveloperUserSaveArgs args)
	{
		List<DeveloperUser> developerUsers = new List<DeveloperUser>();
		
		for (int i = 0; i < args.GamerTag.Count; i++)
		{
			DeveloperUser developerUser = new DeveloperUser()
			{
				AccessToken = args.AccessToken[i],
				RefreshToken = args.RefreshToken[i],
				Pid = args.Pid[i],
				Cid = args.Cid[i],
				GamerTag = long.Parse(args.GamerTag[i]),
			};
			
			developerUsers.Add(developerUser);
		}


		await args.DeveloperUserManagerService.SaveDeveloperUsers(developerUsers);

		return new DeveloperUserResult() { SavedUsers = developerUsers };
	}
}

public class DeveloperUserSaveArgs : ContentCommandArgs
{
	public List<string> GamerTag;
	
	// Backend info
	public List<string> AccessToken;
	public List<string> RefreshToken;
	public List<string> Pid;
	public List<string> Cid;
}

