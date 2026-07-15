using System;
using System.IO;
using System.Runtime.InteropServices;

namespace cli.Services.Sandbox;

/// <summary>
/// Validates that a requested path resolves to a location inside the bound repo root.
/// Handles symlinks (resolved before the containment check) and case-insensitive
/// filesystems (macOS APFS, Windows NTFS) where raw-string comparison is spoofable.
/// </summary>
public sealed class PathContainmentValidator
{
	private readonly string _repoRootCanonical;
	private readonly StringComparison _comparison;

	public PathContainmentValidator(string repoRoot)
	{
		if (string.IsNullOrWhiteSpace(repoRoot))
			throw new ArgumentException("repoRoot must be non-empty", nameof(repoRoot));

		_repoRootCanonical = ResolveRealPath(Path.GetFullPath(repoRoot));
		_comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
	}

	/// <summary>
	/// True when <paramref name="requestedPath"/> (interpreted relative to the repo root if not absolute)
	/// resolves to a location at or beneath the repo root. Sets <paramref name="canonicalPath"/> to the
	/// fully-resolved absolute path either way; callers should only use it when this returns true.
	/// </summary>
	public bool TryCanonicalize(string requestedPath, out string canonicalPath)
	{
		canonicalPath = string.Empty;
		if (string.IsNullOrWhiteSpace(requestedPath)) return false;

		var combined = Path.IsPathRooted(requestedPath)
			? requestedPath
			: Path.Combine(_repoRootCanonical, requestedPath);
		var fullPath = Path.GetFullPath(combined);
		var resolved = ResolveRealPath(fullPath);

		if (!IsUnder(resolved, _repoRootCanonical)) return false;

		canonicalPath = resolved;
		return true;
	}

	private bool IsUnder(string candidate, string root)
	{
		if (candidate.Equals(root, _comparison)) return true;
		var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
		return candidate.StartsWith(rootWithSep, _comparison);
	}

	/// <summary>
	/// Walks path from root to leaf and resolves any symlinks along the way. A resolved
	/// link's target may itself contain unresolved symlinks (e.g. on macOS where
	/// <c>/var/folders</c> is a link to <c>/private/var/folders</c>), so the target is
	/// recursively canonicalized before walking continues. If a segment doesn't exist yet
	/// (e.g., a target file for a write that hasn't happened), the remaining suffix is
	/// appended verbatim — partial resolution is the right behavior for write paths.
	/// </summary>
	private static string ResolveRealPath(string fullPath) => ResolveRealPathCore(fullPath, depth: 0);

	private const int MaxSymlinkDepth = 40;

	private static string ResolveRealPathCore(string fullPath, int depth)
	{
		if (string.IsNullOrEmpty(fullPath)) return fullPath;
		if (depth > MaxSymlinkDepth) return fullPath; // give up on symlink cycles

		var separator = Path.DirectorySeparatorChar;
		var parts = fullPath.Split(separator);
		var current = OperatingSystem.IsWindows()
			? parts[0] + separator
			: separator.ToString();

		for (var i = 1; i < parts.Length; i++)
		{
			var part = parts[i];
			if (string.IsNullOrEmpty(part)) continue;

			var next = Path.Combine(current, part);

			FileSystemInfo? info = Directory.Exists(next)
				? new DirectoryInfo(next)
				: File.Exists(next)
					? new FileInfo(next)
					: null;

			if (info == null)
			{
				// Path tail doesn't exist; append the rest verbatim and stop resolving.
				var remainder = string.Join(separator, parts, i, parts.Length - i);
				return Path.Combine(current, remainder);
			}

			if (info.LinkTarget != null)
			{
				var target = info.ResolveLinkTarget(returnFinalTarget: true);
				if (target != null)
				{
					// The returned target may itself contain unresolved symlink components;
					// recurse to fully canonicalize before continuing the walk.
					current = ResolveRealPathCore(Path.GetFullPath(target.FullName), depth + 1);
				}
				else
				{
					current = next;
				}
			}
			else
			{
				current = next;
			}
		}

		return current;
	}
}
