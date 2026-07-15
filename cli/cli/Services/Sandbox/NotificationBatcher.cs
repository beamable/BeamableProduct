using System;
using System.Collections.Generic;
using System.Linq;

namespace cli.Services.Sandbox;

/// <summary>
/// Accumulates <see cref="SandboxEvent"/>s into a pending batch and produces a flushable
/// <see cref="SandboxBatch"/> at most once per <see cref="BatchWindow"/> (default 1 second).
///
/// This class is pure — it does no scheduling and no I/O. A host loop or timer calls
/// <see cref="TryFlushIfDue"/> on a cadence to drain batches; <see cref="FlushNow"/> exists
/// for the <c>shutdown-imminent</c> case where the next batch must ship before the
/// gateway connection drops.
///
/// Thread-safe. Multiple producers may call <see cref="Enqueue"/> concurrently with a
/// flusher calling <see cref="TryFlushIfDue"/>.
/// </summary>
public sealed class NotificationBatcher
{
	public TimeSpan BatchWindow { get; }

	private readonly object _lock = new();
	private readonly List<SandboxEvent> _pending = new();
	private readonly Dictionary<(string watchId, string path), int> _fileChangedIndex = new();
	private DateTimeOffset? _windowOpenedAt;
	private long _sequence;

	public NotificationBatcher(TimeSpan? batchWindow = null)
	{
		BatchWindow = batchWindow ?? TimeSpan.FromSeconds(1);
	}

	public void Enqueue(SandboxEvent ev, DateTimeOffset now)
	{
		if (ev == null) throw new ArgumentNullException(nameof(ev));

		lock (_lock)
		{
			if (_pending.Count == 0)
			{
				_windowOpenedAt = now;
			}

			if (ev is SandboxEvent.FileChanged fc)
			{
				var key = (fc.WatchId, fc.Path);
				if (_fileChangedIndex.TryGetValue(key, out var existingIdx))
				{
					var existing = (SandboxEvent.FileChanged)_pending[existingIdx];
					var merged = MergeKinds(existing.Kinds, fc.Kinds);
					_pending[existingIdx] = existing with { Kinds = merged };
					return;
				}
				_pending.Add(fc);
				_fileChangedIndex[key] = _pending.Count - 1;
			}
			else
			{
				_pending.Add(ev);
			}
		}
	}

	/// <summary>
	/// Returns a batch if at least <see cref="BatchWindow"/> has elapsed since the first
	/// event of the pending batch was enqueued, or null otherwise. Empty pending batches
	/// always return null — no keepalive ticks.
	/// </summary>
	public SandboxBatch? TryFlushIfDue(DateTimeOffset now)
	{
		lock (_lock)
		{
			if (_pending.Count == 0) return null;
			if (_windowOpenedAt is null) return null;
			if (now - _windowOpenedAt.Value < BatchWindow) return null;

			return FlushPendingLocked(now);
		}
	}

	/// <summary>
	/// Force-flushes the pending batch regardless of the window. Returns null if there is
	/// nothing pending. Used for <c>shutdown-imminent</c>, which must ship before the
	/// websocket drops.
	/// </summary>
	public SandboxBatch? FlushNow(DateTimeOffset now)
	{
		lock (_lock)
		{
			if (_pending.Count == 0) return null;
			return FlushPendingLocked(now);
		}
	}

	private SandboxBatch FlushPendingLocked(DateTimeOffset now)
	{
		var batch = new SandboxBatch
		{
			Sequence = ++_sequence,
			EmittedAt = now,
			Events = _pending.ToArray(),
		};
		_pending.Clear();
		_fileChangedIndex.Clear();
		_windowOpenedAt = null;
		return batch;
	}

	private static IReadOnlyList<string> MergeKinds(IReadOnlyList<string> existing, IReadOnlyList<string> incoming)
	{
		// Preserve order of first occurrence so consumers see a stable sequence; dedupe by
		// case-sensitive string equality (kinds are a closed set: "created", "changed",
		// "deleted", "renamed").
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var merged = new List<string>(existing.Count + incoming.Count);
		foreach (var k in existing)
		{
			if (seen.Add(k)) merged.Add(k);
		}
		foreach (var k in incoming)
		{
			if (seen.Add(k)) merged.Add(k);
		}
		return merged;
	}
}
