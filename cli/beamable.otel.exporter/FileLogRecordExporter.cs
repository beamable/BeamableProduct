using Beamable.Common;
using beamable.otel.common;
using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Text.Json;

namespace beamable.otel.exporter;

public class FileLogRecordExporter : FileExporter<LogRecord>
{
	private readonly string _filesPath;
	private readonly LogLevel _minimalLogLevel;
	private Resource? resource;

	public FileLogRecordExporter(FileExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
		_minimalLogLevel = options.MinimalLogLevel;
	}

	public override ExportResult Export(in Batch<LogRecord> batch)
	{
		if (!Directory.Exists(_filesPath))
		{
			return ExportResult.Failure;
		}

		var res = this.resource ?? this.ParentProvider.GetResource();

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		var allLogsSerialized = new List<SerializableLogRecord>();
		
		foreach (var log in batch)
		{
			//We only export what is configured to be, the default being "Warning" and above
			// And don't contains the attribute Constants.IGNORE_TELEMETRY_ATTRIBUTE
			if (LogRecordFilter(log, _minimalLogLevel))
			{
				allLogsSerialized.Add(LogRecordSerializer.SerializeLogRecord(log));
			}
		}

		if (allLogsSerialized.Count == 0)
		{
			return ExportResult.Success;
		}

		var serializedBatch = new LogsBatch()
		{
			AllRecords = allLogsSerialized,
			ResourceAttributes = res.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
			SchemaVersion = ExporterConstants.SchemaVersion
		};

		var json = JsonSerializer.Serialize(serializedBatch, new JsonSerializerOptions() { WriteIndented = false });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
	
	/// <summary>
	/// Filter the log by the minimal Level and the attributes on this
	/// </summary>
	/// <param name="record"></param>
	/// <param name="minimalLevel"></param>
	/// <returns></returns>
	public static bool LogRecordFilter(LogRecord record, LogLevel minimalLevel)
	{
		bool isValid = record.LogLevel >= minimalLevel;
		
		if (record.Attributes != null)
		{
			isValid &= !record.Attributes.Any(item => item.Key.Contains(nameof(Constants.IGNORE_TELEMETRY_ATTRIBUTE)));
		}
		
		return isValid;
	}
}
