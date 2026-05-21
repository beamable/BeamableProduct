namespace cli.Mcp;

public class McpGroupCommand : CommandGroup, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public McpGroupCommand() : base("mcp", "Model Context Protocol integration for the Beamable CLI")
	{
	}
}
