using Beamable.Server;
using cli.Services.HttpServer;
using cli.Utils;
using Serilog;
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
}

public class ServeCliCommandOutput
{
	public int port;
	public string uri;
}

public class ServeCliCommand
	: StreamCommand<ServeCliCommandArgs, ServeCliCommandOutput>
		, ISkipManifest
{
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

		var portOption = new Option<int>("--port", () => ServerService.DEFAULT_PORT, "The port the local server will bind to");
		portOption.AddAlias("-p");
		AddOption(portOption, (args, port) =>
		{
			if (port is < ServerService.DEFAULT_PORT or > ServerService.MAX_PORT)
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
	}

	public override async Task Handle(ServeCliCommandArgs args)
	{
		RequireProcessIdOption.ConfigureRequiredProcessIdWatcher(args.requireProcessId);
		
		// Pre-Warm the content cache.
		await args.ContentService.GetManifest();
		
		var server = args.Provider.GetService<ServerService>();
		await server.RunServer(args, data =>
		{
			this.SendResults(new ServeCliCommandOutput { uri = data.uri, port = data.port });
		});
	}
}
