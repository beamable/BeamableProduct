using Beamable.Server;
using System.Text;
using FileConstants = Beamable.Common.Constants.Files;

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
				if(!File.Exists(file))
					continue;
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

	
	
	/// <summary>
	/// Writes a UTF8 file without a BOM asynchronously.
	/// </summary>
	/// <remarks>It can be useful, especially in cases when the file would be created on Windows and accessed on other OS.
	/// On Windows by default, the file can have a BOM, which will cause problems when reading it on Linux or macOS.
	/// </remarks>
	/// <param name="path"></param>
	/// <param name="content"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>Task to await</returns>
	public static Task WriteUtf8FileAsync(string path, string content, CancellationToken cancellationToken = default)
	{
		return File.WriteAllTextAsync(path, content, FileConstants.DEFAULT_FILE_ENCONDING, cancellationToken);
	}
	/// <summary>
	/// Writes a UTF8 file without a BOM.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="content"></param>
	public static void WriteUtf8File(string path, string content)
	{
		File.WriteAllText(path, content, FileConstants.DEFAULT_FILE_ENCONDING);
	}
}
