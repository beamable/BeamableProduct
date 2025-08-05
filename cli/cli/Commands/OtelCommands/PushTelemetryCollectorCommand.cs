using Beamable.Common;
using beamable.otel.exporter;
using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Diagnostics;

namespace cli.OtelCommands;

[Serializable]
public class PushTelemetryCollectorCommandArgs : CommandArgs
{
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

		var allTracesFiles = FolderManagementHelper.GetAllFiles(args.ConfigService.ConfigTempOtelTracesDirectoryPath);
		var allMetricsFiles = FolderManagementHelper.GetAllFiles(args.ConfigService.ConfigTempOtelMetricsDirectoryPath);
		List<SerializableActivity> allActivities = new List<SerializableActivity>();
		List<SerializableMetric> allMetrics = new List<SerializableMetric>();

		foreach (string file in allTracesFiles)
		{
			var content = File.ReadAllText(file);
			var activitiesFromFile = JsonConvert.DeserializeObject<List<SerializableActivity>>(content);

			allActivities.AddRange(activitiesFromFile);
		}

		foreach (string file in allMetricsFiles)
		{
			var content = File.ReadAllText(file);
			var metricsFromFile = JsonConvert.DeserializeObject<List<SerializableMetric>>(content);

			allMetrics.AddRange(metricsFromFile);
		}

		var activities = allActivities.Select(ActivitySerializer.DeserializeActivity).ToArray();
		var tracesBatch = new Batch<Activity>(activities, activities.Length);

		var metrics = allMetrics.Select(MetricsSerializer.DeserializeMetric).ToArray();
		var metricsBatch = new Batch<Metric>(metrics, metrics.Length);


		{ // Sending deserialized telemetry data through Otlp exporter
			var logsResult = BeamableOtlpExporter.ExportLogs(args.ConfigService.ConfigTempOtelLogsDirectoryPath);

			if (logsResult == ExportResult.Failure)
			{
				throw new CliException(
					"Failed to export logs. Make sure there is a collector receiving data in the correct endpoint"); //TODO improve this log with more information
			}

			var activityExporterOptions = new OtlpExporterOptions
			{
				Endpoint = new Uri($"http://127.0.0.1:4355/v1/traces"), //TODO get this in a nicer way
				Protocol = OtlpExportProtocol.HttpProtobuf,
			};

			BaseExporter<Activity> otlpActivityExporter = new OtlpTraceExporter(
				activityExporterOptions);

			var traceSuccess = otlpActivityExporter.Export(tracesBatch);

			if (traceSuccess == ExportResult.Success)
			{
				//Delete all traces files
				foreach (var file in allTracesFiles)
				{
					File.Delete(file);
				}
			}
			else
			{
				throw new CliException("Error while trying to export traces to collector. Make sure you have a collector running and expecting data.");
			}


			var metricExporterOptions = new OtlpExporterOptions
			{
				Endpoint = new Uri($"http://127.0.0.1:4355/v1/metrics"), //TODO use the correct values here
				Protocol = OtlpExportProtocol.HttpProtobuf,
			};

			BaseExporter<Metric> otlpMetricExporter = new OtlpMetricExporter(
				metricExporterOptions);

			var metricSuccess = otlpMetricExporter.Export(metricsBatch);
			otlpMetricExporter.ForceFlush();;

			if (metricSuccess == ExportResult.Success)
			{
				//Delete all metrics files
				foreach (var file in allMetricsFiles)
				{
					File.Delete(file);
				}
			}
			else
			{
				throw new CliException("Error while trying to export metrics to collector. Make sure you have a collector running and expecting data.");
			}

		}

		return Task.CompletedTask;
	}
}
