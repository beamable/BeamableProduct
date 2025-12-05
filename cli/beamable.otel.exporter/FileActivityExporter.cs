using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using OpenTelemetry.Resources;
using System.Diagnostics;
using System.Text.Json;

namespace beamable.otel.exporter;

public class FileActivityExporter : FileExporter<Activity>
{
	private readonly string _filesPath;
	private Resource? resource;

	public FileActivityExporter(FileExporterOptions options) : base(options)
	{
		_filesPath = options.ExportPath;
	}

	public override ExportResult Export(in Batch<Activity> batch)
	{
		if (!Directory.Exists(_filesPath))
		{
			return ExportResult.Failure;
		}

		var res = this.resource ?? this.ParentProvider.GetResource();

		var filePath = FolderManagementHelper.GetDestinationFilePath(_filesPath);

		var allActivitiesSerialized = new List<SerializableActivity>();

		foreach (var activity in batch)
		{
			if (activity.Status != ActivityStatusCode.Error) // For now we only care about error traces
			{
				continue;
			}
			allActivitiesSerialized.Add(ActivitySerializer.SerializeActivity(activity));
		}

		if (allActivitiesSerialized.Count == 0)
		{
			return ExportResult.Success;
		}

		var serializedBatch = new ActivityBatch()
		{
			AllTraces = allActivitiesSerialized,
			ResourceAttributes = res.Attributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()),
			SchemaVersion = ExporterConstants.SchemaVersion
		};

		var json = JsonSerializer.Serialize(serializedBatch, new JsonSerializerOptions() { WriteIndented = true });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
}
