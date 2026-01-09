using beamable.otel.common;
using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace beamable.otel.exporter;

public class FileOtlpExporter
{
	public static async Task<(ExportResult resultStatus, string errorMessage)> ExportLogs(string filesPath, string endpoint)
	{
		string errorMessage = string.Empty;
		var allLogsFiles = FolderManagementHelper.GetAllFiles(filesPath);

		var logExporterOptions = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{endpoint}/v1/logs"),
			Protocol = OtlpExportProtocol.HttpProtobuf
		};

		OtlpLogExporter otlpLogExporter = new OtlpLogExporter(logExporterOptions);

		var result = ExportResult.Success;
		
		List<(string, LogsBatch?)> allBatches;
		
		try
		{
			allBatches = await ReadAndDeserializeFileAsync<LogsBatch>(allLogsFiles);
		}
		catch (Exception e)
		{
			return (ExportResult.Failure, e.Message);
		}

		foreach ((string path, LogsBatch? deserializedBatch) in allBatches)
		{
			if(deserializedBatch == null)
				continue;
			var logRecords = deserializedBatch.AllRecords.Select(LogRecordSerializer.DeserializeLogRecord).ToArray();
			var logsBatch = new Batch<LogRecord>(logRecords, logRecords.Length);

			var objectDict = deserializedBatch.ResourceAttributes.ToDictionary(
				kvp => kvp.Key,
				kvp => OtlpExporterResourceInjector.ParseStringToObject(kvp.Value)
			);

			if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpLogExporter>(otlpLogExporter, objectDict, out errorMessage))
			{
				return (ExportResult.Failure, errorMessage);
			}

			result = otlpLogExporter.Export(logsBatch);

			if (result == ExportResult.Failure)
			{
				errorMessage = "Error while using OtlpExporter from OpenTelemetry to send data to collector";
				return (result, errorMessage);
			}

			FolderManagementHelper.DeleteFileInPath(path);
		}
		
		return (result, errorMessage);
	}

	public static async Task<(ExportResult resultStatus, string errorMessage)> ExportTraces(string filesPath, string endpoint)
	{
		string errorMessage = string.Empty;
		var allFiles = FolderManagementHelper.GetAllFiles(filesPath);

		var options = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{endpoint}/v1/traces"),
			Protocol = OtlpExportProtocol.HttpProtobuf,
		};

		OtlpTraceExporter otlpTraceExporter = new OtlpTraceExporter(options);

		var result = ExportResult.Success;
		List<(string, ActivityBatch?)> allBatches;
		
		try
		{
			allBatches = await ReadAndDeserializeFileAsync<ActivityBatch>(allFiles);
		}
		catch (Exception e)
		{
			return (ExportResult.Failure, e.Message);
		}

		foreach ((string path, ActivityBatch? deserializedBatch) in allBatches)
		{
			if(deserializedBatch == null)
				continue;
			
			var activities = deserializedBatch.AllTraces.Select(ActivitySerializer.DeserializeActivity).ToArray();
			var activitiesBatch = new Batch<Activity>(activities, activities.Length);

			var objectDict = deserializedBatch.ResourceAttributes.ToDictionary(
				kvp => kvp.Key,
				kvp => OtlpExporterResourceInjector.ParseStringToObject(kvp.Value)
			);

			if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpTraceExporter>(otlpTraceExporter, objectDict, out errorMessage))
			{
				return (ExportResult.Failure, errorMessage);
			}

			result = otlpTraceExporter.Export(activitiesBatch);

			if (result == ExportResult.Failure)
			{
				errorMessage = "Error while using OtlpExporter from OpenTelemetry to send data to collector";
				return (result, errorMessage);
			}

			FolderManagementHelper.DeleteFileInPath(path);
		}

		
		return (result, errorMessage);
	}

	public static async Task<(ExportResult resultStatus, string errorMessage)> ExportMetrics(string filesPath, string endpoint)
	{
		string errorMessage = string.Empty;
		var allFiles = FolderManagementHelper.GetAllFiles(filesPath);

		var options = new OtlpExporterOptions
		{
			Endpoint = new Uri($"{endpoint}/v1/metrics"),
			Protocol = OtlpExportProtocol.HttpProtobuf,
		};

		OtlpMetricExporter otlpMetricsExporter = new OtlpMetricExporter(options);

		var result = ExportResult.Success;
		
		List<(string, MetricsBatch?)> allBatches;
		
		try
		{
			allBatches = await ReadAndDeserializeFileAsync<MetricsBatch>(allFiles);
		}
		catch (Exception e)
		{
			return (ExportResult.Failure, e.Message);
		}

		foreach ((string path, MetricsBatch? deserializedBatch) in allBatches)
		{
			if(deserializedBatch == null)
				continue;
			var metrics = deserializedBatch.AllMetrics.Select(MetricsSerializer.DeserializeMetric).ToArray();
			var metricsBatch = new Batch<Metric>(metrics, metrics.Length);

			var objectDict = deserializedBatch.ResourceAttributes.ToDictionary(
				kvp => kvp.Key,
				kvp => OtlpExporterResourceInjector.ParseStringToObject(kvp.Value)
			);

			if (!OtlpExporterResourceInjector.TrySetResourceField<OtlpMetricExporter>(otlpMetricsExporter, objectDict, out errorMessage))
			{
				return (ExportResult.Failure, errorMessage);
			}

			result = otlpMetricsExporter.Export(metricsBatch);
			otlpMetricsExporter.ForceFlush();

			if (result == ExportResult.Failure)
			{
				errorMessage = "Error while using OtlpExporter from OpenTelemetry to send data to collector";
				return (result, errorMessage);
			}

			FolderManagementHelper.DeleteFileInPath(path);
		}

		
		return (result, errorMessage);
	}

	private static async Task<List<(string,T?)>> ReadAndDeserializeFileAsync<T>(List<string> paths)
	{
		using var cts = new CancellationTokenSource();
		var results = new ConcurrentBag<(string, T?)>();

		
		
		await Parallel.ForEachAsync(paths, cts.Token, async (path, cancellationToken) =>
		{
			try
			{
				string fileContent = await File.ReadAllTextAsync(path, cancellationToken);
				if (!string.IsNullOrEmpty(fileContent))
				{
					var deserialized = JsonConvert.DeserializeObject<T>(fileContent);
					results.Add((path, deserialized));
				}
			}
			catch (Exception ex)
			{
				await cts.CancelAsync();
				throw new InvalidOperationException($"Failed to process file {path}", ex);
			}
		});

		return results.ToList();
	}
}
