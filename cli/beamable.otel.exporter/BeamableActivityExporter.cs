using beamable.otel.exporter.Serialization;
using beamable.otel.exporter.Utils;
using OpenTelemetry;
using System.Diagnostics;
using System.Text.Json;

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

		var allActivitiesSerialized = new List<SerializableActivity>();

		foreach (var activity in batch)
		{
			allActivitiesSerialized.Add(ActivitySerializer.SerializeActivity(activity));
		}

		var json = JsonSerializer.Serialize(allActivitiesSerialized, new JsonSerializerOptions() { WriteIndented = true });

		File.WriteAllText(filePath, json + Environment.NewLine);

		return ExportResult.Success;
	}
}
