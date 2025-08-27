using Beamable.Common;
using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserSaveCommand : AtomicCommand<DeveloperUserSaveArgs, DeveloperUserResult>
{
	public DeveloperUserSaveCommand() : base("save-user", "Create a new developer user in the local files")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOptionList("alias", "A alias (Name) of the user, which is not the same name as in the portal"), (args, s) => { args.Alias = s.ToList(); });
		AddOption(new ConfigurableOptionList("description", "A new description for this user"), (args, s) => { args.Description = s.ToList(); });
		AddOption(new ConfigurableIntOption("user-type", "The user type of the user"), (args, s) => { args.DeveloperUserType = s; });
		AddOption(new ConfigurableOptionList("pid", "The PID of the user"), (args, s) => { args.Pid = s.ToList(); } );
		AddOption(new ConfigurableOptionList("cid", "The CID of the user"), (args, s) => { args.Cid = s.ToList(); } );
		AddOption(new ConfigurableOptionList("gamer-tag", "The Gamer Tag of the user"), (args, s) => { args.GamerTag = s.ToList(); } );
		AddOption(new ConfigurableOptionList("access-token", "The access token to be saved"), (args, s) => { args.AccessToken = s.ToList(); } );
		AddOption(new ConfigurableOptionList("refresh-token", "The refresh token to be saved"), (args, s) => { args.RefreshToken = s.ToList(); } );
		AddOption(new ConfigurableOptionList("expires-in", "The expires to be saved"), (args, enumerable) =>
		{
			args.ExpiresIn = new List<long>();
			foreach (string arg in enumerable)
			{
				args.ExpiresIn.Add(long.Parse(arg));
			}
		} );
		AddOption(new ConfigurableOptionList("tags", "The tags to set in the local user data splited by @@"), (args, s) =>
		{
			foreach (var userTags in s)
			{
				// we are using the @@ as a split character to the tags
				if (!string.IsNullOrEmpty(userTags) && userTags.Contains("@@"))
				{
					string[] tags = userTags.Split("@@");
					if (tags.Length > 0)
					{
						args.Tags.Add(new List<string>(tags));	
					}
					else
					{
						// we still need to add the user to the list
						args.Tags.Add(new List<string>());
					}
				}
				else
				{
					// we still need to add the user to the list
					args.Tags.Add(new List<string>());
				}
			}
		});
	}
	
	public override async Task<DeveloperUserResult> GetResult(DeveloperUserSaveArgs args)
	{
		List<DeveloperUser> developerUsers = new List<DeveloperUser>();
		
		for (int i = 0; i < args.GamerTag.Count; i++)
		{
			DeveloperUser developerUser = new DeveloperUser()
			{
				Alias = args.Alias[i],
				Description = args.Description[i],
				Tags = args.Tags[i],
				AccessToken = args.AccessToken[i],
				RefreshToken = args.RefreshToken[i],
				ExpiresIn = args.ExpiresIn[i],
				Pid = args.Pid[i],
				Cid = args.Cid[i],
				GamerTag = long.Parse(args.GamerTag[i]),
			};
			
			developerUsers.Add(developerUser);
		}

		try{
			await args.DeveloperUserManagerService.SaveDeveloperUsers(developerUsers, (DeveloperUserType)args.DeveloperUserType);
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
	public int DeveloperUserType;
	
	public List<string> GamerTag;
	public List<string> Alias;
	public List<string> Description;
	public List<List<string>> Tags = new List<List<string>>();
	
	// Backend info
	public List<string> AccessToken;
	public List<string> RefreshToken;
	public List<string> Pid;
	public List<string> Cid;
	public List<long> ExpiresIn;
}

