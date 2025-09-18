using cli.Services;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public class ServicesMetricsUrlCommandArgs : LoginCommandArgs
{
	public string BeamoId;
	public string MetricName;
}

public class ServicesMetricsUrlCommand : AtomicCommand<ServicesMetricsUrlCommandArgs, GetSignedUrlResponse>
{
	public static readonly Option<string> METRIC_NAME_OPTION_ID = new("--metric", "Set to 'cpu' or 'memory'");

	private BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;


	public ServicesMetricsUrlCommand() :
		base("service-metrics",
			ServicesDeletionNotice.REMOVED_PREFIX + "Gets the URL that we can use to see the metrics for our services")
	{
	}

	public override void Configure()
	{
		AddOption(ServicesRegisterCommand.BEAM_SERVICE_OPTION_ID, (args, s) => args.BeamoId = s);

		AddOption(METRIC_NAME_OPTION_ID, (args, s) => args.MetricName = s);
	}

	public override async Task<GetSignedUrlResponse> GetResult(ServicesMetricsUrlCommandArgs args)
	{
		AnsiConsole.MarkupLine(ServicesDeletionNotice.TITLE);
		AnsiConsole.MarkupLine(ServicesDeletionNotice.UNSUPPORTED_MESSAGE);
		throw CliExceptions.COMMAND_NO_LONGER_SUPPORTED;
	}
}
