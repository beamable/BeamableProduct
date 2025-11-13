namespace cli.Portal;

public class PortalExtensionCreateCommandArgs : CommandArgs
{

}

public class PortalExtensionCreateCommand : AppCommand<PortalExtensionCreateCommandArgs>
{
	public PortalExtensionCreateCommand() : base("create", "Create a new Portal Extension App")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(PortalExtensionCreateCommandArgs args)
	{
		throw new NotImplementedException();
	}
}
