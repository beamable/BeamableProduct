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

	public override ExportResult Export(in Batch<Activity> batch)
	{
		FolderManagementHelper.EnsureDestinationFolderExists(_filesPath);

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		return ExportResult.Success;
	}
}
