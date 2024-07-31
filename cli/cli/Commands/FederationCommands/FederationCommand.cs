using JetBrains.Annotations;

namespace cli.FederationCommands;

public class FederationCommand : CommandGroup
{
	public FederationCommand() : base("federation", "commands to work with microservice federation")
	{
		AddAlias("fed");
	}
}
