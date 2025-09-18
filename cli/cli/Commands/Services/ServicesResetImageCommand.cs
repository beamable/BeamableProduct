using cli.Dotnet;
using cli.Utils;
using Spectre.Console;

namespace cli;

public class ServicesResetImageCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();

}

public class ServicesResetImageCommandOutput
{
	public string id;
	public string message;
}

public class ServicesResetImageCommand : StreamCommand<ServicesResetImageCommandArgs, ServicesResetImageCommandOutput>
{
	public ServicesResetImageCommand() : base("image", ServicesDeletionNotice.REMOVED_PREFIX + "Delete any images associated with the given Beamable services")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
	}

	public override async Task Handle(ServicesResetImageCommandArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.UNSUPPORTED_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
	}
}
