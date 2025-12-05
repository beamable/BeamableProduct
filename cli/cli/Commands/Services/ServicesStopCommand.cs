using Beamable.Common;
using cli.Dotnet;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace cli;

public class ServicesStopCommandArgs : CommandArgs
{
	public List<string> services;
}

public class ServicesStopCommand : AppCommand<ServicesStopCommandArgs>, IEmptyResult
{
	public ServicesStopCommand() :
		base("stop", ServicesDeletionNotice.REMOVED_PREFIX + "Stops all your locally running containers for the selected Beamo Services")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
	}

	public override async Task Handle(ServicesStopCommandArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.STOP_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
	}
}
