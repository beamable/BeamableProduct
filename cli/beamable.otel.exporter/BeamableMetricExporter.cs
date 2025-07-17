using beamable.otel.exporter.Utils;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace beamable.otel.exporter;

public class BeamableMetricExporter : BeamableExporter<Metric>
{
	private readonly string _filesPath;

	public BeamableMetricExporter(BeamableExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
	}

	public override ExportResult Export(in Batch<Metric> batch)
	{
		FolderManagementHelper.EnsureDestinationFolderExists(_filesPath);

		return ExportResult.Success;
	}
}
