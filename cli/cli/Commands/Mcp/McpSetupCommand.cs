using Newtonsoft.Json;
using System.CommandLine;

namespace cli.Mcp;

public class McpSetupCommandArgs : CommandArgs
{
	public string projectPath;
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
	}

	public override Task<McpSetupCommandResult> GetResult(McpSetupCommandArgs args)
	{
		var targetDir = ResolveTargetDirectory(args);

		ConfigService.EnsureDotNetToolsManifest(targetDir);

		var config = new
		{
			mcpServers = new Dictionary<string, object>
			{
				["beamable"] = new
				{
					command = "dotnet",
					args = new[] { "beam", "mcp", "serve" }
				}
			}
		};

		var configPath = Path.Combine(targetDir, ".mcp.json");
		var json = JsonConvert.SerializeObject(config, Formatting.Indented);
		File.WriteAllText(configPath, json);

		return Task.FromResult(new McpSetupCommandResult { configPath = configPath });
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
