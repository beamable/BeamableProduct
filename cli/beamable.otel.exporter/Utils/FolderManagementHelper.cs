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
}
