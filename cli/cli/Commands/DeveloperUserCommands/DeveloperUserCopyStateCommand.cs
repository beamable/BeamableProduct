namespace cli.DeveloperUserCommands;
public class DeveloperUserCopyStateCommand : AppCommand<DeveloperUserCopyStateArgs>, ISkipManifest

{
	public DeveloperUserCopyStateCommand() : base("copy-state", "Copy the inventory and stats state from a user to another")
	{
	}

	public override void Configure()
	{
		AddOption(new ConfigurableOption("source", "The identifier to the source user to copy from."), ((args, s) => { args.SourceIdentifier = s; }));
		AddOption(new ConfigurableOption("target", "The identifier to the target user to copy to."), ((args, s) => { args.TargetIdentifier = s; }));
	}

	public override async Task Handle(DeveloperUserCopyStateArgs args)
	{
		var sourceDeveloperUser =  args.DeveloperUserManagerService.LoadCachedDeveloperUser(args.SourceIdentifier);
		var targetDeveloperUser =  args.DeveloperUserManagerService.LoadCachedDeveloperUser(args.TargetIdentifier);

		await args.DeveloperUserManagerService.CopyState(sourceDeveloperUser, targetDeveloperUser);
	}
}

public class DeveloperUserCopyStateArgs : CommandArgs
{
	public string SourceIdentifier;
	public string TargetIdentifier;
	
}

