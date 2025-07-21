namespace beamable.otel.exporter.Utils;

public static class FolderManagementHelper
{
	public static void EnsureDestinationFolderExists(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}

	public static string GetDestinationFilePath(string telemetryBasePath)
	{
		var nowTime = DateTime.UtcNow;

		var currentDay = nowTime.ToString("ddMMyyyy");
		var currentTime = $"{nowTime:HHmmss}_{nowTime:ffff}";
		var datedPath = Path.Combine(telemetryBasePath, currentDay);

		if (!Directory.Exists(datedPath))
		{
			Directory.CreateDirectory(datedPath);
		}

		var fileName = $"{currentTime}.json";
		var finalFilePath = Path.Combine(datedPath, fileName);

		return finalFilePath;
	}

	public static List<string> GetAllFiles(string path)
	{
		var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
		return allFiles.ToList();
	}
}
