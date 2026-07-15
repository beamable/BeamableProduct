using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.Sandbox;

/// <summary>
/// File operations against a containment-validated path. Callers (the observer) must
/// pre-validate paths via <see cref="PathContainmentValidator.TryCanonicalize"/>; this
/// class trusts that what it receives is already inside the repo root.
///
/// All text operations are UTF-8. Binary formats are explicitly out of scope for v1
/// (see design doc, File operations / "Text only").
/// </summary>
public sealed class SandboxFileService
{
	/// <summary>5 MB cap on unranged ReadFile. Ranged reads are bounded by the range.</summary>
	public const int DefaultReadCapBytes = 5 * 1024 * 1024;

	private static readonly HashSet<string> DefaultSkipDirs = new(StringComparer.OrdinalIgnoreCase)
	{
		"node_modules", ".git", "bin", "obj",
	};

	public DirListing ListDir(string canonicalPath, bool showHidden)
	{
		if (!Directory.Exists(canonicalPath))
		{
			throw new SandboxFileException("not-found", $"Directory does not exist: {canonicalPath}");
		}

		var entries = new List<DirEntry>();
		foreach (var path in Directory.EnumerateFileSystemEntries(canonicalPath))
		{
			var name = Path.GetFileName(path);
			if (!showHidden && name.StartsWith('.')) continue;

			var isDir = Directory.Exists(path);
			if (isDir && DefaultSkipDirs.Contains(name)) continue;

			long size = 0;
			long modifiedMs = 0;
			try
			{
				if (isDir)
				{
					var di = new DirectoryInfo(path);
					modifiedMs = new DateTimeOffset(di.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds();
				}
				else
				{
					var fi = new FileInfo(path);
					size = fi.Length;
					modifiedMs = new DateTimeOffset(fi.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds();
				}
			}
			catch (IOException)
			{
				// Permission denied or file deleted between enumerate and stat — skip rather than fail the whole listing.
				continue;
			}

			entries.Add(new DirEntry
			{
				Name = name,
				Kind = isDir ? "dir" : "file",
				Size = size,
				ModifiedAtUnixMs = modifiedMs,
			});
		}

		return new DirListing
		{
			Entries = entries
				.OrderByDescending(e => e.Kind == "dir")
				.ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
				.ToArray(),
		};
	}

	public FileStat Stat(string canonicalPath)
	{
		if (Directory.Exists(canonicalPath))
		{
			var di = new DirectoryInfo(canonicalPath);
			return new FileStat
			{
				Kind = "dir",
				Size = 0,
				ModifiedAtUnixMs = new DateTimeOffset(di.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds(),
				ContentHash = null, // hashing a dir doesn't make sense; null marks it as inapplicable
			};
		}
		if (File.Exists(canonicalPath))
		{
			var fi = new FileInfo(canonicalPath);
			return new FileStat
			{
				Kind = "file",
				Size = fi.Length,
				ModifiedAtUnixMs = new DateTimeOffset(fi.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds(),
				ContentHash = ComputeFileHash(canonicalPath),
			};
		}
		throw new SandboxFileException("not-found", $"Path does not exist: {canonicalPath}");
	}

	public FileReadResult Read(string canonicalPath, long? rangeStart, long? rangeEnd)
	{
		if (!File.Exists(canonicalPath))
		{
			throw new SandboxFileException("not-found", $"File does not exist: {canonicalPath}");
		}

		var fi = new FileInfo(canonicalPath);
		long start = rangeStart ?? 0;
		long endExclusive = rangeEnd ?? fi.Length;
		if (start < 0) start = 0;
		if (endExclusive > fi.Length) endExclusive = fi.Length;
		if (start > endExclusive) start = endExclusive;

		var bytesRequested = endExclusive - start;
		bool isRangedRequest = rangeStart.HasValue || rangeEnd.HasValue;
		if (!isRangedRequest && bytesRequested > DefaultReadCapBytes)
		{
			throw new SandboxFileException(
				"too-large",
				$"File is {fi.Length} bytes; pass a range or stay under the {DefaultReadCapBytes}-byte cap.");
		}

		string text;
		using (var stream = new FileStream(canonicalPath, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			stream.Seek(start, SeekOrigin.Begin);
			var buffer = new byte[bytesRequested];
			var read = stream.Read(buffer, 0, buffer.Length);
			text = Encoding.UTF8.GetString(buffer, 0, read);
		}

		return new FileReadResult
		{
			Contents = text,
			Size = fi.Length,
			ModifiedAtUnixMs = new DateTimeOffset(fi.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds(),
			ContentHash = ComputeFileHash(canonicalPath),
		};
	}

	public FileWriteResult Write(string canonicalPath, string contents, string? expectedContentHash)
	{
		// Conflict probe: if the caller passed expectedContentHash, the current file's hash
		// must match. mtime-based conflict detection is unsafe (1-second resolution on some
		// filesystems); SHA-256 is the only reliable answer.
		if (expectedContentHash != null && File.Exists(canonicalPath))
		{
			var currentHash = ComputeFileHash(canonicalPath);
			if (!string.Equals(currentHash, expectedContentHash, StringComparison.OrdinalIgnoreCase))
			{
				throw new SandboxFileConflictException(currentHash);
			}
		}

		var parent = Path.GetDirectoryName(canonicalPath);
		if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

		// Atomic write: write to a temp sibling, then move-with-overwrite. On crash mid-write
		// the original file is intact; the temp file is orphaned but never visible at the
		// target path.
		var tempPath = canonicalPath + ".tmp." + Guid.NewGuid().ToString("N");
		try
		{
			File.WriteAllText(tempPath, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			File.Move(tempPath, canonicalPath, overwrite: true);
		}
		catch
		{
			// best-effort cleanup if the move failed
			try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
			throw;
		}

		var fi = new FileInfo(canonicalPath);
		return new FileWriteResult
		{
			Size = fi.Length,
			ModifiedAtUnixMs = new DateTimeOffset(fi.LastWriteTimeUtc, TimeSpan.Zero).ToUnixTimeMilliseconds(),
			ContentHash = ComputeFileHash(canonicalPath),
		};
	}

	public void Delete(string canonicalPath)
	{
		if (File.Exists(canonicalPath))
		{
			File.Delete(canonicalPath);
			return;
		}
		if (Directory.Exists(canonicalPath))
		{
			throw new SandboxFileException(
				"is-directory",
				"DeleteFile refuses to remove a directory. Use a future RemoveDir route for that.");
		}
		throw new SandboxFileException("not-found", $"Path does not exist: {canonicalPath}");
	}

	public void Rename(string canonicalFrom, string canonicalTo)
	{
		if (!File.Exists(canonicalFrom) && !Directory.Exists(canonicalFrom))
		{
			throw new SandboxFileException("not-found", $"Source does not exist: {canonicalFrom}");
		}
		var parent = Path.GetDirectoryName(canonicalTo);
		if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

		if (File.Exists(canonicalFrom))
		{
			File.Move(canonicalFrom, canonicalTo);
		}
		else
		{
			Directory.Move(canonicalFrom, canonicalTo);
		}
	}

	public void MakeDir(string canonicalPath)
	{
		// CreateDirectory is recursive and idempotent — exactly the contract the design spec
		// promises ("Recursive (creates intermediate dirs)").
		Directory.CreateDirectory(canonicalPath);
	}

	public static string ComputeFileHash(string path)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var sha = SHA256.Create();
		var hash = sha.ComputeHash(stream);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}
}

public sealed class DirEntry
{
	public string Name { get; init; } = string.Empty;
	public string Kind { get; init; } = "file";
	public long Size { get; init; }
	public long ModifiedAtUnixMs { get; init; }
}

public sealed class DirListing
{
	public DirEntry[] Entries { get; init; } = Array.Empty<DirEntry>();
}

public sealed class FileStat
{
	public string Kind { get; init; } = "file";
	public long Size { get; init; }
	public long ModifiedAtUnixMs { get; init; }
	public string? ContentHash { get; init; }
}

public sealed class FileReadResult
{
	public string Contents { get; init; } = string.Empty;
	public long Size { get; init; }
	public long ModifiedAtUnixMs { get; init; }
	public string? ContentHash { get; init; }
}

public sealed class FileWriteResult
{
	public long Size { get; init; }
	public long ModifiedAtUnixMs { get; init; }
	public string? ContentHash { get; init; }
}

public class SandboxFileException : Exception
{
	public string Code { get; }
	public SandboxFileException(string code, string message) : base(message) { Code = code; }
}

public sealed class SandboxFileConflictException : SandboxFileException
{
	public string CurrentContentHash { get; }
	public SandboxFileConflictException(string currentHash)
		: base("conflict", $"File content hash mismatch; current is {currentHash}")
	{
		CurrentContentHash = currentHash;
	}
}
