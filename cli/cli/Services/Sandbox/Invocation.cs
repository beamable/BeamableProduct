using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace cli.Services.Sandbox;

public enum InvocationStatusKind
{
	Started,
	Running,
	Completed,
	Failed,
	Cancelled,
}

/// <summary>
/// One CLI invocation initiated via the <c>Invoke</c> route. Owns the bounded output
/// buffer, a cancellation token for graceful cancel, and the lifecycle state machine.
/// Status transitions are one-way (Started → Running → terminal); the terminal states
/// are sticky.
/// </summary>
public sealed class Invocation
{
	public string Id { get; }
	public string CommandLine { get; }
	public DateTimeOffset StartedAt { get; }
	public InvocationBuffer Output { get; }

	private readonly CancellationTokenSource _cts = new();
	private InvocationStatusKind _status = InvocationStatusKind.Started;
	private int? _exitCode;
	private string? _failureReason;

	public CancellationToken CancellationToken => _cts.Token;
	public InvocationStatusKind Status { get { lock (_lock) return _status; } }
	public int? ExitCode { get { lock (_lock) return _exitCode; } }
	public string? FailureReason { get { lock (_lock) return _failureReason; } }
	public bool IsTerminal => _status is InvocationStatusKind.Completed
	                                    or InvocationStatusKind.Failed
	                                    or InvocationStatusKind.Cancelled;

	private readonly object _lock = new();

	public Invocation(string id, string commandLine, DateTimeOffset startedAt, InvocationBuffer? buffer = null)
	{
		if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id must be non-empty", nameof(id));
		Id = id;
		CommandLine = commandLine ?? string.Empty;
		StartedAt = startedAt;
		Output = buffer ?? new InvocationBuffer();
	}

	public bool TryTransition(InvocationStatusKind next, int? exitCode = null, string? failureReason = null)
	{
		lock (_lock)
		{
			if (IsTerminal && next != _status) return false; // terminal states stick
			_status = next;
			if (exitCode.HasValue) _exitCode = exitCode;
			if (failureReason != null) _failureReason = failureReason;
			return true;
		}
	}

	public bool RequestCancel()
	{
		lock (_lock)
		{
			if (IsTerminal) return false;
			_cts.Cancel();
			return true;
		}
	}

	internal void DisposeCts() => _cts.Dispose();
}

/// <summary>
/// Thread-safe registry of in-flight and recently-completed invocations. Old terminal
/// invocations age out so the registry doesn't grow unbounded over a long sandbox run.
/// </summary>
public sealed class InvocationRegistry
{
	public int MaxRetained { get; }
	public TimeSpan RetainTerminalFor { get; }

	private readonly ConcurrentDictionary<string, Invocation> _byId = new();
	private readonly object _lock = new();
	private readonly IClock _clock;

	public InvocationRegistry(IClock clock, int maxRetained = 200, TimeSpan? retainTerminalFor = null)
	{
		_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		MaxRetained = maxRetained;
		RetainTerminalFor = retainTerminalFor ?? TimeSpan.FromMinutes(10);
	}

	public Invocation Start(string commandLine)
	{
		var inv = new Invocation(
			id: Guid.NewGuid().ToString("N"),
			commandLine: commandLine,
			startedAt: _clock.UtcNow);
		_byId[inv.Id] = inv;
		PruneTerminalLocked();
		return inv;
	}

	public Invocation? Get(string id)
	{
		if (string.IsNullOrEmpty(id)) return null;
		_byId.TryGetValue(id, out var inv);
		return inv;
	}

	public IReadOnlyList<Invocation> List()
	{
		// Snapshot — caller-safe even under concurrent mutation.
		var arr = new List<Invocation>(_byId.Count);
		foreach (var kv in _byId) arr.Add(kv.Value);
		return arr;
	}

	public IReadOnlyList<Invocation> ListInflight()
	{
		var arr = new List<Invocation>();
		foreach (var kv in _byId)
		{
			if (!kv.Value.IsTerminal) arr.Add(kv.Value);
		}
		return arr;
	}

	public bool Cancel(string id)
	{
		if (!_byId.TryGetValue(id, out var inv)) return false;
		return inv.RequestCancel();
	}

	private void PruneTerminalLocked()
	{
		// Cheap: only walk if size is over the soft cap.
		if (_byId.Count <= MaxRetained) return;
		var now = _clock.UtcNow;
		foreach (var kv in _byId)
		{
			var inv = kv.Value;
			if (!inv.IsTerminal) continue;
			if (now - inv.StartedAt < RetainTerminalFor) continue;
			if (_byId.TryRemove(kv.Key, out var removed)) removed.DisposeCts();
			if (_byId.Count <= MaxRetained) break;
		}
	}
}
