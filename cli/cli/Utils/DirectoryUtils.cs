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
		long size = 0;
		int fileCount = 0;

		var options = new EnumerationOptions
		{
			RecurseSubdirectories = true,
			IgnoreInaccessible = true,          // skip permission errors silently
			AttributesToSkip = FileAttributes.ReparsePoint  // skip symlinks/junctions
		};

		foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", options))
		{
			try
			{
				size += file.Length;   // Length is pre-populated on the FileInfo
				fileCount++;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Could not access file {file.FullName}: {ex.Message}");
			}
		}

		return new DirectoryInfoUtils { Size = size, FileCount = fileCount };
	}

	/// <summary>
	/// Holds the project-related files discovered by <see cref="ScanProjectFiles"/>, bucketed by kind.
	/// </summary>
	public sealed class ProjectScanResult
	{
		public List<string> Csprojs = new();
		public List<string> BeamIgnores = new();
		public List<string> PackageJsons = new();
	}

	/// <summary>
	/// Directory names that never legitimately contain Beamable source projects (.csproj / package.json)
	/// and are pruned during <see cref="ScanProjectFiles"/> traversal so we don't descend into them.
	/// </summary>
	private static readonly HashSet<string> PrunedDirNames =
		new(StringComparer.OrdinalIgnoreCase)
		{
			"bin", "obj", ".git", "node_modules", "Library", "Temp", "Logs", ".vs", ".idea", ".beamable"
		};

	/// <summary>
	/// Walks each search path's directory tree exactly once, pruning well-known non-source directories
	/// (see <see cref="PrunedDirNames"/>) and any subtree under <paramref name="absolutePathsToIgnore"/>
	/// before descending, and buckets the matched files (*.csproj, *.beamignore, package.json).
	/// This replaces three independent recursive <see cref="Directory.GetFiles(string,string,SearchOption)"/>
	/// walks that previously traversed (and post-filtered) the entire workspace per file kind.
	/// </summary>
	public static ProjectScanResult ScanProjectFiles(
		IReadOnlyList<string> searchPaths,
		IReadOnlyList<string> absolutePathsToIgnore)
	{
		var result = new ProjectScanResult();

		// normalize the ignore prefixes to full paths for a stable StartsWith comparison.
		var ignorePrefixes = new string[absolutePathsToIgnore?.Count ?? 0];
		for (var i = 0; i < ignorePrefixes.Length; i++)
		{
			ignorePrefixes[i] = Path.GetFullPath(absolutePathsToIgnore[i]);
		}

		// guard against overlapping search paths and symlink loops by tracking visited directories.
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var stack = new Stack<string>();
		foreach (var searchPath in searchPaths)
		{
			if (string.IsNullOrEmpty(searchPath) || !Directory.Exists(searchPath)) continue;
			stack.Push(Path.GetFullPath(searchPath));
		}

		bool IsIgnored(string fullPath)
		{
			foreach (var prefix in ignorePrefixes)
			{
				if (fullPath.StartsWith(prefix, StringComparison.Ordinal)) return true;
			}
			return false;
		}

		while (stack.Count > 0)
		{
			var dir = stack.Pop();
			if (!visited.Add(dir)) continue;
			if (IsIgnored(dir)) continue;

			IEnumerable<FileSystemInfo> entries;
			try
			{
				entries = new DirectoryInfo(dir).EnumerateFileSystemInfos();
			}
			catch
			{
				// IgnoreInaccessible-equivalent: skip directories we cannot read.
				continue;
			}

			foreach (var entry in entries)
			{
				// skip symlinks/junctions to avoid loops, mirroring CalculateDirectorySize.
				if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue;

				if ((entry.Attributes & FileAttributes.Directory) != 0)
				{
					if (PrunedDirNames.Contains(entry.Name)) continue;
					stack.Push(entry.FullName);
					continue;
				}

				if (string.Equals(entry.Name, "package.json", StringComparison.OrdinalIgnoreCase))
				{
					result.PackageJsons.Add(entry.FullName);
				}
				else if (string.Equals(entry.Extension, ".csproj", StringComparison.OrdinalIgnoreCase))
				{
					result.Csprojs.Add(entry.FullName);
				}
				else if (string.Equals(entry.Extension, ".beamignore", StringComparison.OrdinalIgnoreCase))
				{
					result.BeamIgnores.Add(entry.FullName);
				}
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
