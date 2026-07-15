using System;
using System.Collections.Generic;

namespace cli.Services.Sandbox;

/// <summary>
/// Rate-limits join-code attempts against the sandbox Pair route. The join code is
/// stable for the lifetime of the sandbox run; the rate limit is what makes brute-force
/// guessing impractical and bounds the security delta versus one-time codes.
///
/// Policy: more than <see cref="MaxFailures"/> failures within <see cref="FailureWindow"/>
/// lock out further attempts for <see cref="LockoutDuration"/>. A successful attempt
/// clears the failure ledger.
/// </summary>
public sealed class JoinCodeRateLimiter
{
	public const int MaxFailures = 5;
	public static readonly TimeSpan FailureWindow = TimeSpan.FromSeconds(60);
	public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

	private readonly IClock _clock;
	private readonly object _lock = new();
	private readonly Queue<DateTimeOffset> _recentFailures = new();
	private DateTimeOffset? _lockedUntil;

	public JoinCodeRateLimiter(IClock clock)
	{
		_clock = clock ?? throw new ArgumentNullException(nameof(clock));
	}

	/// <summary>
	/// True if the Pair route is currently locked out. While locked, even a correct code
	/// must be rejected so an attacker who has been throttling can't slip through.
	/// </summary>
	public bool IsLocked()
	{
		lock (_lock)
		{
			return IsLockedNoLock();
		}
	}

	/// <summary>
	/// Should be called by the Pair handler before checking the supplied code. If this
	/// returns false, the route is currently locked; the handler should return 429/locked.
	/// </summary>
	public bool TryBeginAttempt()
	{
		lock (_lock)
		{
			return !IsLockedNoLock();
		}
	}

	/// <summary>
	/// Record a failed code attempt. Returns true if the lockout just triggered.
	/// </summary>
	public bool RecordFailure()
	{
		lock (_lock)
		{
			var now = _clock.UtcNow;
			TrimOldFailuresNoLock(now);
			_recentFailures.Enqueue(now);

			if (_recentFailures.Count > MaxFailures)
			{
				_lockedUntil = now + LockoutDuration;
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// Record a successful pair; clears the failure ledger.
	/// </summary>
	public void RecordSuccess()
	{
		lock (_lock)
		{
			_recentFailures.Clear();
			_lockedUntil = null;
		}
	}

	private bool IsLockedNoLock()
	{
		if (_lockedUntil == null) return false;
		if (_clock.UtcNow >= _lockedUntil.Value)
		{
			_lockedUntil = null;
			_recentFailures.Clear();
			return false;
		}
		return true;
	}

	private void TrimOldFailuresNoLock(DateTimeOffset now)
	{
		var cutoff = now - FailureWindow;
		while (_recentFailures.Count > 0 && _recentFailures.Peek() < cutoff)
		{
			_recentFailures.Dequeue();
		}
	}
}
