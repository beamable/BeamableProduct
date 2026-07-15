using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Server;
using Beamable.Server.Api.Notifications;

namespace cli.Services.Sandbox;

/// <summary>
/// Holds the in-memory state of a running sandbox: launcher identity, repo binding,
/// pairing state, sessions, and a flush signal for the host's shutdown loop. The
/// observer is the single source of truth shared between the MS route handlers
/// (<see cref="SandboxDiscoveryService"/>) and the CLI host that launched it.
///
/// The route logic that's worth testing — auth checks, code verification, session
/// minting, shutdown signaling — all lives here as plain methods. The route class
/// is a thin façade that forwards to these methods with the gateway-authed
/// <c>RequestContext.AccountId</c>.
/// </summary>
public sealed class SandboxObserver : IDisposable
{
	private readonly object _lock = new();
	private readonly Dictionary<string, SandboxSession> _sessions = new();
	private readonly List<SandboxConnectionEvent> _connectionHistory = new();
	private readonly JoinCodeRateLimiter _rateLimiter;
	private readonly IClock _clock;
	private readonly TaskCompletionSource<ShutdownReason> _shutdownTcs = new();
	// Idempotency gate for SignalShutdown so the goodbye event + flush only
	// run once even if multiple paths trip (Ctrl-C racing the RPC, etc.).
	private int _goodbySignaled;
	private readonly InvocationRegistry _invocations;
	private readonly NotificationBatcher _batcher;
	private readonly PathContainmentValidator _pathValidator;
	private readonly SandboxFileService _fileService;
	private readonly SandboxFileWatcher _fileWatcher;
	private IInvocationRunner? _runner;
	private IInvocationRunner? _outOfProcessRunner;
	private IMicroserviceNotificationsApi? _notifications;

	public long LauncherAccountId { get; }
	public string Cid { get; }
	public string Pid { get; }
	public string ServiceName { get; }
	public string RepoRoot { get; }
	public string JoinCode { get; }
	public DateTimeOffset StartedAt { get; }
	public string SandboxVersion { get; }
	public string? Label { get; }

	/// <summary>
	/// Snapshot of the CLI command surface this sandbox process exposes — handed
	/// in at startup so the <c>GetSchema</c> route can serve it without
	/// re-walking reflection on every call. Null until <see cref="BindSchema"/>
	/// is called (tests skip it; the start command sets it before the MS launches).
	/// </summary>
	public CliSchema? Schema { get; private set; }

	public Task<ShutdownReason> ShutdownSignal => _shutdownTcs.Task;

	public IReadOnlyList<SandboxConnectionEvent> ConnectionHistory
	{
		get { lock (_lock) return _connectionHistory.ToArray(); }
	}

	public SandboxObserver(
		long launcherAccountId,
		string cid,
		string pid,
		string serviceName,
		string repoRoot,
		string joinCode,
		string sandboxVersion,
		string? label,
		IClock clock,
		JoinCodeRateLimiter? rateLimiter = null)
	{
		if (launcherAccountId <= 0) throw new ArgumentException("launcher account id must be positive", nameof(launcherAccountId));
		LauncherAccountId = launcherAccountId;
		Cid = cid ?? throw new ArgumentNullException(nameof(cid));
		Pid = pid ?? throw new ArgumentNullException(nameof(pid));
		ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
		RepoRoot = repoRoot ?? throw new ArgumentNullException(nameof(repoRoot));
		JoinCode = joinCode ?? throw new ArgumentNullException(nameof(joinCode));
		SandboxVersion = sandboxVersion ?? throw new ArgumentNullException(nameof(sandboxVersion));
		Label = label;
		_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		_rateLimiter = rateLimiter ?? new JoinCodeRateLimiter(clock);
		_invocations = new InvocationRegistry(clock);
		_batcher = new NotificationBatcher();
		_pathValidator = new PathContainmentValidator(repoRoot);
		_fileService = new SandboxFileService();
		// Watcher pushes change events directly into the batcher so the 1 Hz throttle
		// absorbs editor save-storms.
		_fileWatcher = new SandboxFileWatcher(fc => _batcher.Enqueue(fc, _clock.UtcNow));
		StartedAt = clock.UtcNow;
	}

	public InvocationRegistry Invocations => _invocations;
	public NotificationBatcher Batcher => _batcher;

	/// <summary>
	/// Wires the in-process runner that will execute invocations by default.
	/// The host (<see cref="cli.Services.BeamoLocalSystem.RunLocalSandbox"/>)
	/// owns the runner choice — production wiring uses a real CLI runner, tests
	/// pass a fake.
	/// </summary>
	public void BindRunner(IInvocationRunner runner) => _runner = runner;

	/// <summary>
	/// Wires the out-of-process runner used when <c>Invoke</c> is called with
	/// <c>outOfProcess: true</c>. Optional — if never bound, an out-of-process
	/// invocation request falls back with <see cref="InvokeOutcome.RunnerUnavailable"/>.
	/// </summary>
	public void BindOutOfProcessRunner(IInvocationRunner runner) => _outOfProcessRunner = runner;

	/// <summary>
	/// Captures the CLI schema this sandbox exposes. Called by
	/// <see cref="cli.Sandbox.SandboxStartCommand"/> after walking the command
	/// tree, so the <c>GetSchema</c> route can return it without redoing the work.
	/// </summary>
	public void BindSchema(CliSchema schema) => Schema = schema;

	/// <summary>
	/// Wires the notifications API so the host's flush loop can ship batches over the
	/// platform websocket. Available once the framework's <c>InitializeServices</c> callback
	/// has built the provider.
	/// </summary>
	public void BindNotifications(IMicroserviceNotificationsApi notifications)
	{
		_notifications = notifications;
		SandboxLog.Info($"[sandbox-batch] notifications API bound (channel=sandbox-events)");
	}

	/// <summary>
	/// Pair handler. Gateway auth has already established <paramref name="requestAccountId"/>;
	/// this method checks the account match, rate-limit state, and join code, then mints
	/// a session and returns the per-session secret. The secret never touches disk.
	/// </summary>
	public PairOutcome Pair(long requestAccountId, string suppliedCode)
	{
		if (requestAccountId != LauncherAccountId) return PairOutcome.AccountMismatch();
		if (!_rateLimiter.TryBeginAttempt()) return PairOutcome.RateLimited();

		// Constant-time comparison so attempting all 8-char codes against this route can't
		// be sped up via timing on individual characters.
		if (!ConstantTimeStringEquals(suppliedCode ?? string.Empty, JoinCode))
		{
			_rateLimiter.RecordFailure();
			return PairOutcome.WrongCode();
		}

		_rateLimiter.RecordSuccess();

		var sessionId = Guid.NewGuid().ToString("N");
		var sessionSecret = new byte[32];
		RandomNumberGenerator.Fill(sessionSecret);

		var session = new SandboxSession(sessionId, sessionSecret, _clock.UtcNow);
		lock (_lock)
		{
			_sessions[sessionId] = session;
			_connectionHistory.Add(new SandboxConnectionEvent(
				kind: SandboxConnectionEventKind.Paired,
				sessionId: sessionId,
				at: _clock.UtcNow));
		}

		return PairOutcome.Success(session);
	}

	/// <summary>
	/// Info handler. Returns the metadata the Sandbox Overview page renders. No HMAC required
	/// so Portal can populate the dropdown pre-pair; the response never includes the join code
	/// or any session secret.
	/// </summary>
	public InfoOutcome GetInfo(long requestAccountId)
	{
		if (requestAccountId != LauncherAccountId) return InfoOutcome.AccountMismatch();

		lock (_lock)
		{
			return InfoOutcome.Success(new SandboxInfoSnapshot
			{
				Name = ServiceName,
				Label = Label,
				RepoRoot = RepoRoot,
				Cid = Cid,
				Pid = Pid,
				StartedAt = StartedAt,
				SandboxVersion = SandboxVersion,
				ConnectionHistory = _connectionHistory.Select(e => e.Clone()).ToArray(),
				ActiveSessionCount = _sessions.Count,
			});
		}
	}

	/// <summary>
	/// Shutdown handler. Per the Auth model open question, v1 uses account-match alone
	/// (no HMAC) — this lets a fresh <c>beam sandbox start</c> on another machine kill the
	/// old sandbox during deposition without needing the old session's secret. Any same-account
	/// session can kill the sandbox; that's a self-DoS, not RCE.
	/// </summary>
	public ShutdownOutcome Shutdown(long requestAccountId, ShutdownReason reason)
	{
		if (requestAccountId != LauncherAccountId) return ShutdownOutcome.AccountMismatch();
		SignalShutdown(reason);
		return ShutdownOutcome.Success();
	}

	/// <summary>
	/// Internal shutdown trigger that bypasses the account check. Used by the host
	/// process (Ctrl-C / SIGTERM) where the "request account" concept doesn't apply.
	/// Emits a <c>ShutdownImminent</c> notification + flushes the batcher immediately
	/// so the Portal sees the goodbye before the gateway connection drops.
	/// </summary>
	public void SignalShutdown(ShutdownReason reason)
	{
		// First caller wins. Using a separate flag (not _shutdownTcs.TrySetResult)
		// so the goodbye notification ships *before* anyone awaiting ShutdownSignal
		// resumes — that consumer is the host loop, which tears down the gateway
		// connection on resume.
		if (Interlocked.Exchange(ref _goodbySignaled, 1) == 1) return;

		lock (_lock)
		{
			_connectionHistory.Add(new SandboxConnectionEvent(
				kind: SandboxConnectionEventKind.Shutdown,
				sessionId: null,
				at: _clock.UtcNow));
		}

		_batcher.Enqueue(
			new SandboxEvent.ShutdownImminent(reason.ToString()),
			_clock.UtcNow);
		// Force a flush right now so the wire frame leaves the process before the
		// host loop tears down the gateway connection. NotificationBatcher.FlushNow
		// ignores the normal cadence throttle for exactly this case.
		FlushPending();

		_shutdownTcs.TrySetResult(reason);
	}

	internal bool TryGetSession(string sessionId, out SandboxSession session)
	{
		lock (_lock)
		{
			if (sessionId != null && _sessions.TryGetValue(sessionId, out var s))
			{
				session = s;
				return true;
			}
			session = null!;
			return false;
		}
	}

	// --- Invocation API ----------------------------------------------------------------

	public InvokeOutcome Invoke(long requestAccountId, string commandLine, bool outOfProcess = false)
	{
		if (requestAccountId != LauncherAccountId) return InvokeOutcome.AccountMismatch();
		if (string.IsNullOrWhiteSpace(commandLine)) return InvokeOutcome.MalformedRequest();

		// Per-call runner choice. Default: in-process — cheaper, no spawn
		// overhead, fine for fast read-only / metadata commands. Opt-in: out
		// of process — for commands that bind ports / spawn user services and
		// would otherwise collide with the sandbox's own HttpListener.
		var runner = outOfProcess ? _outOfProcessRunner : _runner;
		if (runner == null) return InvokeOutcome.RunnerUnavailable();

		var invocation = _invocations.Start(commandLine);
		var sink = new InvocationSink(this, invocation.Id);

		// Fire-and-forget: the runner emits status/output via the sink; the registry
		// captures terminal state when the runner finishes.
		_ = Task.Run(async () =>
		{
			try
			{
				sink.EmitStatus(InvocationStatusKind.Running);
				await runner.RunAsync(invocation, sink, invocation.CancellationToken);
				if (!invocation.IsTerminal)
				{
					// Runner returned without explicitly signaling a terminal state — treat
					// as a successful completion with the unknown exit code.
					sink.EmitStatus(InvocationStatusKind.Completed, exitCode: 0);
				}
			}
			catch (OperationCanceledException)
			{
				sink.EmitStatus(InvocationStatusKind.Cancelled);
			}
			catch (Exception ex)
			{
				sink.EmitStatus(InvocationStatusKind.Failed, failureReason: ex.Message);
			}
		});

		return InvokeOutcome.Success(invocation.Id);
	}

	public ListInvocationsOutcome ListInvocations(long requestAccountId)
	{
		if (requestAccountId != LauncherAccountId) return ListInvocationsOutcome.AccountMismatch();
		var snapshot = _invocations.List().Select(i => new InvocationSummary
		{
			Id = i.Id,
			CommandLine = i.CommandLine,
			Status = i.Status.ToString().ToLowerInvariant(),
			ExitCode = i.ExitCode,
			StartedAtUnixMs = i.StartedAt.ToUnixTimeMilliseconds(),
			LineCount = i.Output.LineCount,
			NextSequence = i.Output.NextSequence,
		}).OrderBy(s => s.StartedAtUnixMs).ToArray();
		return ListInvocationsOutcome.Success(snapshot);
	}

	public GetOutputOutcome GetInvocationOutput(long requestAccountId, string invocationId, long? sinceSequence)
	{
		if (requestAccountId != LauncherAccountId) return GetOutputOutcome.AccountMismatch();
		var inv = _invocations.Get(invocationId);
		if (inv == null) return GetOutputOutcome.NotFound();
		var page = inv.Output.Snapshot(sinceSequence ?? -1);
		return GetOutputOutcome.Success(page, status: inv.Status.ToString().ToLowerInvariant(), exitCode: inv.ExitCode);
	}

	public CancelOutcome CancelInvocation(long requestAccountId, string invocationId)
	{
		if (requestAccountId != LauncherAccountId) return CancelOutcome.AccountMismatch();
		var inv = _invocations.Get(invocationId);
		if (inv == null) return CancelOutcome.NotFound();
		var cancelled = _invocations.Cancel(invocationId);
		return cancelled ? CancelOutcome.Success() : CancelOutcome.AlreadyTerminal();
	}

	// --- File operations API -----------------------------------------------------------

	public ListDirOutcome ListDir(long requestAccountId, string path, bool showHidden)
	{
		if (requestAccountId != LauncherAccountId) return ListDirOutcome.AccountMismatch();
		if (!_pathValidator.TryCanonicalize(path ?? string.Empty, out var canonical))
			return ListDirOutcome.PathOutsideRoot();
		try
		{
			return ListDirOutcome.Success(_fileService.ListDir(canonical, showHidden));
		}
		catch (SandboxFileException ex) { return ListDirOutcome.FileError(ex.Code, ex.Message); }
	}

	public StatOutcome Stat(long requestAccountId, string path)
	{
		if (requestAccountId != LauncherAccountId) return StatOutcome.AccountMismatch();
		if (!_pathValidator.TryCanonicalize(path ?? string.Empty, out var canonical))
			return StatOutcome.PathOutsideRoot();
		try { return StatOutcome.Success(_fileService.Stat(canonical)); }
		catch (SandboxFileException ex) { return StatOutcome.FileError(ex.Code, ex.Message); }
	}

	public ReadFileOutcome ReadFile(long requestAccountId, string path, long? rangeStart, long? rangeEnd)
	{
		if (requestAccountId != LauncherAccountId) return ReadFileOutcome.AccountMismatch();
		if (!_pathValidator.TryCanonicalize(path ?? string.Empty, out var canonical))
			return ReadFileOutcome.PathOutsideRoot();
		try { return ReadFileOutcome.Success(_fileService.Read(canonical, rangeStart, rangeEnd)); }
		catch (SandboxFileException ex) { return ReadFileOutcome.FileError(ex.Code, ex.Message); }
	}

	public WriteFileOutcome WriteFile(long requestAccountId, string path, string contents, string? expectedContentHash)
	{
		if (requestAccountId != LauncherAccountId) return WriteFileOutcome.AccountMismatch();
		if (!_pathValidator.TryCanonicalize(path ?? string.Empty, out var canonical))
			return WriteFileOutcome.PathOutsideRoot();
		try { return WriteFileOutcome.Success(_fileService.Write(canonical, contents ?? string.Empty, expectedContentHash)); }
		catch (SandboxFileConflictException ex) { return WriteFileOutcome.Conflict(ex.CurrentContentHash); }
		catch (SandboxFileException ex) { return WriteFileOutcome.FileError(ex.Code, ex.Message); }
	}

	public DeleteFileOutcome DeleteFile(long requestAccountId, string path)
	{
		if (requestAccountId != LauncherAccountId) return DeleteFileOutcome.AccountMismatch();
		if (!_pathValidator.TryCanonicalize(path ?? string.Empty, out var canonical))
			return DeleteFileOutcome.PathOutsideRoot();
		try { _fileService.Delete(canonical); return DeleteFileOutcome.Success(); }
		catch (SandboxFileException ex) { return DeleteFileOutcome.FileError(ex.Code, ex.Message); }
	}

	public RenameOutcome Rename(long requestAccountId, string fromPath, string toPath)
	{
		if (requestAccountId != LauncherAccountId) return RenameOutcome.AccountMismatch();
		if (!_pathValidator.TryCanonicalize(fromPath ?? string.Empty, out var canonicalFrom))
			return RenameOutcome.PathOutsideRoot();
		if (!_pathValidator.TryCanonicalize(toPath ?? string.Empty, out var canonicalTo))
			return RenameOutcome.PathOutsideRoot();
		try { _fileService.Rename(canonicalFrom, canonicalTo); return RenameOutcome.Success(); }
		catch (SandboxFileException ex) { return RenameOutcome.FileError(ex.Code, ex.Message); }
	}

	public MakeDirOutcome MakeDir(long requestAccountId, string path)
	{
		if (requestAccountId != LauncherAccountId) return MakeDirOutcome.AccountMismatch();
		if (!_pathValidator.TryCanonicalize(path ?? string.Empty, out var canonical))
			return MakeDirOutcome.PathOutsideRoot();
		try { _fileService.MakeDir(canonical); return MakeDirOutcome.Success(); }
		catch (SandboxFileException ex) { return MakeDirOutcome.FileError(ex.Code, ex.Message); }
	}

	public WatchPathsOutcome WatchPaths(long requestAccountId, IReadOnlyList<string> paths)
	{
		if (requestAccountId != LauncherAccountId) return WatchPathsOutcome.AccountMismatch();
		if (paths == null || paths.Count == 0) return WatchPathsOutcome.MalformedRequest();

		// Validate every supplied path against containment before opening any watcher.
		// All-or-nothing — partial validation would produce surprising "some paths
		// watched, others rejected" semantics.
		var canonicalPaths = new List<string>(paths.Count);
		foreach (var p in paths)
		{
			if (!_pathValidator.TryCanonicalize(p ?? string.Empty, out var canon))
			{
				SandboxLog.Warn($"[sandbox-watch] WatchPaths rejected: path '{p}' canonicalizes outside repo root '{RepoRoot}'");
				return WatchPathsOutcome.PathOutsideRoot();
			}
			canonicalPaths.Add(canon);
		}

		var id = _fileWatcher.Watch(canonicalPaths);
		return WatchPathsOutcome.Success(id);
	}

	public UnwatchPathsOutcome UnwatchPaths(long requestAccountId, string watchId)
	{
		if (requestAccountId != LauncherAccountId) return UnwatchPathsOutcome.AccountMismatch();
		if (string.IsNullOrEmpty(watchId)) return UnwatchPathsOutcome.NotFound();
		return _fileWatcher.Unwatch(watchId) ? UnwatchPathsOutcome.Success() : UnwatchPathsOutcome.NotFound();
	}

	public int ActiveWatchCount => _fileWatcher.ActiveWatchCount;

	// --- Sink + notification flush -----------------------------------------------------

	internal void RecordOutputLine(string invocationId, string channel, string line)
	{
		var inv = _invocations.Get(invocationId);
		if (inv == null) return;
		inv.Output.Append(channel, line);
		_batcher.Enqueue(new SandboxEvent.InvocationOutput(invocationId, channel, line), _clock.UtcNow);
	}

	internal void RecordStatusChange(string invocationId, InvocationStatusKind status, int? exitCode, string? failureReason)
	{
		var inv = _invocations.Get(invocationId);
		if (inv != null) inv.TryTransition(status, exitCode, failureReason);
		_batcher.Enqueue(
			new SandboxEvent.InvocationStatus(invocationId, status.ToString().ToLowerInvariant(), exitCode),
			_clock.UtcNow);
	}

	/// <summary>
	/// Pumps the batcher: flushes any due batch through the notifications API, returning
	/// the flushed batch (or null if nothing was due). Host's background loop calls this
	/// on a tick (typically every ~100 ms). Safe to call concurrently with route handlers.
	/// </summary>
	public SandboxBatch? PumpBatcher()
	{
		var batch = _batcher.TryFlushIfDue(_clock.UtcNow);
		if (batch != null) ShipBatch(batch);
		return batch;
	}

	/// <summary>
	/// Flushes anything pending immediately. Used during shutdown so the
	/// <c>shutdown-imminent</c> event ships before the gateway drops the connection.
	/// </summary>
	public SandboxBatch? FlushPending()
	{
		var batch = _batcher.FlushNow(_clock.UtcNow);
		if (batch != null) ShipBatch(batch);
		return batch;
	}

	private void ShipBatch(SandboxBatch batch)
	{
		// Notifications API may not be bound in unit tests; the batch still flowed through
		// the batcher so the caller can inspect what would have shipped.
		if (_notifications == null)
		{
			SandboxLog.Warn($"[sandbox-batch] DROPPED batch seq={batch.Sequence} events={batch.Events.Count} — notifications API not bound");
			return;
		}
		var summary = SummarizeBatch(batch);
		SandboxLog.Info($"[sandbox-batch] ship seq={batch.Sequence} events={batch.Events.Count} {summary}");
		// Convert to the DTO before shipping. The internal record types use
		// init-only properties, which UnityJsonContractResolver filters out
		// (it serializes public mutable fields only) — without the DTO
		// conversion the wire payload is `{}`.
		_notifications.NotifyServer(true, "sandbox-events", batch.ToDto());
	}

	private static string SummarizeBatch(SandboxBatch batch)
	{
		var counts = new Dictionary<string, int>();
		foreach (var ev in batch.Events)
		{
			var key = ev switch
			{
				SandboxEvent.InvocationOutput => "output",
				SandboxEvent.InvocationStatus => "status",
				SandboxEvent.FileChanged => "file",
				SandboxEvent.Connection => "conn",
				SandboxEvent.ShutdownImminent => "shutdown",
				_ => "?"
			};
			counts.TryGetValue(key, out var n);
			counts[key] = n + 1;
		}
		return string.Join(",", counts.Select(kv => $"{kv.Key}={kv.Value}"));
	}

	public void Dispose()
	{
		// File watchers hold OS handles; the host calls Dispose after the run loop exits
		// so they don't leak past the sandbox process.
		_fileWatcher.Dispose();
	}

	private sealed class InvocationSink : IInvocationSink
	{
		private readonly SandboxObserver _observer;
		private readonly string _invocationId;

		public InvocationSink(SandboxObserver observer, string invocationId)
		{
			_observer = observer;
			_invocationId = invocationId;
		}

		public void EmitOutput(string channel, string line)
			=> _observer.RecordOutputLine(_invocationId, channel, line);

		public void EmitStatus(InvocationStatusKind status, int? exitCode = null, string? failureReason = null)
			=> _observer.RecordStatusChange(_invocationId, status, exitCode, failureReason);
	}

	private static bool ConstantTimeStringEquals(string a, string b)
	{
		var aBytes = System.Text.Encoding.UTF8.GetBytes(a);
		var bBytes = System.Text.Encoding.UTF8.GetBytes(b);
		if (aBytes.Length != bBytes.Length) return false;
		return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
	}
}

public sealed class SandboxSession
{
	public string Id { get; }
	public byte[] HmacSecret { get; }
	public DateTimeOffset PairedAt { get; }

	public SandboxSession(string id, byte[] hmacSecret, DateTimeOffset pairedAt)
	{
		Id = id;
		HmacSecret = hmacSecret;
		PairedAt = pairedAt;
	}
}

public enum SandboxConnectionEventKind
{
	Paired,
	Unpaired,
	Shutdown,
}

public sealed class SandboxConnectionEvent
{
	public SandboxConnectionEventKind Kind { get; }
	public string? SessionId { get; }
	public DateTimeOffset At { get; }

	public SandboxConnectionEvent(SandboxConnectionEventKind kind, string? sessionId, DateTimeOffset at)
	{
		Kind = kind;
		SessionId = sessionId;
		At = at;
	}

	public SandboxConnectionEvent Clone() => new(Kind, SessionId, At);
}

public enum ShutdownReason
{
	UserRequested,
	Deposed,
	HostTerminating,
}

public sealed class SandboxInfoSnapshot
{
	public string Name { get; set; } = string.Empty;
	public string? Label { get; set; }
	public string RepoRoot { get; set; } = string.Empty;
	public string Cid { get; set; } = string.Empty;
	public string Pid { get; set; } = string.Empty;
	public DateTimeOffset StartedAt { get; set; }
	public string SandboxVersion { get; set; } = string.Empty;
	public SandboxConnectionEvent[] ConnectionHistory { get; set; } = Array.Empty<SandboxConnectionEvent>();
	public int ActiveSessionCount { get; set; }
}

public enum SandboxRouteStatus
{
	Ok = 200,
	Unauthorized = 401,
	TooManyRequests = 429,
}

public readonly struct PairOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public SandboxSession? Session { get; init; }

	public static PairOutcome Success(SandboxSession session) => new()
		{ Status = SandboxRouteStatus.Ok, Session = session };
	public static PairOutcome AccountMismatch() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static PairOutcome WrongCode() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "wrong-code" };
	public static PairOutcome RateLimited() => new()
		{ Status = SandboxRouteStatus.TooManyRequests, Error = "rate-limited" };
}

public readonly struct InfoOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public SandboxInfoSnapshot? Snapshot { get; init; }

	public static InfoOutcome Success(SandboxInfoSnapshot snap) => new()
		{ Status = SandboxRouteStatus.Ok, Snapshot = snap };
	public static InfoOutcome AccountMismatch() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
}

public readonly struct ShutdownOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }

	public static ShutdownOutcome Success() => new() { Status = SandboxRouteStatus.Ok };
	public static ShutdownOutcome AccountMismatch() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
}

public sealed class InvocationSummary
{
	public string Id { get; init; } = string.Empty;
	public string CommandLine { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public int? ExitCode { get; init; }
	public long StartedAtUnixMs { get; init; }
	public int LineCount { get; init; }
	public long NextSequence { get; init; }
}

public readonly struct InvokeOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public string? InvocationId { get; init; }

	public static InvokeOutcome Success(string invocationId) => new()
		{ Status = SandboxRouteStatus.Ok, InvocationId = invocationId };
	public static InvokeOutcome AccountMismatch() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static InvokeOutcome MalformedRequest() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "empty-command" };
	public static InvokeOutcome RunnerUnavailable() => new()
		{ Status = (SandboxRouteStatus)503, Error = "runner-unavailable" };
}

public readonly struct ListInvocationsOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public InvocationSummary[]? Items { get; init; }

	public static ListInvocationsOutcome Success(InvocationSummary[] items) => new()
		{ Status = SandboxRouteStatus.Ok, Items = items };
	public static ListInvocationsOutcome AccountMismatch() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
}

public readonly struct GetOutputOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public InvocationOutputPage? Page { get; init; }
	public string? InvocationStatus { get; init; }
	public int? ExitCode { get; init; }

	public static GetOutputOutcome Success(InvocationOutputPage page, string status, int? exitCode) => new()
		{ Status = SandboxRouteStatus.Ok, Page = page, InvocationStatus = status, ExitCode = exitCode };
	public static GetOutputOutcome AccountMismatch() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static GetOutputOutcome NotFound() => new()
		{ Status = (SandboxRouteStatus)404, Error = "not-found" };
}

public readonly struct CancelOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }

	public static CancelOutcome Success() => new() { Status = SandboxRouteStatus.Ok };
	public static CancelOutcome AccountMismatch() => new()
		{ Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static CancelOutcome NotFound() => new()
		{ Status = (SandboxRouteStatus)404, Error = "not-found" };
	public static CancelOutcome AlreadyTerminal() => new()
		{ Status = (SandboxRouteStatus)409, Error = "already-terminal" };
}

// --- File operations outcomes ---------------------------------------------------------
// All file outcomes share a common shape: 200 success carries the result payload; auth
// failures and containment violations land on 401/403; file-layer issues (not-found,
// too-large, conflict) carry an error code the caller can branch on.

public readonly struct ListDirOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public DirListing? Listing { get; init; }
	public static ListDirOutcome Success(DirListing l) => new() { Status = SandboxRouteStatus.Ok, Listing = l };
	public static ListDirOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static ListDirOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static ListDirOutcome FileError(string code, string msg) => new() { Status = (SandboxRouteStatus)404, Error = code };
}

public readonly struct StatOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public FileStat? Stat { get; init; }
	public static StatOutcome Success(FileStat s) => new() { Status = SandboxRouteStatus.Ok, Stat = s };
	public static StatOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static StatOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static StatOutcome FileError(string code, string msg) => new() { Status = (SandboxRouteStatus)404, Error = code };
}

public readonly struct ReadFileOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public FileReadResult? Result { get; init; }
	public static ReadFileOutcome Success(FileReadResult r) => new() { Status = SandboxRouteStatus.Ok, Result = r };
	public static ReadFileOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static ReadFileOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static ReadFileOutcome FileError(string code, string msg)
	{
		// "too-large" maps to 413 Payload Too Large; everything else uses 404 since the
		// observable bad-state is "you asked for something the FS won't give you".
		var statusCode = code == "too-large" ? 413 : 404;
		return new() { Status = (SandboxRouteStatus)statusCode, Error = code };
	}
}

public readonly struct WriteFileOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public FileWriteResult? Result { get; init; }
	public string? ConflictHash { get; init; }
	public static WriteFileOutcome Success(FileWriteResult r) => new() { Status = SandboxRouteStatus.Ok, Result = r };
	public static WriteFileOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static WriteFileOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static WriteFileOutcome Conflict(string currentHash) => new()
		{ Status = (SandboxRouteStatus)409, Error = "conflict", ConflictHash = currentHash };
	public static WriteFileOutcome FileError(string code, string msg) => new() { Status = (SandboxRouteStatus)500, Error = code };
}

public readonly struct DeleteFileOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public static DeleteFileOutcome Success() => new() { Status = SandboxRouteStatus.Ok };
	public static DeleteFileOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static DeleteFileOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static DeleteFileOutcome FileError(string code, string msg) => new() { Status = (SandboxRouteStatus)404, Error = code };
}

public readonly struct RenameOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public static RenameOutcome Success() => new() { Status = SandboxRouteStatus.Ok };
	public static RenameOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static RenameOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static RenameOutcome FileError(string code, string msg) => new() { Status = (SandboxRouteStatus)404, Error = code };
}

public readonly struct MakeDirOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public static MakeDirOutcome Success() => new() { Status = SandboxRouteStatus.Ok };
	public static MakeDirOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static MakeDirOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static MakeDirOutcome FileError(string code, string msg) => new() { Status = (SandboxRouteStatus)500, Error = code };
}

public readonly struct WatchPathsOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public string? WatchId { get; init; }
	public static WatchPathsOutcome Success(string watchId) => new() { Status = SandboxRouteStatus.Ok, WatchId = watchId };
	public static WatchPathsOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static WatchPathsOutcome PathOutsideRoot() => new() { Status = (SandboxRouteStatus)403, Error = "path-outside-root" };
	public static WatchPathsOutcome MalformedRequest() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "empty-paths" };
}

public readonly struct UnwatchPathsOutcome
{
	public SandboxRouteStatus Status { get; init; }
	public string? Error { get; init; }
	public static UnwatchPathsOutcome Success() => new() { Status = SandboxRouteStatus.Ok };
	public static UnwatchPathsOutcome AccountMismatch() => new() { Status = SandboxRouteStatus.Unauthorized, Error = "account-mismatch" };
	public static UnwatchPathsOutcome NotFound() => new() { Status = (SandboxRouteStatus)404, Error = "not-found" };
}
