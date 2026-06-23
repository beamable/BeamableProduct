namespace cli.Mcp;

public class McpGroupCommand : CommandGroup, IStandaloneCommand
{
	public McpGroupCommand() : base("mcp", "Model Context Protocol integration for the Beamable CLI")
	{
	}
}
