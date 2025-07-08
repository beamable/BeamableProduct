using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserCreateBatchCommand : AtomicCommand<DeveloperUserCreateBatchArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserCreateBatchCommand() : base("create-user-batch", "")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableIntOption("rolling-buffer-size", "The max amount of temporary users that you can have before starting to delete the oldest"), (args, s) => { args.RollingBufferSize = s; });
		AddOption(new ConfigurableOptionList("templates-list", ""), (args, enumerable) =>
		{
			foreach (var templateIdentifier in enumerable)
			{
				args.TemplatesIdentifiers.Add(templateIdentifier);
			}
		});
		
		AddOption(new ConfigurableOptionList("amount-list", ""), (args, enumerable) =>
		{
			foreach (var amountStr in enumerable)
			{
				args.PlayersAmount.Add(int.Parse(amountStr));
			}
		});
	}


	public override async Task<DeveloperUserResult> GetResult(DeveloperUserCreateBatchArgs args)
	{
		if (args.TemplatesIdentifiers.Count != args.PlayersAmount.Count)
		{
			throw new CliException("Invalid number of templates specified");
		}
		Dictionary<string, int> amountPerTemplate = new Dictionary<string, int>();

		for (int i = 0; i < args.TemplatesIdentifiers.Count; i++)
		{
			string templateIdentifier = args.TemplatesIdentifiers[i];
			int amount = args.PlayersAmount[i];
			
			amountPerTemplate.Add(templateIdentifier, amount);
		}
		List<DeveloperUser> result = await args.DeveloperUserManagerService.CreateUserFromTemplate(args.TemplatesIdentifiers, amountPerTemplate, args.RollingBufferSize);

		return new DeveloperUserResult()
		{
			CreatedUsers = result.Select(item => new DeveloperUserData()
			{
				Alias = item.Alias,
				CreateByGamerTag = item.CreatedByGamerTag,
				Description = item.Description,
				CreateCopyOnStart = item.CreateCopyOnStart,
				DeveloperUserType = (int)DeveloperUserType.Captured,
				GamerTag = item.GamerTag,
				TemplatedGamerTag = item.TemplateGamerTag,
				RefreshToken = item.RefreshToken,
				AccessToken = item.AccessToken,
				Cid = item.Cid,
				Pid = item.Pid,
				Tags = new List<string>(item.Tags),
				ExpiresIn = item.ExpiresIn
			}).ToList(),
		};
	}
}

public class DeveloperUserCreateBatchArgs : ContentCommandArgs
{
	public int RollingBufferSize;
	
	public readonly List<string> TemplatesIdentifiers = new List<string>();
	public readonly List<int> PlayersAmount = new List<int>();
}
