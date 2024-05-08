namespace cli.Services;

public class AppLifecycle
{
	public CancellationTokenSource Source { get; set; } = new CancellationTokenSource();

	public CancellationToken CancellationToken => Source.Token;

	public bool IsCancelled => Source.IsCancellationRequested;

	public void Cancel() => Source.Cancel();
	
}
