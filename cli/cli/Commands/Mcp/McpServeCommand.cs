namespace cli.Mcp;

public class McpServeCommandArgs : CommandArgs { }

public class McpServeCommand
	: AppCommand<McpServeCommandArgs>
	, IStandaloneCommand
	, ISkipManifest
{
	public McpServeCommand() : base("serve", "Start an MCP stdio server exposing beam commands as tools")
	{
	}

	public override void Configure() { }

	public override async Task Handle(McpServeCommandArgs args)
	{
		var builder = new McpServerBuilder();
		await builder.RunAsync();
	}
}
