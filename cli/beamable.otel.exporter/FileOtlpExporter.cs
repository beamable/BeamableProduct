using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.Diagnostics;

namespace beamable.otel.exporter;

public class FileOtlpExporter
{
	public static ExportResult ExportLogs(string filesPath, string endpoint, out string errorMessage)
	{
		var allLogsFiles = FolderManagementHelper.GetAllFiles(filesPath);

		var logExporterOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{endpoint}/v1/logs"),
			Protocol = OtlpExportProtocol.HttpProtobuf,
		};

		OtlpLogExporter otlpLogExporter = new OtlpLogExporter(logExporterOptions);

		var result = ExportResult.Success;

		foreach (string file in allLogsFiles)
		{
			var content = File.ReadAllText(file);

			if (string.IsNullOrEmpty(content))
			{
				continue; //ignore this file for now, it will be deleted in the deletion part of this
			}

			var deserializedBatch = JsonConvert.DeserializeObject<LogsBatch>(content);

			var logRecords = deserializedBatch.AllRecords.Select(LogRecordSerializer.DeserializeLogRecord).ToArray();
			var logsBatch = new Batch<LogRecord>(logRecords, logRecords.Length);

			var objectDict = deserializedBatch.ResourceAttributes.ToDictionary(
				kvp => kvp.Key,
				kvp => OtlpExporterResourceInjector.ParseStringToObject(kvp.Value)
			);

			if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpLogExporter>(otlpLogExporter, objectDict, out errorMessage))
			{
				return ExportResult.Failure;
			}

			result = otlpLogExporter.Export(logsBatch);

			if (result == ExportResult.Failure)
			{
				errorMessage = "Error while using OtlpExporter from OpenTelemetry to send data to collector";
				return result;
			}
			else
			{
				FolderManagementHelper.DeleteFileInPath(file);
			}
		}

		errorMessage = string.Empty;
		return result;
	}

	public static ExportResult ExportTraces(string filesPath, string endpoint, out string errorMessage)
	{
		var allFiles = FolderManagementHelper.GetAllFiles(filesPath);

		var options = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{endpoint}/v1/traces"),
			Protocol = OtlpExportProtocol.HttpProtobuf,
		};

		OtlpTraceExporter otlpTraceExporter = new OtlpTraceExporter(options);

		var result = ExportResult.Success;

		foreach (string file in allFiles)
		{
			var content = File.ReadAllText(file);

			if (string.IsNullOrEmpty(content))
			{
				continue; //ignore this file for now, it will be deleted in the deletion part of this
			}

			var deserializedBatch = JsonConvert.DeserializeObject<ActivityBatch>(content);

			var activities = deserializedBatch.AllTraces.Select(ActivitySerializer.DeserializeActivity).ToArray();
			var activitiesBatch = new Batch<Activity>(activities, activities.Length);

			var objectDict = deserializedBatch.ResourceAttributes.ToDictionary(
				kvp => kvp.Key,
				kvp => OtlpExporterResourceInjector.ParseStringToObject(kvp.Value)
			);

			if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpTraceExporter>(otlpTraceExporter, objectDict, out errorMessage))
			{
				return ExportResult.Failure;
			}

			result = otlpTraceExporter.Export(activitiesBatch);

			if (result == ExportResult.Failure)
			{
				errorMessage = "Error while using OtlpExporter from OpenTelemetry to send data to collector";
				return result;
			}
			else
			{
				FolderManagementHelper.DeleteFileInPath(file);
			}
		}

		errorMessage = string.Empty;
		return result;
	}

	public static ExportResult ExportMetrics(string filesPath, string endpoint, out string errorMessage)
	{
		var allFiles = FolderManagementHelper.GetAllFiles(filesPath);

		var options = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{endpoint}/v1/metrics"),
			Protocol = OtlpExportProtocol.HttpProtobuf,
		};

		OtlpMetricExporter otlpMetricsExporter = new OtlpMetricExporter(options);

		var result = ExportResult.Success;

		foreach (string file in allFiles)
		{
			var content = File.ReadAllText(file);

			if (string.IsNullOrEmpty(content))
			{
				continue; //ignore this file for now, it will be deleted in the deletion part of this
			}

			var deserializedBatch = JsonConvert.DeserializeObject<MetricsBatch>(content);

			var metrics = deserializedBatch.AllMetrics.Select(MetricsSerializer.DeserializeMetric).ToArray();
			var metricsBatch = new Batch<Metric>(metrics, metrics.Length);

			var objectDict = deserializedBatch.ResourceAttributes.ToDictionary(
				kvp => kvp.Key,
				kvp => OtlpExporterResourceInjector.ParseStringToObject(kvp.Value)
			);

			if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpMetricExporter>(otlpMetricsExporter, objectDict, out errorMessage))
			{
				return ExportResult.Failure;
			}

			result = otlpMetricsExporter.Export(metricsBatch);
			otlpMetricsExporter.ForceFlush();

			if (result == ExportResult.Failure)
			{
				errorMessage = "Error while using OtlpExporter from OpenTelemetry to send data to collector";
				return result;
			}
			else
			{
				FolderManagementHelper.DeleteFileInPath(file);
			}
		}

		errorMessage = string.Empty;
		return result;
	}
}
