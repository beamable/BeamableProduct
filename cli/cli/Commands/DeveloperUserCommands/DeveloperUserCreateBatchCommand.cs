using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserCreateBatchCommand : AtomicCommand<DeveloperUserCreateBatchArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserCreateBatchCommand() : base("create-user-batch", "Create multiple users from multiples templates doing the request in parallel")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableIntOption("rolling-buffer-size", "The max amount of temporary users that you can have before starting to delete the oldest"), (args, s) => { args.RollingBufferSize = s; });
		AddOption(new ConfigurableOptionList("templates-list", "The gamer tag list for all templates that will be copied into a new player"), (args, enumerable) =>
		{
			foreach (var templateIdentifier in enumerable)
			{
				args.TemplatesGamerTag.Add(templateIdentifier);
			}
		});
		
		AddOption(new ConfigurableOptionList("amount-list", "A parallel list of the template-list arg that contains the amount of users for each template"), (args, enumerable) =>
		{
			foreach (var amountStr in enumerable)
			{
				args.PlayersAmount.Add(int.Parse(amountStr));
			}
		});
	}


	public override async Task<DeveloperUserResult> GetResult(DeveloperUserCreateBatchArgs args)
	{
		if (args.TemplatesGamerTag.Count != args.PlayersAmount.Count)
		{
			throw new CliException("Invalid number of templates specified");
		}
		Dictionary<string, int> amountPerTemplate = new Dictionary<string, int>();

		for (int i = 0; i < args.TemplatesGamerTag.Count; i++)
		{
			string templateIdentifier = args.TemplatesGamerTag[i];
			int amount = args.PlayersAmount[i];
			
			amountPerTemplate.Add(templateIdentifier, amount);
		}
		List<DeveloperUser> result = await args.DeveloperUserManagerService.CreateUsersFromTemplateInBatch(args.TemplatesGamerTag, amountPerTemplate, args.RollingBufferSize);

		return new DeveloperUserResult()
		{
			CreatedUsers = DeveloperUserManagerService.DeveloperUsersToDeveloperUsersData(result).ToList()
		};
	}
}

public class DeveloperUserCreateBatchArgs : ContentCommandArgs
{
	public int RollingBufferSize;
	
	public readonly List<string> TemplatesGamerTag = new List<string>();
	public readonly List<int> PlayersAmount = new List<int>();
}
