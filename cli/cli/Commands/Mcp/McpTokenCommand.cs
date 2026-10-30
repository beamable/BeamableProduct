using Beamable.Server;
using Newtonsoft.Json;
using System.CommandLine;

namespace cli.Mcp;

public class McpTokenCommandArgs : CommandArgs
{
	public bool plain;
	public string url;
}

/// <summary>
/// Prints the headers an MCP client needs for this workspace's server. It is the `headersHelper` that
/// `beam mcp setup --transport http` configures: Claude Code runs it on every connection and on a 401, so a server
/// restart (new token) needs no config change.
/// </summary>
public class McpTokenCommand
	: AppCommand<McpTokenCommandArgs>
	, IStandaloneCommand
	, ISkipManifest
{
	/// <summary>Set by Claude Code for its headers helper: the URL of the server it is connecting to.</summary>
	public const string ENV_VAR_SERVER_URL = "CLAUDE_CODE_MCP_SERVER_URL";

	public override bool IsForInternalUse => true;

	public McpTokenCommand() : base("token", "Print the Authorization header for this workspace's running MCP endpoint")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<bool>("--plain", "Print only the token, for clients where you paste it yourself"),
			(args, b) => args.plain = b);
		AddOption(new Option<string>("--url", $"The MCP URL the client connects to. Defaults to ${ENV_VAR_SERVER_URL}, then the newest server"),
			(args, s) => args.url = s);
	}

	public override Task Handle(McpTokenCommandArgs args)
	{
		var url = string.IsNullOrEmpty(args.url) ? Environment.GetEnvironmentVariable(ENV_VAR_SERVER_URL) : args.url;
		var locator = McpLocalServer.Find(args.ConfigService, url);
		if (locator == null)
		{
			throw new CliException(McpLocalServer.NOT_RUNNING);
		}

		// Write straight to stdout: a headers helper must print exactly one JSON object and nothing else.
		Console.Out.WriteLine(args.plain
			? locator.token
			: JsonConvert.SerializeObject(new Dictionary<string, string> { ["Authorization"] = "Bearer " + locator.token }));
		Console.Out.Flush();
		return Task.CompletedTask;
	}
}
