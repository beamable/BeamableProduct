using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class SandboxObserverInvocationTests
{
	private const long LauncherId = 4827;
	private FakeClock _clock = null!;
	private SandboxObserver _observer = null!;
	private CapturingRunner _runner = null!;

	[SetUp]
	public void SetUp()
	{
		_clock = new FakeClock(DateTimeOffset.UnixEpoch);
		_observer = new SandboxObserver(
			launcherAccountId: LauncherId,
			cid: "1", pid: "2",
			serviceName: "BeamSandbox_4827_d1f2c3a4-b5e6-4a7b-8c9d-0123456789ab",
			repoRoot: "/r",
			joinCode: "K2M-X9Q3",
			sandboxVersion: "0.0.1",
			label: null,
			clock: _clock);
		_runner = new CapturingRunner();
		_observer.BindRunner(_runner);
	}

	[Test]
	public void Invoke_BeforeRunnerBound_Returns503()
	{
		var bare = new SandboxObserver(LauncherId, "1", "2", "n", "/r", "c", "v", null, _clock);
		var outcome = bare.Invoke(LauncherId, "project ps");
		Assert.That((int)outcome.Status, Is.EqualTo(503));
		Assert.That(outcome.Error, Is.EqualTo("runner-unavailable"));
	}

	[Test]
	public void Invoke_AccountMismatch_Returns401()
	{
		Assert.That(_observer.Invoke(LauncherId + 1, "project ps").Status,
			Is.EqualTo(SandboxRouteStatus.Unauthorized));
	}

	[Test]
	public void Invoke_EmptyCommand_Rejected()
	{
		Assert.That(_observer.Invoke(LauncherId, "").Error, Is.EqualTo("empty-command"));
		Assert.That(_observer.Invoke(LauncherId, "   ").Error, Is.EqualTo("empty-command"));
	}

	[Test]
	public async Task Invoke_StartsRunner_AndEnqueuesStatusEvents()
	{
		var outcome = _observer.Invoke(LauncherId, "project ps");
		Assert.That(outcome.Status, Is.EqualTo(SandboxRouteStatus.Ok));
		Assert.That(outcome.InvocationId, Is.Not.Empty);

		await _runner.Completed;

		// Allow the background runner task scheduled by Invoke to complete its post-RunAsync
		// status emission. We poll the batcher with a short generous timeout.
		await WaitForCondition(() =>
		{
			_clock.Advance(TimeSpan.FromSeconds(1));
			var batch = _observer.PumpBatcher();
			return batch != null && batch.Events.OfType<SandboxEvent.InvocationStatus>()
				.Any(s => s.Status == "completed");
		}, TimeSpan.FromSeconds(2));
	}

	[Test]
	public async Task Invoke_RunnerOutput_FlowsToBufferAndBatcher()
	{
		_runner.OnRun = (inv, sink) =>
		{
			sink.EmitOutput("stream", "hello");
			sink.EmitOutput("stream", "world");
			sink.EmitStatus(InvocationStatusKind.Completed, exitCode: 0);
			return Task.CompletedTask;
		};

		var outcome = _observer.Invoke(LauncherId, "echo");
		await _runner.Completed;
		await Task.Delay(50); // let the post-RunAsync code in observer settle

		_clock.Advance(TimeSpan.FromSeconds(1));
		var batch = _observer.PumpBatcher();
		Assert.That(batch, Is.Not.Null);

		var outputs = batch!.Events.OfType<SandboxEvent.InvocationOutput>().ToArray();
		Assert.That(outputs.Select(o => o.Line), Does.Contain("hello").And.Contain("world"));
		Assert.That(outputs.All(o => o.InvocationId == outcome.InvocationId), Is.True);

		// Same lines should also be in the per-invocation buffer for re-attach.
		var page = _observer.GetInvocationOutput(LauncherId, outcome.InvocationId!, null).Page!;
		Assert.That(page.Lines.Select(l => l.Line), Is.EqualTo(new[] { "hello", "world" }));
	}

	[Test]
	public async Task GetInvocationOutput_UnknownId_ReturnsNotFound()
	{
		var outcome = _observer.GetInvocationOutput(LauncherId, "no-such-invocation", null);
		Assert.That((int)outcome.Status, Is.EqualTo(404));
		await Task.CompletedTask;
	}

	[Test]
	public async Task GetInvocationOutput_SinceSequence_ReturnsOnlyNewer()
	{
		_runner.OnRun = (inv, sink) =>
		{
			sink.EmitOutput("stream", "a");
			sink.EmitOutput("stream", "b");
			sink.EmitOutput("stream", "c");
			sink.EmitStatus(InvocationStatusKind.Completed, 0);
			return Task.CompletedTask;
		};

		var invId = _observer.Invoke(LauncherId, "x").InvocationId!;
		await _runner.Completed;
		await Task.Delay(50);

		var firstPage = _observer.GetInvocationOutput(LauncherId, invId, null).Page!;
		Assert.That(firstPage.Lines, Has.Length.EqualTo(3));

		var afterFirst = _observer.GetInvocationOutput(LauncherId, invId, firstPage.Lines[0].Sequence).Page!;
		Assert.That(afterFirst.Lines.Select(l => l.Line), Is.EqualTo(new[] { "b", "c" }));
	}

	[Test]
	public async Task ListInvocations_ReflectsRegistry()
	{
		_runner.OnRun = (_, sink) =>
		{
			sink.EmitStatus(InvocationStatusKind.Completed, 0);
			return Task.CompletedTask;
		};

		_observer.Invoke(LauncherId, "first");
		_observer.Invoke(LauncherId, "second");
		await Task.Delay(100); // both runner tasks should be done

		var list = _observer.ListInvocations(LauncherId).Items!;
		Assert.That(list, Has.Length.EqualTo(2));
		Assert.That(list.Select(s => s.CommandLine), Is.EquivalentTo(new[] { "first", "second" }));
	}

	[Test]
	public async Task CancelInvocation_RunningInvocation_TripsTokenAndEmitsStatus()
	{
		var cancelObserved = new TaskCompletionSource<bool>();
		_runner.OnRun = async (inv, sink) =>
		{
			try
			{
				await Task.Delay(Timeout.Infinite, inv.CancellationToken);
			}
			catch (OperationCanceledException)
			{
				cancelObserved.SetResult(true);
				throw;
			}
		};

		var invId = _observer.Invoke(LauncherId, "long").InvocationId!;
		await Task.Delay(50); // runner is now inside Task.Delay(Infinite)

		var cancel = _observer.CancelInvocation(LauncherId, invId);
		Assert.That(cancel.Status, Is.EqualTo(SandboxRouteStatus.Ok));

		Assert.That(await cancelObserved.Task.WaitAsync(TimeSpan.FromSeconds(2)), Is.True);

		// After the runner unwinds, the observer's catch block records Cancelled.
		await WaitForCondition(() =>
		{
			var inv = _observer.Invocations.Get(invId);
			return inv?.Status == InvocationStatusKind.Cancelled;
		}, TimeSpan.FromSeconds(2));
	}

	[Test]
	public async Task CancelInvocation_TerminalInvocation_Returns409()
	{
		_runner.OnRun = (_, sink) =>
		{
			sink.EmitStatus(InvocationStatusKind.Completed, 0);
			return Task.CompletedTask;
		};
		var id = _observer.Invoke(LauncherId, "fast").InvocationId!;
		await Task.Delay(50);

		var cancel = _observer.CancelInvocation(LauncherId, id);
		Assert.That((int)cancel.Status, Is.EqualTo(409));
		Assert.That(cancel.Error, Is.EqualTo("already-terminal"));
	}

	[Test]
	public void PumpBatcher_WithoutNotificationsBound_DoesNotThrow()
	{
		// Notifications API isn't bound — flush should still return the batch the
		// caller can inspect, and not throw because of the null sink.
		_observer.Invoke(LauncherId, "anything");
		_clock.Advance(TimeSpan.FromSeconds(2));
		Assert.DoesNotThrow(() => _observer.PumpBatcher());
	}

	private static async Task WaitForCondition(Func<bool> check, TimeSpan timeout)
	{
		var deadline = DateTimeOffset.UtcNow + timeout;
		while (DateTimeOffset.UtcNow < deadline)
		{
			if (check()) return;
			await Task.Delay(20);
		}
		Assert.Fail($"Condition never became true within {timeout}");
	}
}

internal sealed class CapturingRunner : IInvocationRunner
{
	public Func<Invocation, IInvocationSink, Task> OnRun = (_, sink) =>
	{
		sink.EmitStatus(InvocationStatusKind.Completed, exitCode: 0);
		return Task.CompletedTask;
	};

	private readonly TaskCompletionSource<bool> _completed = new();
	public Task Completed => _completed.Task;

	public async Task RunAsync(Invocation invocation, IInvocationSink sink, CancellationToken token)
	{
		try
		{
			await OnRun(invocation, sink);
		}
		finally
		{
			_completed.TrySetResult(true);
		}
	}
}
