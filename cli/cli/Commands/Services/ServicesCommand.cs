namespace cli;

public class ServicesCommand : CommandGroup
{
	public override bool IsForInternalUse => true;

	public ServicesCommand()
		: base("services", "Commands that allow interacting with microservices in Beamable project")
	{
	}
}
