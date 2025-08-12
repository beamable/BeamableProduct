using Beamable.Server;

namespace cli.Utils;

public class DirectoryInfoUtils
{
	public long Size;
	public int FileCount;
}

public static class DirectoryUtils
{
	public static DirectoryInfoUtils CalculateDirectorySize(string path)
	{
		var result = new DirectoryInfoUtils()
		{
			FileCount = 0,
			Size = 0
		};

		var files = Directory.GetFiles(path);
		foreach (string file in files)
		{
			try
			{
				var fileInfo = new FileInfo(file);
				result.Size += fileInfo.Length;
				result.FileCount++;
			}
			catch (Exception ex)
			{
				Log.Warning($"Could not access file {file}: {ex.Message}");
			}
		}

		var subdirectories = Directory.GetDirectories(path);
		foreach (string subdirectory in subdirectories)
		{
			try
			{
				var subResult = CalculateDirectorySize(subdirectory);
				result.Size += subResult.Size;
				result.FileCount += subResult.FileCount;
			}
			catch (Exception ex)
			{
				Log.Warning($"Could not access directory {subdirectory}: {ex.Message}");
			}
		}

		return result;
	}

	public static string FormatBytes(long size)
	{
		string[] sizes = { "B", "KB", "MB", "GB" };
		double len = size;
		int order = 0;
		while (len >= 1024 && order < sizes.Length - 1)
		{
			order++;
			len = len / 1024;
		}
		return $"{len:0.##} {sizes[order]}";
	}
}
