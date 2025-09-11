namespace cli.Utils;

public sealed class Debouncer
{
    private readonly TimeSpan _delay;
    private readonly Action _action;

    private Timer? _timer;
    private readonly object _lock = new();

    public Debouncer(TimeSpan delay, Action action)
    {
        _delay = delay;
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Schedule the action to run after the debounce delay.
    /// If called again before the delay expires, the timer resets.
    /// </summary>
    public void Signal()
    {
        lock (_lock)
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite); // stop old timer
            _timer?.Dispose();
            _timer = new Timer(_ => _action(), null, _delay, Timeout.InfiniteTimeSpan);
        }
    }

}