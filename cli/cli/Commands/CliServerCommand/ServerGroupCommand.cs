using JetBrains.Annotations;

namespace cli.CliServerCommand;

public class ServerGroupCommand : CommandGroup
{
	public override bool IsForInternalUse => true;

	public ServerGroupCommand() : base("server", "The CLI can be run as a server")
	{
	}
}
