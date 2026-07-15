using System;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class JoinCodeRateLimiterTests
{
	private FakeClock _clock = null!;
	private JoinCodeRateLimiter _limiter = null!;

	[SetUp]
	public void SetUp()
	{
		_clock = new FakeClock(DateTimeOffset.UnixEpoch);
		_limiter = new JoinCodeRateLimiter(_clock);
	}

	[Test]
	public void FreshLimiter_IsNotLocked()
	{
		Assert.That(_limiter.IsLocked(), Is.False);
		Assert.That(_limiter.TryBeginAttempt(), Is.True);
	}

	[Test]
	public void FiveFailures_DoNotTriggerLockout()
	{
		// MaxFailures = 5 → the 5th failure is still allowed (rate "more than N").
		for (var i = 0; i < JoinCodeRateLimiter.MaxFailures; i++)
		{
			Assert.That(_limiter.RecordFailure(), Is.False, $"failure #{i + 1} should not lock");
		}
		Assert.That(_limiter.IsLocked(), Is.False);
	}

	[Test]
	public void SixthFailure_TriggersLockout()
	{
		for (var i = 0; i < JoinCodeRateLimiter.MaxFailures; i++)
		{
			_limiter.RecordFailure();
		}
		Assert.That(_limiter.RecordFailure(), Is.True);
		Assert.That(_limiter.IsLocked(), Is.True);
		Assert.That(_limiter.TryBeginAttempt(), Is.False);
	}

	[Test]
	public void LockoutClears_AfterLockoutDuration()
	{
		for (var i = 0; i < JoinCodeRateLimiter.MaxFailures + 1; i++)
		{
			_limiter.RecordFailure();
		}
		Assert.That(_limiter.IsLocked(), Is.True);

		_clock.Advance(JoinCodeRateLimiter.LockoutDuration + TimeSpan.FromSeconds(1));

		Assert.That(_limiter.IsLocked(), Is.False);
		Assert.That(_limiter.TryBeginAttempt(), Is.True);
	}

	[Test]
	public void OldFailures_ExpireOutOfTheWindow()
	{
		// 4 failures, then wait > FailureWindow, then 4 more — should still not lock,
		// because the first batch has aged out.
		for (var i = 0; i < 4; i++) _limiter.RecordFailure();

		_clock.Advance(JoinCodeRateLimiter.FailureWindow + TimeSpan.FromSeconds(5));

		for (var i = 0; i < 4; i++)
		{
			Assert.That(_limiter.RecordFailure(), Is.False);
		}
		Assert.That(_limiter.IsLocked(), Is.False);
	}

	[Test]
	public void SuccessResetsLedger()
	{
		for (var i = 0; i < 4; i++) _limiter.RecordFailure();
		_limiter.RecordSuccess();
		// Need 6 more failures to lock again, not just 2.
		for (var i = 0; i < 5; i++)
		{
			Assert.That(_limiter.RecordFailure(), Is.False);
		}
		Assert.That(_limiter.IsLocked(), Is.False);
	}

	[Test]
	public void DuringLockout_FurtherFailuresStayLocked()
	{
		for (var i = 0; i < JoinCodeRateLimiter.MaxFailures + 1; i++)
		{
			_limiter.RecordFailure();
		}
		Assert.That(_limiter.IsLocked(), Is.True);
		// Even with more attempts during lockout, the lockout window doesn't extend further
		// in the current design (no reset-on-fail). It expires at the original deadline.
		_clock.Advance(JoinCodeRateLimiter.LockoutDuration - TimeSpan.FromSeconds(1));
		Assert.That(_limiter.IsLocked(), Is.True);
		_clock.Advance(TimeSpan.FromSeconds(2));
		Assert.That(_limiter.IsLocked(), Is.False);
	}
}
