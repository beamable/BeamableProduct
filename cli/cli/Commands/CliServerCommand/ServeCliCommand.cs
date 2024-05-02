using Beamable.Server;
using cli.Services.HttpServer;
using Serilog;
using System.CommandLine;

namespace cli.CliServerCommand;

public class ServeCliCommandArgs : CommandArgs
{
	public int port;
	public string owner;
	public int selfDestructTimeSeconds;
	public bool incPortUntilSuccess;
}

public class ServeCliCommandOutput
{
	public int port;
	public string uri;
}

public class ServeCliCommand : StreamCommand<ServeCliCommandArgs, ServeCliCommandOutput>
{
	public ServeCliCommand() : base("serve", "create a local server for the cli")
	{
	}

	public override void Configure()
	{
		AddArgument(
			new Argument<string>("owner", () => "cli",
				"the owner of the server is used to identify the server later with the /info endpoint"),
			(args, owner) => args.owner = owner);
		
		var portOption = new Option<int>("--port", () => 8342, "the port the local server will bind to");
		portOption.AddAlias("-p");
		AddOption(portOption, (args, port) => args.port = port);

		var timerOption = new Option<int>("--self-destruct-seconds", () => 0,
			"the number of seconds the server will stay alive without receiving any traffic");
		timerOption.AddAlias("-d");
		AddOption(timerOption, (args, time) => args.selfDestructTimeSeconds = time);

		var incPortOption = new Option<bool>("--auto-inc-port", () => true,
			"when true, if the given --port is not available, it will be incremented until an available port is discovered");
		incPortOption.AddAlias("-i");
		AddOption(incPortOption, (args, inc) => args.incPortUntilSuccess = inc);
	}

	public override async Task Handle(ServeCliCommandArgs args)
	{
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
