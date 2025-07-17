using beamable.otel.exporter.Utils;
using OpenTelemetry;
using System.Diagnostics;

namespace beamable.otel.exporter;

public class BeamableActivityExporter : BeamableExporter<Activity>
{
	private readonly string _filesPath;

	public BeamableActivityExporter(BeamableExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
	}

	public override ExportResult Export(in Batch<Activity> batch) // timestamp_logger.json
	{
		FolderManagementHelper.EnsureDestinationFolderExists(_filesPath);

		var nowTime = DateTime.UtcNow;

		var currentDay = nowTime.ToString("ddMMyyyy");
		var currentTime = $"{nowTime:HHmmss}_{nowTime:ffff}";
		var datedPath = Path.Combine(_filesPath, currentDay);

		if (!Directory.Exists(datedPath))
		{
			Directory.CreateDirectory(datedPath);
		}

		var fileName = $"{currentTime}.json";
		var finalFilePath = Path.Combine(datedPath, fileName);

		return ExportResult.Success;
	}
}
