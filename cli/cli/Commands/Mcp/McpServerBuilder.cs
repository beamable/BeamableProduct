using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace cli.Mcp;

public class McpServerBuilder
{
	public async Task RunAsync(string cid, string pid, CancellationToken cancellationToken = default)
	{
		var executor = new McpToolExecutor(cid, pid);
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
		"If the .beamable workspace folder does not exist yet, run 'init' first (see beam_get_help('init') for required arguments).")]
	public Task<string> beam_exec(
		[Description("The beam command string, everything after 'beam'")] string command)
		=> _executor.ExecuteAsync(command);
 
	[McpServerTool]
	[Description(
		"Return a schema of Beamable types — C# content objects, federation interfaces, SDK utility types, and Web SDK documentation. " +
		"Call with no arguments first to get an overview with section counts and the full list of utility namespaces. " +
		"Then call again with section='content', 'federation', or 'utility' to load C# types, or section='web' to get the Beamable Web SDK (TypeScript) documentation URL for portal extension development. " +
		"For 'utility', always pass a filter string (namespace prefix or type name keyword) to avoid loading the full set. " +
		"Use this when helping customers subclass ContentObject, implement federation, define microservice types, reuse any Beamable.Common utility, or build portal extensions with the Web SDK.")]
	public Task<string> beam_list_types(
		[Description("Which section to load: 'content', 'federation', 'utility', or 'web'. Leave empty for an overview with counts and the list of utility namespaces.")] string section = "",
		[Description("Narrows utility results to types whose namespace or name contains this string (case-insensitive). E.g. 'Inventory', 'Beamable.Common.Content'.")] string filter = "")
		=> _executor.GetTypeSchemaAsync(section, filter);
}
