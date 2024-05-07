using Beamable.Server;
using cli.Services.HttpServer;
using cli.Utils;
using Serilog;
using System.CommandLine;

namespace cli.CliServerCommand;

public class ServeCliCommandArgs : CommandArgs
{
	public int port;
	public string owner;
	public int selfDestructTimeSeconds;
	public bool incPortUntilSuccess;

	public LogConfigData logData;
}

public class ServeCliCommandOutput
{
	public int port;
	public string uri;
}

public class ServeCliCommand : StreamCommand<ServeCliCommandArgs, ServeCliCommandOutput>
{
	private LogConfigData _logData;
	public override bool IsForInternalUse => true;

	public ServeCliCommand(LogConfigData logData) : base("serve", "Create a local server for the cli")
	{
		_logData = logData;
	}

	public override void Configure()
	{
		AddArgument(
			new Argument<string>("owner", () => "cli",
				"The owner of the server is used to identify the server later with the /info endpoint"),
			(args, owner) => args.owner = owner);
		
		var portOption = new Option<int>("--port", () => 8342, "The port the local server will bind to");
		portOption.AddAlias("-p");
		AddOption(portOption, (args, port) => args.port = port);

		var incPortOption = new Option<bool>("--auto-inc-port", () => true,
			"When true, if the given --port is not available, it will be incremented until an available port is discovered");
		incPortOption.AddAlias("-i");
		AddOption(incPortOption, (args, inc) => args.incPortUntilSuccess = inc);
		
		var timerOption = new Option<int>("--self-destruct-seconds", () => 0,
			"The number of seconds the server will stay alive without receiving any traffic. A value of zero means there is no self destruct timer.");
		timerOption.AddAlias("-d");
		AddOption(timerOption, (args, time) => args.selfDestructTimeSeconds = time);

	}

	public override async Task Handle(ServeCliCommandArgs args)
	{
		args.logData = _logData;
		var server = args.Provider.GetService<ServerService>();
		
		await server.RunServer(args, data =>
		{
			this.SendResults(new ServeCliCommandOutput
			{
				uri = data.uri,
				port = data.port
			});
		});
	}
}
