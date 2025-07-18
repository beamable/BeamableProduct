using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using System.Text.Json;

namespace beamable.otel.exporter;

public class BeamableLogRecordExporter : BeamableExporter<LogRecord>
{
	private readonly string _filesPath;
	public BeamableLogRecordExporter(BeamableExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
	}

	public override ExportResult Export(in Batch<LogRecord> batch) //timestamp_day/timestamp_hour_min_file.json
	{
		FolderManagementHelper.EnsureDestinationFolderExists(_filesPath);

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		var allLogsSerialized = new List<SerializableLogRecord>();

		foreach (var log in batch)
		{
			allLogsSerialized.Add(LogRecordSerializer.SerializeLogRecord(log));
		}

		var json = JsonSerializer.Serialize(allLogsSerialized, new JsonSerializerOptions() { WriteIndented = true });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
}
