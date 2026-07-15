using System;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class InvocationRegistryTests
{
	private FakeClock _clock = null!;
	private InvocationRegistry _registry = null!;

	[SetUp]
	public void SetUp()
	{
		_clock = new FakeClock(DateTimeOffset.UnixEpoch);
		_registry = new InvocationRegistry(_clock);
	}

	[Test]
	public void Start_AssignsIdAndCapturesStartTime()
	{
		var inv = _registry.Start("project ps");
		Assert.That(inv.Id, Is.Not.Empty);
		Assert.That(inv.CommandLine, Is.EqualTo("project ps"));
		Assert.That(inv.Status, Is.EqualTo(InvocationStatusKind.Started));
		Assert.That(inv.StartedAt, Is.EqualTo(_clock.UtcNow));
	}

	[Test]
	public void Get_ByUnknownId_ReturnsNull()
	{
		Assert.That(_registry.Get("nope"), Is.Null);
		Assert.That(_registry.Get(""), Is.Null);
	}

	[Test]
	public void Get_ByKnownId_ReturnsSameInstance()
	{
		var inv = _registry.Start("foo");
		Assert.That(_registry.Get(inv.Id), Is.SameAs(inv));
	}

	[Test]
	public void ListInflight_ExcludesTerminal()
	{
		var a = _registry.Start("a");
		var b = _registry.Start("b");
		b.TryTransition(InvocationStatusKind.Completed, exitCode: 0);

		var inflight = _registry.ListInflight();
		Assert.That(inflight, Has.Count.EqualTo(1));
		Assert.That(inflight[0], Is.SameAs(a));
	}

	[Test]
	public void Cancel_TerminalInvocation_ReturnsFalse()
	{
		var inv = _registry.Start("x");
		inv.TryTransition(InvocationStatusKind.Completed, 0);
		Assert.That(_registry.Cancel(inv.Id), Is.False);
	}

	[Test]
	public void Cancel_RunningInvocation_TripsToken()
	{
		var inv = _registry.Start("x");
		Assert.That(_registry.Cancel(inv.Id), Is.True);
		Assert.That(inv.CancellationToken.IsCancellationRequested, Is.True);
	}

	[Test]
	public void TerminalStatus_IsSticky()
	{
		var inv = _registry.Start("x");
		Assert.That(inv.TryTransition(InvocationStatusKind.Completed, 0), Is.True);
		Assert.That(inv.TryTransition(InvocationStatusKind.Failed, failureReason: "huh"), Is.False);
		Assert.That(inv.Status, Is.EqualTo(InvocationStatusKind.Completed));
		Assert.That(inv.ExitCode, Is.EqualTo(0));
	}

	[Test]
	public void Prune_DropsOldTerminalOnceOverCap()
	{
		var small = new InvocationRegistry(_clock, maxRetained: 2, retainTerminalFor: TimeSpan.FromSeconds(1));
		var a = small.Start("a"); a.TryTransition(InvocationStatusKind.Completed, 0);
		var b = small.Start("b"); b.TryTransition(InvocationStatusKind.Completed, 0);
		_clock.Advance(TimeSpan.FromSeconds(10));
		var c = small.Start("c"); // this Start triggers PruneTerminalLocked since count would exceed cap

		// After pruning, only c (inflight) plus possibly one terminal remains; total <= cap.
		Assert.That(small.List().Count, Is.LessThanOrEqualTo(2));
	}
}
