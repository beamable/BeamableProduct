using beamable.otel.exporter;
using OpenTelemetry;

namespace cli.OtelCommands;

[Serializable]
public class PushTelemetryCollectorCommandArgs : CommandArgs
{
	//TODO add args here: endpoint, auto-run-collector
}

public class PushTelemetryCollectorCommand : AppCommand<PushTelemetryCollectorCommandArgs>
{
	public PushTelemetryCollectorCommand() : base("push", "Pushes local telemetry data saved through the BeamableExporter to a running collector")
	{
	}

	public override void Configure()
	{
	}

	public override Task Handle(PushTelemetryCollectorCommandArgs args)
	{
		if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelLogsDirectoryPath))
		{
			throw new CliException("Couldn't resolve telemetry logs path");
		}

		if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelTracesDirectoryPath))
		{
			throw new CliException("Couldn't resolve telemetry traces path");
		}

		if (string.IsNullOrEmpty(args.ConfigService.ConfigTempOtelMetricsDirectoryPath))
		{
			throw new CliException("Couldn't resolve telemetry metrics path");
		}

		{ // Sending deserialized telemetry data through Otlp exporter
			//TODO: get the endpoint through command args, also enable the option of running the standard collector first and then exporting the data, which we can get the endpoint by that manner as well
			var logsResult = FileOtlpExporter.ExportLogs(args.ConfigService.ConfigTempOtelLogsDirectoryPath, "http://127.0.0.1:4355");

			if (logsResult == ExportResult.Failure)
			{
				throw new CliException(
					"Failed to export logs. Make sure there is a collector receiving data in the correct endpoint"); //TODO improve this log with more information
			}

			var traceResult = FileOtlpExporter.ExportTraces(args.ConfigService.ConfigTempOtelTracesDirectoryPath, "http://127.0.0.1:4355");

			if (traceResult == ExportResult.Failure)
			{
				throw new CliException("Error while trying to export traces to collector. Make sure you have a collector running and expecting data.");
			}

			var metricsResult = FileOtlpExporter.ExportMetrics(args.ConfigService.ConfigTempOtelMetricsDirectoryPath, "http://127.0.0.1:4355");

			if (metricsResult == ExportResult.Failure)
			{
				throw new CliException("Error while trying to export metrics to collector. Make sure you have a collector running and expecting data.");
			}

		}

		return Task.CompletedTask;
	}
}
