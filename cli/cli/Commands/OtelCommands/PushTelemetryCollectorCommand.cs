using Beamable.Common;
using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
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

		var allLogsFiles = FolderManagementHelper.GetAllFiles(args.ConfigService.ConfigTempOtelLogsDirectoryPath);
		var allTracesFiles = FolderManagementHelper.GetAllFiles(args.ConfigService.ConfigTempOtelTracesDirectoryPath);

		List<SerializableLogRecord> allLogs = new List<SerializableLogRecord>();
		List<SerializableActivity> allActivities = new List<SerializableActivity>();

		foreach (string file in allLogsFiles)
		{
			var content = File.ReadAllText(file);
			var logsFromFile = JsonConvert.DeserializeObject<List<SerializableLogRecord>>(content);

			allLogs.AddRange(logsFromFile);
		}

		foreach (string file in allTracesFiles)
		{
			var content = File.ReadAllText(file);
			var activitiesFromFile = JsonConvert.DeserializeObject<List<SerializableActivity>>(content);

			allActivities.AddRange(activitiesFromFile);
		}

		var logRecords = allLogs.Select(LogRecordSerializer.DeserializeLogRecord).ToArray();
		var logsBatch = new Batch<LogRecord>(logRecords, logRecords.Length);

		var activities = allActivities.Select(ActivitySerializer.DeserializeActivity).ToArray();
		var tracesBatch = new Batch<Activity>(activities, activities.Length);


		{ // Sending deserialized telemetry data through Otlp exporter
			var logExporterOptions = new OtlpExporterOptions
			{
				Endpoint = new Uri($"http://127.0.0.1:4355/v1/logs"), //TODO get this in a nicer way
				Protocol = OtlpExportProtocol.HttpProtobuf,
			};

			BaseExporter<LogRecord> otlpLogExporter = new OtlpLogExporter(
				logExporterOptions);

			var logSuccess = otlpLogExporter.Export(logsBatch);

			if (logSuccess == ExportResult.Success)
			{
				//Delete all logs files
				foreach (var file in allLogsFiles)
				{
					File.Delete(file);
				}
			}
			else
			{
				throw new CliException("Error while trying to export logs to collector. Make sure you have a collector running and expecting data.");
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
		}

		return Task.CompletedTask;
	}
}
