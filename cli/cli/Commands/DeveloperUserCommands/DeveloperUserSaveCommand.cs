using Beamable.Common;
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
		AddOption(new ConfigurableOptionList("access-token", "The access token to be saved"), (args, s) => { args.AccessToken = s.ToList(); } );
		AddOption(new ConfigurableOptionList("refresh-token", "The refresh token to be saved"), (args, s) => { args.RefreshToken = s.ToList(); } );
		AddOption(new ConfigurableOptionList("pid", "The PID of the user"), (args, s) => { args.Pid = s.ToList(); } );
		AddOption(new ConfigurableOptionList("cid", "The CID of the user"), (args, s) => { args.Cid = s.ToList(); } );
		AddOption(new ConfigurableOptionList("gamer-tag", "The Gamer Tag of the user"), (args, s) => { args.GamerTag = s.ToList(); } );
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

		try{
			await args.DeveloperUserManagerService.SaveDeveloperUsers(developerUsers);
		}
		catch (Exception e) // Any generic exception on save the users
		{
			BeamableLogger.LogError(e);
				
			throw new CliException($"Generic error on save file", DeveloperUserManagerService.SAVE_FILE_ERROR, true);
		}
		return new DeveloperUserResult() { SavedUsers = DeveloperUserManagerService.DeveloperUsersToDeveloperUsersData(developerUsers).ToList() };
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

