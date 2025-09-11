
namespace beamable.otel.exporter.Utils;

public class CleanupResult
{
	public bool Success;
	public List<string> ErrorMessages = new List<string>();
	public long BytesFreed { get; set; }

	public void Merge(CleanupResult other)
	{
		BytesFreed += other.BytesFreed;
		ErrorMessages.AddRange(other.ErrorMessages);
	}
}

public static class FolderManagementHelper
{
	public static string GetDestinationFilePath(string telemetryBasePath)
	{
		var nowTime = DateTime.UtcNow;

		var currentDay = nowTime.ToString("yyyyMMdd");
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
		var allFiles = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
		return allFiles.ToList();
	}

	public static void DeleteFileInPath(string path)
	{
		File.Delete(path);
		var dir = Path.GetDirectoryName(path);

		if (!Directory.EnumerateFiles(dir).Any())
		{
			Directory.Delete(dir);
		}
	}

	public static CleanupResult ClearOldTelemetryFiles(string path, int retentionDays, bool forceDeleteALl = false)
	{
		var result = new CleanupResult();
		var cutoffDate = DateTime.UtcNow.Date.AddDays(-retentionDays);

		try
		{
			var dateFolders = GetALlValidFoldersWithTelemetry(path);

			foreach (var folder in dateFolders)
			{
				var folderDate = ParseFolderDate(folder.Name);
				var shouldDelete = forceDeleteALl || folderDate <= cutoffDate;

				if (shouldDelete)
				{
					var folderResult = ProcessFolderCleanup(folder);
					result.Merge(folderResult);
				}
			}

			result.Success = true;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessages.Add(ex.Message);
		}

		return result;
	}

	private static CleanupResult ProcessFolderCleanup(DirectoryInfo folder)
	{
		var result = new CleanupResult();

		var allFiles = folder.GetFiles("*.json", SearchOption.AllDirectories);

		foreach (var file in allFiles)
		{
			try
			{
				result.BytesFreed += file.Length;

				file.Delete();
			}
			catch (Exception ex)
			{
				result.ErrorMessages.Add($"Error deleting file {file.Name}: {ex.Message}");
			}
		}

		if (!folder.GetFiles().Any() && !folder.GetDirectories().Any())
		{
			folder.Delete();
		}

		return result;
	}

	private static List<DirectoryInfo> GetALlValidFoldersWithTelemetry(string path)
	{
		var baseDir = new DirectoryInfo(path);
		var dateFolders = new List<DirectoryInfo>();

		foreach (var directory in baseDir.GetDirectories())
		{
			if (IsValidDateFolderName(directory.Name))
			{
				dateFolders.Add(directory);
			}
		}

		// Sorting the folders by date, older first in the list
		return dateFolders.OrderBy(d => ParseFolderDate(d.Name)).ToList();
	}

	public static bool IsValidDateFolderName(string folderName)
	{
		// Check if folder name matches "AAAAMMDD" pattern
		if (folderName.Length != 8 || !folderName.All(char.IsDigit))
		{
			return false;
		}

		try
		{
			ParseFolderDate(folderName);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static DateTime ParseFolderDate(string folderName)
	{
		var year = int.Parse(folderName.Substring(0, 4));
		var month = int.Parse(folderName.Substring(4, 2));
		var day = int.Parse(folderName.Substring(6, 2));

		return new DateTime(year, month, day);
	}
}
