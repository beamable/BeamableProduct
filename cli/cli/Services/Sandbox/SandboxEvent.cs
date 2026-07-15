using System;
using System.Collections.Generic;
using System.Linq;

namespace cli.Services.Sandbox;

/// <summary>
/// Internal discriminated union of events the sandbox sends on the
/// <c>sandbox-events</c> channel. Used by the batcher and observer for
/// pattern matching, collapsing, and dispatch — clean and immutable.
///
/// On the wire we ship <see cref="SandboxBatchDto"/> instead: Beamable's
/// <c>UnityJsonContractResolver</c> only serializes public mutable fields,
/// so records (whose backing fields are init-only) serialize to <c>{}</c>.
/// Convert via <see cref="SandboxBatchExtensions.ToDto"/> right before
/// calling <c>NotifyServer</c>.
/// </summary>
public abstract record SandboxEvent
{
	public sealed record InvocationOutput(string InvocationId, string Channel, string Line) : SandboxEvent;

	public sealed record InvocationStatus(string InvocationId, string Status, int? ExitCode) : SandboxEvent;

	public sealed record FileChanged(string WatchId, string Path, IReadOnlyList<string> Kinds) : SandboxEvent;

	public sealed record Connection(string SessionId, string Kind) : SandboxEvent;

	public sealed record ShutdownImminent(string Reason) : SandboxEvent;
}

public sealed class SandboxBatch
{
	public long Sequence { get; init; }
	public DateTimeOffset EmittedAt { get; init; }
	public IReadOnlyList<SandboxEvent> Events { get; init; } = Array.Empty<SandboxEvent>();
}

// ── Wire DTOs ───────────────────────────────────────────────────────────────────
// Public mutable fields, camelCase names so the serialized JSON matches the
// Portal-side TypeScript types directly. Single flat "event" DTO with a `type`
// discriminator + nullable per-variant fields — mirrors how the TS side reads
// the discriminated union.

[Serializable]
public class SandboxBatchDto
{
	public long sequence;
	public long emittedAtUnixMs;
	public SandboxEventDto[] events = Array.Empty<SandboxEventDto>();
}

[Serializable]
public class SandboxEventDto
{
	/// <summary>
	/// Discriminator: one of <c>invocation-output</c>, <c>invocation-status</c>,
	/// <c>file-changed</c>, <c>connection</c>, <c>shutdown-imminent</c>.
	/// </summary>
	public string type = string.Empty;

	// invocation-output, invocation-status
	public string? invocationId;
	// invocation-output
	public string? channel;
	public string? line;
	// invocation-status
	public string? status;
	public int? exitCode;
	// file-changed
	public string? watchId;
	public string? path;
	public string[]? kinds;
	// connection
	public string? sessionId;
	public string? kind;
	// shutdown-imminent
	public string? reason;
}

public static class SandboxBatchExtensions
{
	public static SandboxBatchDto ToDto(this SandboxBatch batch) => new()
	{
		sequence = batch.Sequence,
		emittedAtUnixMs = batch.EmittedAt.ToUnixTimeMilliseconds(),
		events = batch.Events.Select(EventToDto).ToArray(),
	};

	private static SandboxEventDto EventToDto(SandboxEvent e) => e switch
	{
		SandboxEvent.InvocationOutput o => new SandboxEventDto
		{
			type = "invocation-output",
			invocationId = o.InvocationId,
			channel = o.Channel,
			line = o.Line,
		},
		SandboxEvent.InvocationStatus s => new SandboxEventDto
		{
			type = "invocation-status",
			invocationId = s.InvocationId,
			status = s.Status,
			exitCode = s.ExitCode,
		},
		SandboxEvent.FileChanged f => new SandboxEventDto
		{
			type = "file-changed",
			watchId = f.WatchId,
			path = f.Path,
			kinds = f.Kinds.ToArray(),
		},
		SandboxEvent.Connection c => new SandboxEventDto
		{
			type = "connection",
			sessionId = c.SessionId,
			kind = c.Kind,
		},
		SandboxEvent.ShutdownImminent si => new SandboxEventDto
		{
			type = "shutdown-imminent",
			reason = si.Reason,
		},
		_ => throw new InvalidOperationException($"Unknown SandboxEvent variant: {e.GetType().Name}"),
	};
}
