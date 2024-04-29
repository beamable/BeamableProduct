using Beamable.Server;
using cli.Services.HttpServer;
using Serilog;
using System.CommandLine;

namespace cli.CliServerCommand;

public class ServeCliCommandArgs : CommandArgs
{
	public int port;
	public string owner;
}

public class ServeCliCommandOutput
{
	
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
		
	}

	public override async Task Handle(ServeCliCommandArgs args)
	{
		var server = args.Provider.GetService<ServerService>();
		await server.RunServer(args);
	}
}
