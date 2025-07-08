using cli.Content;
using cli.Services.DeveloperUserManager;

namespace cli.DeveloperUserCommands;

public class DeveloperUserRemoveCommand : AtomicCommand<DeveloperUserRemoveArgs, DeveloperUserResult>, ISkipManifest
{
	public DeveloperUserRemoveCommand() : base("remove-user", "")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("identifier", ""), (args, s) => { args.Identifier = s; });
	}

	public override Task Handle(DeveloperUserRemoveArgs args)
	{
		args.DeveloperUserManagerService.RemoveUser(args.Identifier);
		
		return Task.CompletedTask;
	}

	public override Task<DeveloperUserResult> GetResult(DeveloperUserRemoveArgs args)
	{
		return Task.FromResult(new DeveloperUserResult());
	}
}

public class DeveloperUserRemoveArgs : ContentCommandArgs
{
	public string Identifier;
}
