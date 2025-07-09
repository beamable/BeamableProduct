namespace cli.Services;

public class AppLifecycle
{
	private readonly TaskCompletionSource _cancelCompleted = new TaskCompletionSource();
	
	public CancellationTokenSource Source { get; set; } = new CancellationTokenSource();

	public CancellationToken CancellationToken => Source.Token;

	public bool IsCancelled => Source.IsCancellationRequested;
	
	public bool ShouldWaitForCancel { get; set; }
	public Task WaitForCancelCompleted() => _cancelCompleted.Task;

	public void Cancel() => Source.Cancel();

	public void MarkCancelCompleted()
	{
		_cancelCompleted.TrySetResult();
	}
	
}
