using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using System.Diagnostics;
using System.Text.Json;

namespace beamable.otel.exporter;

public class FileActivityExporter : FileExporter<Activity>
{
	private readonly string _filesPath;

	public FileActivityExporter(FileExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
	}

	public override ExportResult Export(in Batch<Activity> batch)
	{
		var resource = this.ParentProvider.GetResource();
		FolderManagementHelper.EnsureDestinationFolderExists(_filesPath);

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		var allActivitiesSerialized = new List<SerializableActivity>();

		foreach (var activity in batch)
		{
			allActivitiesSerialized.Add(ActivitySerializer.SerializeActivity(activity));
		}

		var serializedBatch = new ActivityBatch()
		{
			AllTraces = allActivitiesSerialized,
			ResourceAttributes = resource.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
			SchemaVersion = ExporterConstants.SchemaVersion
		};

		var json = JsonSerializer.Serialize(serializedBatch, new JsonSerializerOptions() { WriteIndented = true });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
}
