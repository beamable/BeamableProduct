using Beamable.Common.BeamCli;
using Beamable.Server;
using cli.Services;
using cli.Services.HttpServer;
using System.CommandLine;
using System.Diagnostics;

namespace cli.CliServerCommand;

public class ServeCliCommandArgs : CommandArgs
{
	public int port;
	public string owner;
	public int selfDestructTimeSeconds;
	public bool incPortUntilSuccess;
	public int requireProcessId;
	public bool useCustomSplitter;
	public bool skipContentPreWarm;

	/// <summary>The session secret requests to /execute must carry. Generated when the server starts.</summary>
	[NonSerialized] // kept out of the verbose argument dump
	public string token;

	/// <summary>When set, every /execute exchange is saved here as a golden protocol fixture.</summary>
	public string recordTo;

	/// <summary>When true, AI agents can use the CLI through MCP at <see cref="McpHttpEndpoint.PATH"/>.</summary>
	public bool enableMcp;

	/// <summary>The running MCP endpoint, when <see cref="enableMcp"/> is set.</summary>
	[NonSerialized]
	public McpHttpEndpoint mcpEndpoint;
}

public class ServeCliCommandOutput
{
	public int port;
	public string uri;

	/// <summary>Send as <c>Authorization: Bearer</c> on every /execute request.</summary>
	public string token;
}

public class ServeCliCommand
	: StreamCommand<ServeCliCommandArgs, ServeCliCommandOutput>
		, ISkipManifest
		, IStreamsIndefinitely
{
	/// <summary>Same as --record-to, for servers started by a tool (like Unity) whose arguments you can't change.</summary>
	public const string ENV_VAR_RECORD_TO = "BEAM_SERVER_RECORD_TO";

	public override bool IsForInternalUse => true;

	public ServeCliCommand() : base("serve", "Create a local server for the cli")
	{
	}

	public override void Configure()
	{
		AddArgument(
			new Argument<string>("owner", () => "cli",
				"The owner of the server is used to identify the server later with the /info endpoint"),
			(args, owner) => args.owner = owner);

		var portOption = new Option<int>("--port", () => 0,
			"The port the local server will bind to. 0 picks this workspace's preferred port, which stays the same between runs");
		portOption.AddAlias("-p");
		AddOption(portOption, (args, port) =>
		{
			if (port != 0 && port is < ServerService.DEFAULT_PORT or > ServerService.MAX_PORT)
				throw new CliException($"Port must be between {ServerService.DEFAULT_PORT} and {ServerService.MAX_PORT}");

			args.port = port;
		});

		var incPortOption = new Option<bool>("--auto-inc-port", () => true,
			"When true, if the given --port is not available, it will be incremented until an available port is discovered");
		incPortOption.AddAlias("-i");
		AddOption(incPortOption, (args, inc) => args.incPortUntilSuccess = inc);

		var timerOption = new Option<int>("--self-destruct-seconds", () => 0,
			"The number of seconds the server will stay alive without receiving any traffic. A value of zero means there is no self destruct timer");
		timerOption.AddAlias("-d");
		AddOption(timerOption, (args, time) => args.selfDestructTimeSeconds = time);

		AddOption(new RequireProcessIdOption(), (args, i) => args.requireProcessId = i);

		var splitterOption = new Option<bool>("--custom-splitter", () => false,
			"When true, will use custom logic to split the command line given to the server via HTTP request.\n" +
			"The default splitter (from Microsoft) does NOT allow you to pass in JSON blobs as arguments.\n" +
			"The custom splitter does its best to support all our commands correctly and accept json blobs as arguments");
		splitterOption.AddAlias("-cs");
		AddOption(splitterOption, (args, b) => args.useCustomSplitter = b);
		
		var skipContentPrewarm = new Option<bool>("--skip-content-prewarm", () => false,
			"When true, will NOT pre-warm the content service with the latest content manifest");
		skipContentPrewarm.AddAlias("-scpw");
		AddOption(skipContentPrewarm, (args, b) => args.skipContentPreWarm = b);

		AddOption(new Option<bool>("--enable-mcp", () => false,
				$"When true, AI agents can use this server as an MCP server (Streamable HTTP) at {McpHttpEndpoint.PATH}, with the same session token"),
			(args, b) => args.enableMcp = b);

		AddOption(new Option<string>("--record-to",
				"A folder where every /execute request and the events it streamed are saved as JSON, for protocol regression fixtures"),
			(args, dir) => args.recordTo = dir);
	}

	public override async Task Handle(ServeCliCommandArgs args)
	{
		RequireProcessIdOption.ConfigureRequiredProcessIdWatcher(args.requireProcessId);
		if (args.port == 0)
		{
			args.port = CliServerPorts.PreferredPort(args.ConfigService.BeamableWorkspace);
		}
		if (string.IsNullOrEmpty(args.recordTo))
		{
			args.recordTo = Environment.GetEnvironmentVariable(ENV_VAR_RECORD_TO);
		}
		
		// Pre-Warm the content cache.
		if (!string.IsNullOrEmpty(args.AppContext.Cid) && !args.skipContentPreWarm)
		{
			await args.ContentService.GetManifest();
		}
		
		var version = VersionService.GetNugetPackagesForExecutingCliVersion().ToString();
		if (args.enableMcp)
		{
			args.mcpEndpoint = await McpHttpEndpoint.Start(version,
				$"Beamable CLI for the workspace at {args.ConfigService.BeamableWorkspace} (cid=[{args.AppContext.Cid}] pid=[{args.AppContext.Pid}]). " +
				"Commands run with this workspace's credentials and act on that realm.");
		}

		var server = args.Provider.GetService<ServerService>();
		await server.RunServer(args, data =>
		{
			var locatorPath = ServerLocatorFile.Write(args.ConfigService.ConfigTempDirectoryPath, new ServerLocator
			{
				pid = Environment.ProcessId,
				port = data.port,
				uri = data.uri,
				owner = args.owner,
				version = version,
				mcpUrl = args.mcpEndpoint == null ? null : data.uri.TrimEnd('/') + McpHttpEndpoint.PATH,
				token = args.token,
				startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
			});
			Log.Debug($"Wrote server locator to {locatorPath}");
			this.SendResults(new ServeCliCommandOutput { uri = data.uri, port = data.port, token = args.token });
		});
	}
}
