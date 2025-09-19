using cli.Services;
using cli.Utils;
using Spectre.Console;

namespace cli;

public class ServicesLogsUrlCommandArgs : LoginCommandArgs
{
	public string BeamoId;
}

public class ServicesLogsUrlCommand : AtomicCommand<ServicesLogsUrlCommandArgs, GetSignedUrlResponse>
{
	private BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;


	public ServicesLogsUrlCommand() :
		base("service-logs",
			ServicesDeletionNotice.REMOVED_PREFIX + "Gets the URL that we can use to see logs our service is emitting")
	{
	}

	public override void Configure()
	{
		AddOption(ServicesRegisterCommand.BEAM_SERVICE_OPTION_ID, (args, s) => args.BeamoId = s);
	}

	public override async Task<GetSignedUrlResponse> GetResult(ServicesLogsUrlCommandArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.UNSUPPORTED_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
	}
}
