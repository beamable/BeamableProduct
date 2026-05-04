using cli.Commands.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace cli.Mcp;

public class McpServerBuilder
{
	public async Task RunAsync(CancellationToken cancellationToken = default)
	{
		App.IsRunningInMcpServer = true;
		var executor = new McpToolExecutor();
		var tools = new BeamMcpTools(executor);

		// The MCP stdio transport owns stdout for JSON-RPC framing.
		// Redirect Console.Out to Null so no stray writes corrupt the protocol.
		var originalOut = Console.Out;
		Console.SetOut(TextWriter.Null);

		try
		{
			var hostBuilder = Host.CreateApplicationBuilder();
			hostBuilder.Services
				.AddMcpServer()
				.WithStdioServerTransport()
				.WithTools<BeamMcpTools>(tools, null);

			var host = hostBuilder.Build();
			await host.RunAsync(cancellationToken);
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}
}

[McpServerToolType]
public class BeamMcpTools
{
	private readonly McpToolExecutor _executor;

	public BeamMcpTools(McpToolExecutor executor)
	{
		_executor = executor;
	}

	[McpServerTool]
	[Description(
		"Step 1 of 3 — Discover available commands. " +
		"Call this FIRST, before beam_get_help or beam_exec, whenever you are about to perform any Beamable task. " +
		"Pass an empty string to list all root commands; pass a command name (e.g. 'project', 'services', 'content') to list its subcommands. " +
		"Navigate the command tree until you find the exact command you need, then proceed to beam_get_help.")]
	public Task<string> beam_list_commands(
		[Description("Command prefix to filter by, e.g. 'project', 'content', 'services'. Leave empty to list all root commands.")] string prefix = "")
		=> _executor.ExecuteHelpAsync(prefix);

	[McpServerTool]
	[Description(
		"Step 2 of 3 — Read the full docs for a command before running it. " +
		"Call this after beam_list_commands identifies the exact command path you need. " +
		"Returns all arguments, options, defaults, and the output schema. " +
		"You MUST call this for every command before calling beam_exec — never assume argument names or syntax. " +
		"If working in a new project directory, start with beam_get_help('init'): the 'init' command creates the .beamable workspace folder that all other commands require.")]
	public Task<string> beam_get_help(
		[Description("The beam command path to get help for, e.g. 'project new microservice' or 'content publish'")] string command)
		=> _executor.ExecuteHelpAsync(command);

	[McpServerTool]
	[Description(
		"Step 3 of 3 — Execute a beam command. " +
		"Only call this AFTER completing Step 1 (beam_list_commands) and Step 2 (beam_get_help) for the command you intend to run. " +
		"Never guess arguments or assume command syntax. " +
		"Pass everything after 'beam', e.g. 'init --cid MyCid --pid MyPid' or 'project new microservice --name Foo'. " +
		"If the .beamable workspace folder does not exist yet, run 'init' first (see beam_get_help('init') for required arguments). " +
		"For multi-step workflows, call beam_get_skill first to load a step-by-step guide.")]
	public Task<string> beam_exec(
		[Description("The beam command string, everything after 'beam'")] string command)
		=> _executor.ExecuteAsync(command);

	[McpServerTool]
	[Description(
		"Load a step-by-step skill guide for a complex Beamable workflow. " +
		"Call with an empty string to list all available skills with summaries. " +
		"Call with a skill name to load the full guide. " +
		"Always load the relevant skill BEFORE attempting multi-step workflows like creating extensions, microservices, deploying, or managing content.")]
	public Task<string> beam_get_skill(
		[Description("Skill name to load, e.g. 'beam-create-portal-extension'. Leave empty to list all available skills.")] string skill = "")
		=> McpToolExecutor.GetSkillAsync(skill);

	[McpServerTool]
	[Description(
		"Get local file paths to the Beamable SDK source code. " +
		"Auto-detects the SDK platform and version from the current project directory. " +
		"Returns local filesystem paths — read the source files directly instead of browsing types through the API. " +
		"For CLI/Microservice projects, source is in the NuGet cache at ~/.nuget/packages/beamable.common/{version}/content/.")]
	public Task<string> beam_get_source(
		[Description("Platform: 'unity', 'cli', 'unreal', or 'web'. Auto-detected if omitted.")] string platform = "",
		[Description("SDK version override, e.g. '5.0.1'. Auto-detected if omitted.")] string version = "",
		[Description("File path within the source tree, e.g. 'Runtime/Content/ContentObject.cs'.")] string filePath = "")
		=> _executor.GetSourceUrlAsync(platform, version, filePath);
}
