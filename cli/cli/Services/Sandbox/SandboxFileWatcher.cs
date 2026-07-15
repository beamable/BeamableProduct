using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Beamable.Server;

namespace cli.Services.Sandbox;

/// <summary>
/// Owns <see cref="FileSystemWatcher"/> lifetimes for a sandbox. Each <c>WatchPaths</c>
/// call registers a watcher per supplied directory path; the watcher's events are
/// translated into <see cref="SandboxEvent.FileChanged"/> and pushed into the observer's
/// batcher via the supplied callback after a short tail-debounce.
///
/// <para>The debounce is the second line of defense against editor save-storms. The
/// raw <see cref="FileSystemWatcher"/> fires 2–5 events per logical save (atomic-rename
/// dance + LastWrite/CreationTime updates that arrive simultaneously on some OSes).
/// The batcher collapses by (watchId, path) inside its window, but events that span
/// the window boundary produce two batches. By coalescing per path for a small
/// window before forwarding, a single logical edit always ends up in a single batch.</para>
///
/// <para>Lifetime: a watch handle dies on explicit <c>UnwatchPaths</c>, on
/// <see cref="Dispose"/> (called by host shutdown), or when its enclosing session
/// ends. Watchers are not coalesced across calls — each <c>WatchPaths</c> produces
/// a distinct <c>watchId</c>.</para>
/// </summary>
public sealed class SandboxFileWatcher : IDisposable
{
	// How long to wait after the last FS event for a path before forwarding.
	// 250ms is the standard "atomic rename + metadata follow-up" window; well
	// under the batcher's 1 s and well over any single editor's save bounce.
	private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(250);
	private static readonly TimeSpan DebounceTick = TimeSpan.FromMilliseconds(100);

	private readonly object _lock = new();
	private readonly Dictionary<string, WatchHandle> _watchers = new();
	private readonly Action<SandboxEvent.FileChanged> _onChange;

	// Per-path debounce buckets keyed by full path. Each entry collects kinds
	// and resets its deadline on every new event; the timer drains entries
	// whose deadline has passed.
	private readonly Dictionary<string, DebouncedChange> _pending = new();
	private readonly Timer _debounceTimer;
	private int _disposed;

	public SandboxFileWatcher(Action<SandboxEvent.FileChanged> onChange)
	{
		_onChange = onChange ?? throw new ArgumentNullException(nameof(onChange));
		_debounceTimer = new Timer(OnDebounceTick, null, DebounceTick, DebounceTick);
	}

	/// <summary>
	/// Begin watching one or more canonical (containment-validated) directory paths.
	/// Returns a stable watchId the caller can use to stop watching.
	/// </summary>
	public string Watch(IReadOnlyList<string> canonicalPaths)
	{
		if (canonicalPaths == null || canonicalPaths.Count == 0)
		{
			throw new ArgumentException("at least one path is required", nameof(canonicalPaths));
		}

		var watchId = Guid.NewGuid().ToString("N");
		var fsws = new List<FileSystemWatcher>(canonicalPaths.Count);
		var actualPaths = new List<string>(canonicalPaths.Count);

		foreach (var path in canonicalPaths)
		{
			if (!Directory.Exists(path))
			{
				SandboxLog.Warn($"[sandbox-watch] skip missing path: {path}");
				continue;
			}
			var w = BuildWatcher(path, watchId);
			fsws.Add(w);
			actualPaths.Add(path);
		}

		lock (_lock)
		{
			_watchers[watchId] = new WatchHandle(fsws);
		}
		SandboxLog.Info($"[sandbox-watch] started watchId={watchId.Substring(0, 8)} paths=[{string.Join(", ", actualPaths)}] (subdirs=true)");
		return watchId;
	}

	public bool Unwatch(string watchId)
	{
		WatchHandle? handle;
		List<SandboxEvent.FileChanged> orphaned;
		lock (_lock)
		{
			if (!_watchers.TryGetValue(watchId, out handle)) return false;
			_watchers.Remove(watchId);

			// Drain any pending debounced events for this watchId so we don't
			// silently drop edits that arrived in the last few hundred ms.
			orphaned = DrainPendingForWatchIdLocked(watchId);
		}
		handle.Dispose();
		FlushOutside(orphaned);
		return true;
	}

	public int ActiveWatchCount
	{
		get { lock (_lock) return _watchers.Count; }
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
		_debounceTimer?.Dispose();

		List<WatchHandle> snapshot;
		List<SandboxEvent.FileChanged> remaining;
		lock (_lock)
		{
			snapshot = new List<WatchHandle>(_watchers.Values);
			_watchers.Clear();
			// Flush everything still pending. Better one duplicate-looking event
			// at shutdown than a silently-dropped edit.
			remaining = DrainAllPendingLocked();
		}
		foreach (var h in snapshot) h.Dispose();
		FlushOutside(remaining);
	}

	private FileSystemWatcher BuildWatcher(string root, string watchId)
	{
		var w = new FileSystemWatcher(root)
		{
			IncludeSubdirectories = true,
			// CreationTime intentionally NOT in the filter set: it fires alongside
			// LastWrite on macOS for any save, doubling the event rate for zero
			// signal gain. LastWrite + FileName/DirectoryName are enough to
			// observe content edits, creates, deletes, and renames.
			NotifyFilter = NotifyFilters.LastWrite
			               | NotifyFilters.FileName
			               | NotifyFilters.DirectoryName,
		};

		void Forward(string kind, string fullPath)
		{
			if (IsIgnorablePath(fullPath))
			{
				// Editor temp files and OS noise — never surface to Portal.
				return;
			}

			SandboxLog.Info($"[sandbox-watch] fs event watchId={watchId.Substring(0, 8)} kind={kind} path={fullPath}");
			EnqueueDebounced(watchId, kind, fullPath);
		}

		w.Created += (_, e) => Forward("created", e.FullPath);
		w.Changed += (_, e) => Forward("changed", e.FullPath);
		w.Deleted += (_, e) => Forward("deleted", e.FullPath);
		w.Renamed += (_, e) => Forward("renamed", e.FullPath);
		w.Error   += (_, e) => SandboxLog.ErrorMsg($"[sandbox-watch] FileSystemWatcher error watchId={watchId.Substring(0, 8)}: {e.GetException()?.Message}");

		w.EnableRaisingEvents = true;
		return w;
	}

	private void EnqueueDebounced(string watchId, string kind, string fullPath)
	{
		var now = DateTime.UtcNow;
		lock (_lock)
		{
			if (_pending.TryGetValue(fullPath, out var entry))
			{
				entry.Kinds.Add(kind);
				entry.LastSeenUtc = now;
				// WatchId stays — first writer wins. Cross-watch collisions on the
				// same path inside the debounce window are vanishingly rare.
			}
			else
			{
				_pending[fullPath] = new DebouncedChange(watchId, kind, now);
			}
		}
	}

	private void OnDebounceTick(object? state)
	{
		List<SandboxEvent.FileChanged>? toFlush = null;
		var now = DateTime.UtcNow;
		lock (_lock)
		{
			if (_pending.Count == 0) return;
			foreach (var kv in _pending)
			{
				if (now - kv.Value.LastSeenUtc < DebounceWindow) continue;
				toFlush ??= new List<SandboxEvent.FileChanged>();
				toFlush.Add(new SandboxEvent.FileChanged(
					kv.Value.WatchId, kv.Key, kv.Value.Kinds.ToArray()));
			}
			if (toFlush != null)
			{
				foreach (var fc in toFlush) _pending.Remove(fc.Path);
			}
		}
		FlushOutside(toFlush);
	}

	private List<SandboxEvent.FileChanged> DrainPendingForWatchIdLocked(string watchId)
	{
		var keys = _pending.Where(kv => kv.Value.WatchId == watchId).Select(kv => kv.Key).ToList();
		var drained = new List<SandboxEvent.FileChanged>(keys.Count);
		foreach (var path in keys)
		{
			var entry = _pending[path];
			drained.Add(new SandboxEvent.FileChanged(entry.WatchId, path, entry.Kinds.ToArray()));
			_pending.Remove(path);
		}
		return drained;
	}

	private List<SandboxEvent.FileChanged> DrainAllPendingLocked()
	{
		var drained = new List<SandboxEvent.FileChanged>(_pending.Count);
		foreach (var kv in _pending)
		{
			drained.Add(new SandboxEvent.FileChanged(kv.Value.WatchId, kv.Key, kv.Value.Kinds.ToArray()));
		}
		_pending.Clear();
		return drained;
	}

	private void FlushOutside(List<SandboxEvent.FileChanged>? events)
	{
		if (events == null || events.Count == 0) return;
		foreach (var ev in events)
		{
			try { _onChange(ev); }
			catch (Exception ex) { SandboxLog.Warn($"[sandbox-watch] callback threw: {ex.Message}"); }
		}
	}

	// ── Temp / noise filter ────────────────────────────────────────────────────────

	private static readonly string[] IgnoredSuffixes =
	{
		".tmp", ".temp", ".swp", ".swx", ".swo", ".bak", ".orig",
		".crdownload", ".part", ".lock",
	};

	private static readonly string[] IgnoredPrefixes =
	{
		"~$",                 // Microsoft Office lock files
		".#",                 // Emacs lock files
		".goutputstream-",    // GIO atomic write
		".__atomicWrite",     // some editors' atomic-write scratch
	};

	private static readonly string[] IgnoredExactNames =
	{
		".DS_Store",
		"Thumbs.db",
	};

	private static bool IsIgnorablePath(string fullPath)
	{
		var name = Path.GetFileName(fullPath);
		if (string.IsNullOrEmpty(name)) return false;

		foreach (var exact in IgnoredExactNames)
		{
			if (string.Equals(name, exact, StringComparison.OrdinalIgnoreCase)) return true;
		}

		// Emacs writes "foo~" backups.
		if (name.EndsWith("~", StringComparison.Ordinal)) return true;

		foreach (var suffix in IgnoredSuffixes)
		{
			if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
		}

		foreach (var prefix in IgnoredPrefixes)
		{
			if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
		}

		// .git internals fire a torrent of events on every commit/branch op and
		// aren't meaningful to a Portal-side watcher.
		if (fullPath.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			return true;

		return false;
	}

	private sealed class DebouncedChange
	{
		public DebouncedChange(string watchId, string kind, DateTime now)
		{
			WatchId = watchId;
			Kinds = new HashSet<string>(StringComparer.Ordinal) { kind };
			LastSeenUtc = now;
		}

		public string WatchId { get; }
		public HashSet<string> Kinds { get; }
		public DateTime LastSeenUtc { get; set; }
	}

	private sealed class WatchHandle : IDisposable
	{
		private readonly List<FileSystemWatcher> _watchers;
		public WatchHandle(List<FileSystemWatcher> watchers) => _watchers = watchers;
		public void Dispose()
		{
			foreach (var w in _watchers)
			{
				try { w.EnableRaisingEvents = false; w.Dispose(); } catch { /* best-effort */ }
			}
		}
	}
}
