using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserCreateCommand : AtomicCommand<DeveloperUserCreateArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserCreateCommand() : base("create-user", "")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableIntOption("max-amount", "The max amount of temporary users that you can have before starting to delete the oldest"), (args, s) => { args.MaxAmount = s; });
		AddOption(new ConfigurableOption("alias", ""), (args, s) => { args.Alias = s; });
		AddOption(new ConfigurableOption("template", ""), (args, s) => { args.TemplateIdentifier = s; });
		AddOption(new ConfigurableOption("description", ""), (args, s) => { args.Description = s; });
		AddOption(new ConfigurableOptionList("tags", ""), (args, s) =>
		{
			foreach (var tag in s)
			{
				args.Tags.Add(tag);
			}
		});
	}

	public override async Task Handle(DeveloperUserCreateArgs args)
	{
		if (!string.IsNullOrEmpty(args.TemplateIdentifier))
		{
			await args.DeveloperUserManagerService.CreateUserFromTemplate(args.TemplateIdentifier, args.Alias, args.Description, args.Tags.ToArray());
		}
		else
		{
			await args.DeveloperUserManagerService.CreateUser(args.Alias, args.Description, args.Tags.ToArray());
		}
	}

	public override Task<DeveloperUserResult> GetResult(DeveloperUserCreateArgs args)
	{
		return Task.FromResult(new DeveloperUserResult());
	}
}

public class DeveloperUserCreateArgs : ContentCommandArgs
{
	public int MaxAmount;
	public string TemplateIdentifier;
	public string Alias;
	public string Description;
	public readonly List<string> Tags = new List<string>();
}
