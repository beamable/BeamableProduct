using CliWrap;

namespace cli;

public class OrganizationCommandArgs : CommandArgs
{

}
public class OrganizationCommand : AppCommand<OrganizationCommandArgs>
{
	public OrganizationCommand() : base("org", "Commands related to beamable organizations")
	{
	}

	public override void Configure()
	{

	}

	public override Task Handle(OrganizationCommandArgs args)
	{
		return Task.CompletedTask;
	}
}
