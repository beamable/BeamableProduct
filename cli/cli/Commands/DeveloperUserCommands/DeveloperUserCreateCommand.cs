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
		AddOption(new ConfigurableOption("alias", ""), (args, s) => { args.Alias = s; });
		AddOption(new ConfigurableOption("template", ""), (args, s) => { args.TemplateIdentifier = s; });
		AddOption(new ConfigurableOption("description", ""), (args, s) => { args.Description = s; });
		AddOption(new ConfigurableIntOption("user-type", ""), ((args, number) => { args.DeveloperUserType = (DeveloperUserType)number; }));
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
			await args.DeveloperUserManagerService.CreateUserFromTemplate(args.TemplateIdentifier, args.Alias, args.Description, args.Tags, args.DeveloperUserType);
		}
		else
		{
			await args.DeveloperUserManagerService.CreateUser(args.Alias, args.Description, args.Tags, args.DeveloperUserType);
		}
	}

	public override Task<DeveloperUserResult> GetResult(DeveloperUserCreateArgs args)
	{
		return Task.FromResult(new DeveloperUserResult());
	}
}

public class DeveloperUserCreateArgs : ContentCommandArgs
{
	public string TemplateIdentifier;
	public string Alias;
	public string Description;
	public DeveloperUserType DeveloperUserType;
	public readonly List<string> Tags = new List<string>();
}
