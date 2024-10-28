namespace cli.FederationCommands;

public class FederationCommand : CommandGroup
{
	public FederationCommand() : base("federation", "Commands to work with microservice federation")
	{
		AddAlias("fed");
	}
}
