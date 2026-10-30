using Beamable.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;

namespace cli.Mcp;

public class McpSetupCommandArgs : CommandArgs
{
	public string projectPath;
	public string transport;
}

[Serializable]
public class McpSetupCommandResult
{
	public string configPath;
}

public class McpSetupCommand
	: AtomicCommand<McpSetupCommandArgs, McpSetupCommandResult>
	, IStandaloneCommand
	, ISkipManifest
{
	public McpSetupCommand() : base("setup", "Write a .mcp.json config file so AI clients can invoke beam commands as MCP tools")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--project-path", "Directory to write the .mcp.json file into; defaults to the Beamable workspace root"),
			(args, v) => args.projectPath = v);
		AddOption(new Option<string>("--transport", () => TRANSPORT_STDIO,
				$"How AI clients reach the CLI: '{TRANSPORT_STDIO}' starts a separate `beam mcp serve` per client, '{TRANSPORT_HTTP}' connects to this workspace's running `beam server serve --enable-mcp` (the one Unity runs)")
				.FromAmong(TRANSPORT_STDIO, TRANSPORT_HTTP),
			(args, v) => args.transport = v);
	}

	public const string TRANSPORT_STDIO = "stdio";
	public const string TRANSPORT_HTTP = "http";

	public override Task<McpSetupCommandResult> GetResult(McpSetupCommandArgs args)
	{
		var targetDir = ResolveTargetDirectory(args);

		ConfigService.EnsureDotNetToolsManifest(targetDir);

		var configPath = Path.Combine(targetDir, ".mcp.json");

		var root = File.Exists(configPath)
			? JObject.Parse(File.ReadAllText(configPath))
			: new JObject();

		string mcpUrl = null;
		if (args.transport == TRANSPORT_HTTP)
		{
			mcpUrl = McpLocalServer.Find(args.ConfigService)?.mcpUrl
			         ?? throw new CliException(McpLocalServer.NOT_RUNNING + " Then run this again.");
		}
		SetBeamableServer(root, mcpUrl);

		File.WriteAllText(configPath, root.ToString(Formatting.Indented));

		return Task.FromResult(new McpSetupCommandResult { configPath = configPath });
	}

	/// <summary>
	/// Points the config's "beamable" server at <paramref name="mcpUrl"/> (Streamable HTTP), or at a `beam mcp serve`
	/// per client when it is null. Other servers in the config are left alone.
	/// </summary>
	public static void SetBeamableServer(JObject root, string mcpUrl)
	{
		var servers = root["mcpServers"] as JObject ?? new JObject();
		servers["beamable"] = mcpUrl != null
			? JObject.FromObject(new
			{
				type = "http",
				url = mcpUrl,
				// Runs on every connection, so it always hands over the current server's token.
				headersHelper = "dotnet beam mcp token"
			})
			: JObject.FromObject(new
			{
				command = "dotnet",
				args = new[] { "beam", "mcp", "serve" }
			});
		root["mcpServers"] = servers;
	}

	private static string ResolveTargetDirectory(McpSetupCommandArgs args)
	{
		if (!string.IsNullOrWhiteSpace(args.projectPath))
			return Path.GetFullPath(args.projectPath);

		var workspace = args.ConfigService?.BeamableWorkspace;
		return string.IsNullOrWhiteSpace(workspace)
			? Directory.GetCurrentDirectory()
			: workspace;
	}

}
