using JetBrains.Annotations;

namespace cli.DeveloperUserCommands;

public class DeveloperUserManagerCommand : CommandGroup
{
	public DeveloperUserManagerCommand() : base("developer-user-manager", "The command that manages the developer users")
	{
	}
}
