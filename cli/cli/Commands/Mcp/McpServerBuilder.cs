using cli.Commands.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace cli.Mcp;

public class McpServerBuilder
{
	// Ensures EnsureWorkspaceFromRootsAsync runs only once across concurrent tool calls.
	// The MCP server invokes multiple tools in parallel, each of which calls this method;
	// the flag prevents redundant roots requests after the first successful resolution.
	private static int _workspaceResolved;

	/// <summary>
	/// Embedded markdown sent to MCP clients as the server's <c>instructions</c> during initialize.
	/// Edit <c>cli/cli/Docs/Mcp/ServerInstructions.md</c> to change it.
	/// </summary>
	public const string InstructionsResourceName = "cli.Docs.Mcp.ServerInstructions.md";

	/// <summary>Reads the server instructions from the embedded markdown resource.</summary>
	public static string GetServerInstructions()
	{
		var assembly = typeof(McpServerBuilder).Assembly;
		using var stream = assembly.GetManifestResourceStream(InstructionsResourceName);
		if (stream == null) return string.Empty;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd().Trim();
	}

	/// <summary>Applies the Beamable server options (instructions) to an MCP server.</summary>
	public static void ConfigureServerOptions(McpServerOptions options)
	{
		options.ServerInstructions = GetServerInstructions();
	}

	public static async Task EnsureWorkspaceFromRootsAsync(McpServer server)
	{
		if (Interlocked.CompareExchange(ref _workspaceResolved, 1, 0) != 0)
			return;

		try
		{
			var result = await server.RequestRootsAsync(new ListRootsRequestParams());
			var root = result?.Roots?.FirstOrDefault();
			if (root?.Uri != null
			    && Uri.TryCreate(root.Uri, UriKind.Absolute, out var uri)
			    && uri.IsFile
			    && Directory.Exists(uri.LocalPath))
			{
				Directory.SetCurrentDirectory(uri.LocalPath);
				return;
			}
		}
		catch
		{
			// Client doesn't support roots
		}
	}

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
				.AddMcpServer(ConfigureServerOptions)
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

	[McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false,
		Title = "Discover beam CLI commands")]
	[Description(
		"Discover available beam commands." +
		" Use this when you don't yet know which command does what you need." +
		" Pass an empty string to list all root commands; pass a command name (e.g. 'project', 'services', 'content') to list its subcommands." +
		" Once you know the command, call beam_get_help for its options, or beam_exec directly if you already know them.")]
	public async Task<string> beam_list_commands(
		McpServer server,
		[Description("Command prefix to filter by, e.g. 'project', 'content', 'services'. Leave empty to list all root commands.")] string prefix = "")
	{
		await McpServerBuilder.EnsureWorkspaceFromRootsAsync(server);
		return await _executor.ExecuteHelpAsync(prefix);
	}

	[McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false,
		Title = "Get beam command documentation")]
	[Description(
		"Read the full docs for a command: all arguments, options, defaults, and the output schema." +
		" Call this when you are unsure of a command's arguments — don't guess option names." +
		" (beam_exec also returns this help when it is called with invalid arguments.)" +
		" If working in a new project directory, start with beam_get_help('init'): the 'init' command creates the .beamable workspace folder that all other commands require.")]
	public async Task<string> beam_get_help(
		McpServer server,
		[Description("The beam command path to get help for, e.g. 'project new microservice' or 'content publish'")] string command)
	{
		await McpServerBuilder.EnsureWorkspaceFromRootsAsync(server);
		return await _executor.ExecuteHelpAsync(command);
	}

	[McpServerTool(ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = true,
		Title = "Execute a beam CLI command")]
	[Description(
		"Execute a beam command." +
		" Pass everything after 'beam', e.g. 'init --cid MyCid --pid MyPid' or 'project new service Foo'." +
		" If you already know the command and its arguments, call this directly." +
		" If the arguments are invalid, the response starts with 'Invalid arguments' followed by the command's help; fix the arguments and call again." +
		" Commands run non-interactively ('-q' is added automatically), so pass every value a command would otherwise prompt for." +
		" If the .beamable workspace folder does not exist yet, run 'init' first." +
		" For multi-step workflows, call beam_get_skill first to load a step-by-step guide.")]
	public async Task<string> beam_exec(
		McpServer server,
		[Description("The beam command string, everything after 'beam'")] string command)
	{
		await McpServerBuilder.EnsureWorkspaceFromRootsAsync(server);
		return await _executor.ExecuteAsync(command);
	}

	[McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false,
		Title = "Load a Beamable workflow skill guide")]
	[Description(
		"Load a step-by-step skill guide for a complex Beamable workflow." +
		" Call with an empty string to list all available skills with summaries." +
		" Call with a skill name to load the full guide." +
		" Always load the relevant skill BEFORE attempting multi-step workflows like creating extensions, microservices, deploying, or managing content.")]
	public Task<string> beam_get_skill(
		[Description("Skill name to load, e.g. 'beam-create-portal-extension'. Leave empty to list all available skills.")] string skill = "")
		=> McpToolExecutor.GetSkillAsync(skill);

	[McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false,
		Title = "Get Beamable SDK source code")]
	[Description(
		"Get Beamable SDK source code." +
		" Returns local file paths to SDK source, and when filePath is provided, reads and returns the file content directly." +
		" Use this in sandboxed environments where the agent cannot access NuGet cache or package directories." +
		" For large files, use offset and limit to paginate — check hasMore and nextOffset in the response." +
		" Auto-detects the SDK platform and version from the current project directory.")]
	public async Task<string> beam_get_source(
		McpServer server,
		[Description("Platform: 'unity', 'cli', 'unreal', or 'web'. Auto-detected if omitted.")] string platform = "",
		[Description("SDK version override, e.g. '5.0.1'. Auto-detected if omitted.")] string version = "",
		[Description(
			"File to read from SDK source. Can be:" +
			" (1) a filename like 'ContentObject.cs' (searches all known SDK directories)," +
			" (2) a relative path like 'Runtime/Content/ContentObject.cs', or" +
			" (3) an absolute path under an allowed SDK directory." +
			" When provided and found, file content is returned in the response." +
			" Omit to get directory paths only.")] string filePath = "",
		[Description("Character offset to start reading from. Use nextOffset from a previous response to continue reading a large file. Defaults to 0 (start of file)")] int offset = 0,
		[Description("Maximum characters to return per chunk. Capped at 65536. Defaults to 65536 if omitted or 0. Use with offset to paginate large files")] int limit = 0)
	{
		await McpServerBuilder.EnsureWorkspaceFromRootsAsync(server);
		return await _executor.GetSourceCode(platform, version, filePath, offset, limit);
	}
}
