using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

[assembly: InternalsVisibleTo("tests")]

namespace cli.Services;

internal sealed class ContentWatchScope : IAsyncDisposable
{
	private readonly CancellationTokenSource _source;
	private readonly HashSet<Task> _active = new();
	private ExceptionDispatchInfo _failure;
	private Task _shutdown;
	public CancellationToken Token { get; }

	public ContentWatchScope(CancellationToken token)
	{
		_source = CancellationTokenSource.CreateLinkedTokenSource(token);
		Token = _source.Token;
	}

	// Keep existing worker concurrency, but own every task until its work has finished.
	public Task Run(Func<Task> worker)
	{
		lock (_active)
		{
			if (_shutdown != null || Token.IsCancellationRequested) return Task.CompletedTask;
			Task task = null;
			task = Task.Run(async () =>
			{
				try { Token.ThrowIfCancellationRequested(); await worker(); }
				catch (OperationCanceledException) when (Token.IsCancellationRequested) { }
				catch (Exception error)
				{
					lock (_active) _failure ??= ExceptionDispatchInfo.Capture(error);
					_source.Cancel();
				}
				finally { lock (_active) _active.Remove(task); }
			});
			_active.Add(task);
			return task;
		}
	}

	public ValueTask DisposeAsync()
	{
		lock (_active) return new ValueTask(_shutdown ??= StopAsync(_active.ToArray()));
	}

	private async Task StopAsync(Task[] active)
	{
		// Publish the shutdown task before invoking cancellation callbacks or awaiting workers.
		await Task.Yield();
		try
		{
			try { _source.Cancel(); }
			finally { await Task.WhenAll(active); }
			_failure?.Throw();
		}
		finally { _source.Dispose(); }
	}
}
