using Beamable.Common;
using Spectre.Console;
using System.CommandLine;

namespace cli.Mcp;

public class McpSetupCommandArgs : CommandArgs
{
	public string projectPath;
	public bool agentsFile;
}

[Serializable]
public class McpSetupCommandResult
{
	public string configPath;
	public string agentsFilePath;
	public string agentsFileOutcome;
}

public class McpSetupCommand
	: AtomicCommand<McpSetupCommandArgs, McpSetupCommandResult>
	, IStandaloneCommand
	, ISkipManifest
{
	public McpSetupCommand() : base("setup", "Configure the Beamable MCP server for AI clients and optionally generate an AGENTS.md guide")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--project-path", "Directory to write the .mcp.json file into; defaults to the Beamable workspace root"),
			(args, v) => args.projectPath = v);
		AddOption(new Option<bool>("--agents-file", () => false, "Also generate an AGENTS.md AI-agent guide (skips the interactive prompt)"),
			(args, v) => args.agentsFile = v);
	}

	public override Task<McpSetupCommandResult> GetResult(McpSetupCommandArgs args)
	{
		var targetDir = ResolveTargetDirectory(args);
		var (configPath, _) = AiFileWriter.EnsureBeamableServer(targetDir);

		var result = new McpSetupCommandResult { configPath = configPath };

		if (ShouldWriteAgentsFile(args))
		{
			var (agentsPath, outcome) = AiFileWriter.EnsureAgentsFile(targetDir);
			result.agentsFilePath = agentsPath;
			result.agentsFileOutcome = outcome.ToString();
			switch (outcome)
			{
				case AiFileWriter.AgentsFileOutcome.Created:
					BeamableLogger.Log($"Created {agentsPath}");
					break;
				case AiFileWriter.AgentsFileOutcome.Appended:
					BeamableLogger.Log($"Appended the Beamable AI guide to {agentsPath}");
					break;
				case AiFileWriter.AgentsFileOutcome.AlreadyPresent:
					BeamableLogger.Log($"Beamable AI guide already present in {agentsPath}");
					break;
				case AiFileWriter.AgentsFileOutcome.NoContent:
					BeamableLogger.LogWarning("Could not locate the embedded AGENTS.md guide; skipped.");
					break;
			}
		}

		return Task.FromResult(result);
	}

	// Explicit flag wins; otherwise prompt only when interactive (never hang in quiet/piped/agent contexts).
	private static bool ShouldWriteAgentsFile(McpSetupCommandArgs args)
	{
		if (args.agentsFile)
			return true;

		if (args.Quiet || (args.AppContext?.UsePipeOutput ?? false))
			return false;

		return AnsiConsole.Confirm("Also generate an AGENTS.md AI-agent guide?", defaultValue: true);
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
