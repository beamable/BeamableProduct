using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
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
	private Resource? resource;

	public FileLogRecordExporter(FileExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
	}

	public override ExportResult Export(in Batch<LogRecord> batch)
	{
		var resource = this.ParentProvider.GetResource();
		FolderManagementHelper.EnsureDestinationFolderExists(_filesPath);

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		var allLogsSerialized = new List<SerializableLogRecord>();

		foreach (var log in batch)
		{
			allLogsSerialized.Add(LogRecordSerializer.SerializeLogRecord(log));
		}

		var serializedBatch = new LogsBatch()
		{
			AllRecords = allLogsSerialized,
			ResourceAttributes = resource.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
			SchemaVersion = ExporterConstants.SchemaVersion
		};

		var json = JsonSerializer.Serialize(serializedBatch, new JsonSerializerOptions() { WriteIndented = true });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
}
