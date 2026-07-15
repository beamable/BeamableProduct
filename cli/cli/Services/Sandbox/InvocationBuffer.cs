using System;
using System.Collections.Generic;
using System.Linq;

namespace cli.Services.Sandbox;

/// <summary>
/// One line of output emitted by an invocation, tagged with the structured-output
/// channel it came from (<c>stream</c>, <c>error</c>, <c>logs</c>, etc.) and a
/// monotonically-increasing sequence number for incremental re-attach.
/// </summary>
public readonly struct OutputLine
{
	public long Sequence { get; init; }
	public string Channel { get; init; }
	public string Line { get; init; }
}

/// <summary>
/// A bounded, FIFO-truncating, sequenced output buffer for a single invocation.
/// When the cap is hit, oldest lines are dropped and <see cref="InvocationOutputPage.DroppedCount"/>
/// surfaces how many were lost so Portal can render a "scrolled past oldest"
/// affordance instead of silently missing data.
/// </summary>
public sealed class InvocationBuffer
{
	public int MaxLines { get; }
	public int MaxBytes { get; }

	private readonly object _lock = new();
	private readonly LinkedList<OutputLine> _lines = new();
	private long _nextSequence;
	private long _droppedCount;
	private int _totalBytes;

	public InvocationBuffer(int maxLines = 5000, int maxBytes = 5 * 1024 * 1024)
	{
		if (maxLines <= 0) throw new ArgumentOutOfRangeException(nameof(maxLines));
		if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
		MaxLines = maxLines;
		MaxBytes = maxBytes;
	}

	public OutputLine Append(string channel, string line)
	{
		channel ??= string.Empty;
		line ??= string.Empty;

		lock (_lock)
		{
			var entry = new OutputLine
			{
				Sequence = _nextSequence++,
				Channel = channel,
				Line = line,
			};
			_lines.AddLast(entry);
			_totalBytes += EstimateBytes(entry);
			TrimToCapsLocked();
			return entry;
		}
	}

	/// <summary>
	/// Snapshot output strictly newer than <paramref name="sinceSequence"/>. Pass 0
	/// (or omit) to get everything currently buffered. The returned
	/// <see cref="InvocationOutputPage.DroppedCount"/> reflects total drops since the
	/// invocation started — Portal can compare it against its last seen value to detect
	/// that scrollback was lost.
	/// </summary>
	public InvocationOutputPage Snapshot(long sinceSequence = -1)
	{
		lock (_lock)
		{
			OutputLine[] lines = sinceSequence < 0
				? _lines.ToArray()
				: _lines.Where(l => l.Sequence > sinceSequence).ToArray();

			return new InvocationOutputPage
			{
				Lines = lines,
				DroppedCount = _droppedCount,
				NextSequence = _nextSequence,
			};
		}
	}

	public long NextSequence { get { lock (_lock) return _nextSequence; } }
	public long DroppedCount { get { lock (_lock) return _droppedCount; } }
	public int LineCount { get { lock (_lock) return _lines.Count; } }

	private void TrimToCapsLocked()
	{
		while (_lines.Count > MaxLines || _totalBytes > MaxBytes)
		{
			var first = _lines.First;
			if (first == null) break;
			_totalBytes -= EstimateBytes(first.Value);
			_lines.RemoveFirst();
			_droppedCount++;
		}
	}

	private static int EstimateBytes(OutputLine line)
	{
		// Approximate UTF-16 byte count plus a small per-entry overhead so the cap
		// reflects actual memory pressure, not just visible characters.
		return 16 + (line.Channel?.Length ?? 0) * 2 + (line.Line?.Length ?? 0) * 2;
	}
}

public sealed class InvocationOutputPage
{
	public OutputLine[] Lines { get; init; } = Array.Empty<OutputLine>();

	/// <summary>
	/// Total lines dropped from the head of the buffer over the invocation's lifetime.
	/// Strictly increases. Clients can compare to their last seen value to detect that
	/// scrollback was truncated between fetches.
	/// </summary>
	public long DroppedCount { get; init; }

	/// <summary>
	/// Sequence of the next line that will be appended. A re-attach client should pass
	/// this back as <c>sinceSequence</c> on its next poll to get only new lines.
	/// </summary>
	public long NextSequence { get; init; }
}
