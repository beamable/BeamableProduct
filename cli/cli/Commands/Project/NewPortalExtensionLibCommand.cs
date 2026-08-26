using cli.Portal;
using cli.Services;

namespace cli.Commands.Project;

public class NewPortalExtensionLibCommandArgs : SolutionCommandArgs
{
}

public class NewPortalExtensionLibCommand : AppCommand<NewPortalExtensionLibCommandArgs>, IStandaloneCommand, IEmptyResult
{
	private readonly InitCommand _initCommand;

	public NewPortalExtensionLibCommand(InitCommand initCommand) : base("portal-extension-lib",
		"Creates a new shared TypeScript library that can be imported by Portal Extensions")
	{
		_initCommand = initCommand;
		AddAlias("pe-lib");
	}

	public override void Configure()
	{
		AddArgument(new ServiceNameArgument(), (args, i) => args.ProjectName = i);
		SolutionCommandArgs.Configure(this);
	}

	public override async Task Handle(NewPortalExtensionLibCommandArgs args)
	{
		if (!PortalExtensionCheckCommand.CheckPortalExtensionsDependencies())
			throw new CliException("Not all required dependencies exist. Aborting.");

		await args.CreateConfigIfNeeded(_initCommand);
		await args.ProjectService.CreateNewPortalExtensionLib(args);
	}
}
