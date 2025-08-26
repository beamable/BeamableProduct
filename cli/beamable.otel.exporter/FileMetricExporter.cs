using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Text.Json;

namespace beamable.otel.exporter;

public class FileMetricExporter : FileExporter<Metric>
{
	private readonly string _filesPath;
	private Resource? resource;

	public FileMetricExporter(FileExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
	}

	public override ExportResult Export(in Batch<Metric> batch)
	{
		var res = this.resource ?? this.ParentProvider.GetResource();
		FolderManagementHelper.EnsureDestinationFolderExists(_filesPath);

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		var allMetricsSerialized = new List<SerializableMetric>();

		foreach (Metric metric in batch)
		{
			allMetricsSerialized.Add(MetricsSerializer.SerializeMetric(metric));
		}

		var serializedBatch = new MetricsBatch()
		{
			AllMetrics = allMetricsSerialized,
			ResourceAttributes = res.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
			SchemaVersion = ExporterConstants.SchemaVersion
		};

		var json = JsonSerializer.Serialize(serializedBatch, new JsonSerializerOptions() { WriteIndented = true });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
}
