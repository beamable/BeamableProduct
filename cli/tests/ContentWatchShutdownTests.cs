using System;
using System.Threading;
using System.Threading.Tasks;
using cli.Services;
using NUnit.Framework;

namespace tests;

public class ContentWatchShutdownTests
{
	private static TaskCompletionSource<bool> Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	[TestCase(false)]
	[TestCase(true)]
	public async Task Cancellation_waits_for_inflight_work_even_when_requested_repeatedly(bool repeat)
	{
		using var cancel = new CancellationTokenSource();
		var scope = new ContentWatchScope(cancel.Token);
		var entered = Signal();
		var release = Signal();
		var finished = false;
		var worker = scope.Run(async () => { entered.SetResult(true); await release.Task; finished = true; });
		await entered.Task.WaitAsync(Timeout);
		cancel.Cancel();
		if (repeat) cancel.Cancel();
		var stop = scope.DisposeAsync().AsTask();
		try { Assert.That(stop.IsCompleted, Is.False, "Shutdown must wait for already-started work."); }
		finally { release.TrySetResult(true); await worker.WaitAsync(Timeout); await stop.WaitAsync(Timeout); }
		Assert.That(finished, Is.True);
		await scope.DisposeAsync();
	}

	[Test]
	public async Task Worker_failure_cancels_siblings_and_waits_for_cleanup_before_reporting_error()
	{
		var scope = new ContentWatchScope(CancellationToken.None);
		var entered = Signal();
		var cancelled = Signal();
		var release = Signal();
		using var registration = scope.Token.Register(() => cancelled.TrySetResult(true));
		var sibling = scope.Run(async () => { entered.SetResult(true); await release.Task; });
		await entered.Task.WaitAsync(Timeout);
		var error = new InvalidOperationException("publication failed");
		var failure = scope.Run(() => Task.FromException(error));
		try
		{
			await cancelled.Task.WaitAsync(Timeout);
			var stop = scope.DisposeAsync().AsTask();
			Assert.That(stop.IsCompleted, Is.False);
			release.SetResult(true);
			var observed = Assert.ThrowsAsync<InvalidOperationException>(async () => await stop.WaitAsync(Timeout));
			Assert.That(observed, Is.SameAs(error));
		}
		finally
		{
			release.TrySetResult(true);
			await sibling.WaitAsync(Timeout);
			try { await failure.WaitAsync(Timeout); } catch (InvalidOperationException) { }
			try { await scope.DisposeAsync(); } catch (InvalidOperationException) { }
		}
	}

	[Test]
	public async Task Notifications_after_shutdown_do_not_start_work()
	{
		var scope = new ContentWatchScope(CancellationToken.None);
		await scope.DisposeAsync();
		var invoked = false;
		await scope.Run(() => { invoked = true; return Task.CompletedTask; });
		Assert.That(invoked, Is.False);
	}
}
