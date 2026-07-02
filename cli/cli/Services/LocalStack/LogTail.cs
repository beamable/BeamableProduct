using System.Text;

namespace cli.Services.LocalStack;

/// <summary>
/// Helpers for reading append-only child log files: seek to the last N lines, and follow a growing file
/// line-by-line. Used by <c>beam local up</c> (readiness + <c>--attach</c>) and <c>beam local logs</c>.
/// The line-count seek is ported from #4258 <c>BackendLogCommand.SeekToLastNLines</c>.
/// </summary>
public static class LogTail
{
	/// <summary>Positions <paramref name="fs"/> at the start of the last <paramref name="n"/> lines (or the
	/// beginning of the file if it has fewer). Scans backwards in blocks counting <c>\n</c>.</summary>
	public static void SeekToLastNLines(FileStream fs, int n, int bufferSize = 4096)
	{
		if (n <= 0)
		{
			fs.Seek(0, SeekOrigin.End);
			return;
		}

		var buffer = new byte[bufferSize];
		long filePos = fs.Length;
		var newlinesFound = 0;

		while (filePos > 0)
		{
			var toRead = (int)Math.Min(bufferSize, filePos);
			filePos -= toRead;
			fs.Seek(filePos, SeekOrigin.Begin);
			var read = fs.Read(buffer, 0, toRead);

			for (var i = read - 1; i >= 0; i--)
			{
				if (buffer[i] != (byte)'\n') continue;
				newlinesFound++;
				// The (n+1)-th newline from the end marks the byte just before the last n lines start.
				if (newlinesFound > n)
				{
					fs.Seek(filePos + i + 1, SeekOrigin.Begin);
					return;
				}
			}
		}

		fs.Seek(0, SeekOrigin.Begin);
	}
}

/// <summary>
/// Follows an append-only text file line-by-line. Handles partial (not-yet-terminated) trailing lines by
/// buffering them until their newline arrives. Tolerates the writer holding the file open (shared read).
/// </summary>
public sealed class LineTailer : IDisposable
{
	private readonly FileStream _fs;
	private string _partial = "";

	public LineTailer(string path, int lastNLines)
	{
		_fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		if (lastNLines >= 0)
			LogTail.SeekToLastNLines(_fs, lastNLines);
		// lastNLines < 0 means "from the beginning" (position 0 by default).
	}

	/// <summary>Returns the complete lines that have become available since the last call.</summary>
	public IEnumerable<string> ReadAvailableLines()
	{
		var available = _fs.Length - _fs.Position;
		if (available <= 0) yield break;

		var bytes = new byte[available];
		var read = _fs.Read(bytes, 0, (int)available);
		var text = _partial + Encoding.UTF8.GetString(bytes, 0, read);

		var parts = text.Split('\n');
		// The last element is a partial line unless the chunk ended exactly on a newline (then it is "").
		_partial = parts[^1];
		for (var i = 0; i < parts.Length - 1; i++)
			yield return parts[i].TrimEnd('\r');
	}

	public void Dispose() => _fs.Dispose();
}
