namespace cli.Commands.Project;

public class NewPortalExtensionCommandArgs : SolutionCommandArgs
{
}

public class NewPortalExtensionCommand : AppCommand<NewPortalExtensionCommandArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;

	public NewPortalExtensionCommand(InitCommand initCommand) : base("portal-extension", "Creates a new Portal Extension App")
	{
		_initCommand = initCommand;
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		SolutionCommandArgs.Configure(this);
	}

	public override async Task Handle(NewPortalExtensionCommandArgs args)
	{
		await args.CreateConfigIfNeeded(_initCommand);
		var newPortalExtensionInfo = await args.ProjectService.CreateNewPortalExtension(args);

		//TODO make sure all config files exist and also find a way for the manifest scan to find the apps
	}
}
