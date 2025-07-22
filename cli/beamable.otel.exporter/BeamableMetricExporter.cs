using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Text.Json;

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

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		var allMetricsSerialized = new List<SerializableMetric>();

		foreach (Metric metric in batch)
		{
			allMetricsSerialized.Add(MetricsSerializer.SerializeMetric(metric));
		}

		var json = JsonSerializer.Serialize(allMetricsSerialized, new JsonSerializerOptions() { WriteIndented = true });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
}
