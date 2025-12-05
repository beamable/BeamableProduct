using cli.Services;
using System.CommandLine;
using Beamable.Server;
using cli.Utils;
using Spectre.Console;

namespace cli;

public class ServicesUpdateDockerfileCommandArgs : CommandArgs
{
	public string ServiceName;
}

public class ServicesUpdateDockerfileCommand : AppCommand<ServicesUpdateDockerfileCommandArgs>, IEmptyResult
{
	public override bool IsForInternalUse => true;
	
	public ServicesUpdateDockerfileCommand() : base("update-dockerfile", ServicesDeletionNotice.REMOVED_PREFIX + "Updates the Dockerfile for the specified service")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("ServiceName", description: "The name of the microservice to udpate the Dockerfile"),
			(args, i) => args.ServiceName = i);
	}

	public override async Task Handle(ServicesUpdateDockerfileCommandArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.STOP_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
	}
}
