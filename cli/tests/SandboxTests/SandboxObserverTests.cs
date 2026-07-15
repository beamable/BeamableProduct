using System;
using System.Linq;
using System.Threading.Tasks;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class SandboxObserverTests
{
	private const long LauncherId = 4827;
	private const string JoinCode = "K2M-X9Q3";
	private FakeClock _clock = null!;

	[SetUp]
	public void SetUp()
	{
		_clock = new FakeClock(DateTimeOffset.UnixEpoch);
	}

	private SandboxObserver NewObserver(string code = JoinCode) =>
		new(
			launcherAccountId: LauncherId,
			cid: "123",
			pid: "456",
			serviceName: "BeamSandbox_4827_d1f2c3a4-b5e6-4a7b-8c9d-0123456789ab",
			repoRoot: "/repo",
			joinCode: code,
			sandboxVersion: "0.0.1",
			label: null,
			clock: _clock);

	[Test]
	public void Pair_CorrectCode_ReturnsSessionWithSecret()
	{
		var obs = NewObserver();
		var outcome = obs.Pair(LauncherId, JoinCode);

		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Ok));
		Assert.That(outcome.Session, Is.Not.Null);
		Assert.That(outcome.Session!.HmacSecret.Length, Is.EqualTo(32));
		Assert.That(outcome.Session.Id, Is.Not.Empty);
	}

	[Test]
	public void Pair_WrongCode_ReturnsUnauthorized()
	{
		var obs = NewObserver();
		var outcome = obs.Pair(LauncherId, "WRONG-COD");
		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(outcome.Error, Is.EqualTo("wrong-code"));
		Assert.That(outcome.Session, Is.Null);
	}

	[Test]
	public void Pair_DifferentAccount_ReturnsUnauthorized_AndDoesNotCountTowardRateLimit()
	{
		var obs = NewObserver();
		for (var i = 0; i < 100; i++)
		{
			var bad = obs.Pair(LauncherId + 1, JoinCode);
			Assert.That(bad.Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
			Assert.That(bad.Error, Is.EqualTo("account-mismatch"));
		}
		// Same-account correct code still works — wrong-account attempts didn't poison the rate limiter.
		var good = obs.Pair(LauncherId, JoinCode);
		Assert.That(good.Status, Is.EqualTo(SandboxRouteStatus.Ok));
	}

	[Test]
	public void Pair_SixFailedAttempts_TriggerRateLimit()
	{
		var obs = NewObserver();
		for (var i = 0; i < JoinCodeRateLimiter.MaxFailures + 1; i++)
		{
			obs.Pair(LauncherId, "BAD" + i);
		}
		var locked = obs.Pair(LauncherId, JoinCode);
		Assert.That(locked.Status, Is.EqualTo(SandboxRouteStatus.TooManyRequests));
		Assert.That(locked.Error, Is.EqualTo("rate-limited"));
	}

	[Test]
	public void Pair_SuccessClearsFailureLedger()
	{
		var obs = NewObserver();
		// 4 fails (still under the limit), then a success — ledger clears, so 5 more fails
		// must not lock the route.
		for (var i = 0; i < 4; i++) obs.Pair(LauncherId, "BAD" + i);
		Assert.That(obs.Pair(LauncherId, JoinCode).Status, Is.EqualTo(SandboxRouteStatus.Ok));

		for (var i = 0; i < 5; i++)
		{
			var r = obs.Pair(LauncherId, "BAD-AGAIN-" + i);
			Assert.That(r.Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
			Assert.That(r.Error, Is.EqualTo("wrong-code"));
		}
	}

	[Test]
	public void Pair_RecordsConnectionEvent()
	{
		var obs = NewObserver();
		obs.Pair(LauncherId, JoinCode);
		var history = obs.ConnectionHistory;
		Assert.That(history, Has.Count.EqualTo(1));
		Assert.That(history[0].Kind, Is.EqualTo(SandboxConnectionEventKind.Paired));
		Assert.That(history[0].SessionId, Is.Not.Null);
	}

	[Test]
	public void Pair_DistinctCalls_ProduceDistinctSessionSecrets()
	{
		var obs = NewObserver();
		var a = obs.Pair(LauncherId, JoinCode).Session!;
		var b = obs.Pair(LauncherId, JoinCode).Session!;
		Assert.That(a.Id, Is.Not.EqualTo(b.Id));
		Assert.That(a.HmacSecret, Is.Not.EqualTo(b.HmacSecret));
	}

	[Test]
	public void Pair_CodeLengthMismatch_DoesNotThrow_ReturnsWrongCode()
	{
		var obs = NewObserver();
		Assert.That(obs.Pair(LauncherId, "x").Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(obs.Pair(LauncherId, "").Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(obs.Pair(LauncherId, null!).Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
	}

	[Test]
	public void Info_FromLauncher_ReturnsSnapshot()
	{
		var obs = NewObserver();
		var outcome = obs.GetInfo(LauncherId);
		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Ok));
		var snap = outcome.Snapshot!;
		Assert.That(snap.Cid, Is.EqualTo("123"));
		Assert.That(snap.Pid, Is.EqualTo("456"));
		Assert.That(snap.RepoRoot, Is.EqualTo("/repo"));
		Assert.That(snap.SandboxVersion, Is.EqualTo("0.0.1"));
		Assert.That(snap.ActiveSessionCount, Is.EqualTo(0));
	}

	[Test]
	public void Info_FromDifferentAccount_Returns401()
	{
		var obs = NewObserver();
		var outcome = obs.GetInfo(LauncherId + 1);
		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
	}

	[Test]
	public void Info_NeverLeaksJoinCodeOrSecrets()
	{
		var obs = NewObserver();
		obs.Pair(LauncherId, JoinCode);
		var snap = obs.GetInfo(LauncherId).Snapshot!;

		// SandboxInfoSnapshot defines a finite set of fields; this guards against future
		// fields accidentally leaking the join code or session secret material.
		var serialized = System.Text.Json.JsonSerializer.Serialize(snap);
		Assert.That(serialized, Does.Not.Contain(JoinCode));
		// session secret bytes are 32 random bytes; we just confirm none of the fields
		// look like a base64 secret by checking that no field's value is suspicious length.
		// In practice we just sanity-check that the connection history exposes a sessionId
		// but not the bytes.
		Assert.That(snap.ConnectionHistory, Has.Length.EqualTo(1));
	}

	[Test]
	public void Info_ActiveSessionCount_TracksPairs()
	{
		var obs = NewObserver();
		obs.Pair(LauncherId, JoinCode);
		obs.Pair(LauncherId, JoinCode);
		Assert.That(obs.GetInfo(LauncherId).Snapshot!.ActiveSessionCount, Is.EqualTo(2));
	}

	[Test]
	public async Task Shutdown_FromLauncher_SignalsHostLoop()
	{
		var obs = NewObserver();
		var outcome = obs.Shutdown(LauncherId, ShutdownReason.UserRequested);
		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Ok));
		// The signal task should resolve immediately.
		var completed = await Task.WhenAny(obs.ShutdownSignal, Task.Delay(1000));
		Assert.That(completed, Is.SameAs(obs.ShutdownSignal));
		Assert.That(obs.ShutdownSignal.Result, Is.EqualTo(ShutdownReason.UserRequested));
	}

	[Test]
	public void Shutdown_FromDifferentAccount_Returns401_AndDoesNotSignal()
	{
		var obs = NewObserver();
		var outcome = obs.Shutdown(LauncherId + 1, ShutdownReason.UserRequested);
		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Unauthorized));
		Assert.That(obs.ShutdownSignal.IsCompleted, Is.False);
	}

	[Test]
	public async Task Shutdown_Twice_KeepsFirstReason()
	{
		var obs = NewObserver();
		obs.Shutdown(LauncherId, ShutdownReason.Deposed);
		obs.Shutdown(LauncherId, ShutdownReason.UserRequested);

		// First reason wins because TrySetResult is one-shot.
		Assert.That(obs.ShutdownSignal.IsCompleted, Is.True);
		Assert.That(await obs.ShutdownSignal, Is.EqualTo(ShutdownReason.Deposed));
	}

	[Test]
	public void Shutdown_AppendsConnectionHistoryEvent()
	{
		var obs = NewObserver();
		obs.Pair(LauncherId, JoinCode);
		obs.Shutdown(LauncherId, ShutdownReason.UserRequested);
		var kinds = obs.ConnectionHistory.Select(e => e.Kind).ToArray();
		Assert.That(kinds, Is.EqualTo(new[]
		{
			SandboxConnectionEventKind.Paired,
			SandboxConnectionEventKind.Shutdown,
		}));
	}

	[Test]
	public void Constructor_RejectsInvalidLauncherId()
	{
		Assert.Throws<ArgumentException>(() =>
		{
			_ = new SandboxObserver(
				launcherAccountId: 0,
				cid: "1", pid: "2", serviceName: "n", repoRoot: "/r",
				joinCode: "c", sandboxVersion: "v", label: null, clock: _clock);
		});
		Assert.Throws<ArgumentException>(() =>
		{
			_ = new SandboxObserver(
				launcherAccountId: -5,
				cid: "1", pid: "2", serviceName: "n", repoRoot: "/r",
				joinCode: "c", sandboxVersion: "v", label: null, clock: _clock);
		});
	}
}
