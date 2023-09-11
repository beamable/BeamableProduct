using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesMetricsUrlCommandArgs : LoginCommandArgs
{
	public string BeamoId;
	public string MetricName;
}

public class ServicesMetricsUrlCommand : AppCommand<ServicesMetricsUrlCommandArgs>
{
	public static readonly Option<string> METRIC_NAME_OPTION_ID = new("--metric", "Set to 'cpu' or 'memory'");

	private BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;


	public ServicesMetricsUrlCommand() :
		base("service-metrics",
			"Gets the URL that we can use to see the metrics for our services")
	{
	}

	public override void Configure()
	{
		AddOption(ServicesRegisterCommand.BEAM_SERVICE_OPTION_ID, (args, s) => args.BeamoId = s);

		AddOption(METRIC_NAME_OPTION_ID, (args, s) => args.MetricName = s);
	}

	public override async Task Handle(ServicesMetricsUrlCommandArgs args)
	{
		_localBeamo = args.BeamoLocalSystem;
		_remoteBeamo = args.BeamoService;
		// Make sure we are up-to-date with the remote manifest
		var currentRemoteManifest = await _remoteBeamo.GetCurrentManifest();
		// Only allow selecting from services we know are enabled remotely (serviceName maps to Beamo Ids)
		var existingBeamoIds = currentRemoteManifest.manifest.Select(c => c.serviceName).ToList();
		// If we don't have a given BeamoId or if the given one is not currently remotely deployed ask for one.
		if (string.IsNullOrEmpty(args.BeamoId) || currentRemoteManifest.manifest.FindIndex(c => c.serviceName == args.BeamoId) == -1)
			args.BeamoId = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Choose the [lightskyblue1]Beamo-O Service[/] to Modify:")
				.AddChoices(existingBeamoIds)
				.AddBeamHightlight());

		// If we don't have a metric, default to CPU
		if (string.IsNullOrEmpty(args.MetricName))
			args.MetricName = "cpu";

		var response = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Default)
			.StartAsync("Sending Request...", async ctx =>
				await _remoteBeamo.GetMetricsUrl(args.BeamoId, args.MetricName)
			);

		Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
	}
}
