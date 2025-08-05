using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

namespace beamable.otel.exporter;

public class BeamableOtlpExporter
{
	public static ExportResult ExportLogs(string filesPath)
	{
		var allLogsFiles = FolderManagementHelper.GetAllFiles(filesPath);

		var logExporterOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri($"http://127.0.0.1:4355/v1/logs"), //TODO get this in a nicer way
			Protocol = OtlpExportProtocol.HttpProtobuf,
		};

		OtlpLogExporter otlpLogExporter = new OtlpLogExporter(logExporterOptions);

		var result = ExportResult.Success;

		foreach (string file in allLogsFiles)
		{
			var content = File.ReadAllText(file);
			var deserializedBatch = JsonConvert.DeserializeObject<LogsBatch>(content);

			var logRecords = deserializedBatch.AllRecords.Select(LogRecordSerializer.DeserializeLogRecord).ToArray();
			var logsBatch = new Batch<LogRecord>(logRecords, logRecords.Length);

			var objectDict = deserializedBatch.ResourceAttributes.ToDictionary(
				kvp => kvp.Key,
				kvp => OtlpExporterResourceInjector.ParseStringToObject(kvp.Value)
			);

			if (!OtlpExporterResourceInjector.TryInjectResource(otlpLogExporter, objectDict))
			{
				return ExportResult.Failure;
			}

			result = otlpLogExporter.Export(logsBatch);

			if (result == ExportResult.Failure)
			{
				break;
			}
		}

		if (result == ExportResult.Success)
		{
			foreach (var f in allLogsFiles)
			{
				File.Delete(f);
			}
		}

		return result;
	}
}
