using JetBrains.Annotations;

namespace cli.DeveloperUserCommands;

public class DeveloperUserManagerCommand : CommandGroup
{
	public DeveloperUserManagerCommand() : base("developer-user-manager", "Manage local developer test users — create, batch-create, copy state, and watch user files")
	{
	}
}
