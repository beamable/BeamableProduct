using beamable.otel.exporter.Utils;
using OpenTelemetry;
using OpenTelemetry.Logs;

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

		return ExportResult.Success;
	}
}
