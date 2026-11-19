using System;
using System.Threading;

namespace Beamable.Server;

/// <summary>
/// Executes the monitor's coalesced socket-check handoff. No timer or independent detection policy lives here.
/// Upstream checks call synchronous ILogger/metric listeners; those cannot hold sampling or new diagnostic delivery.
/// </summary>
internal sealed class ProcessHealthSocketChecks
{
    private readonly Action _check;
    private readonly AutoResetEvent _requested = new(false);
    private readonly object _lifetime = new();
    private Thread _thread;
    private bool _stopped;
    private long _failedChecks;
    private string _lastErrorType;

    internal long FailedChecks => Interlocked.Read(ref _failedChecks);
    internal string LastErrorType => Volatile.Read(ref _lastErrorType);

    internal ProcessHealthSocketChecks(Action check) => _check = check;
    internal void Start()
    {
        lock (_lifetime)
        {
            if (_stopped || _thread != null) return;
            _thread = new Thread(Run) { IsBackground = true, Name = "beam-health-socket-checks" };
            _thread.Start();
        }
    }

    internal void Request()
    {
        if (!Volatile.Read(ref _stopped)) _requested.Set();
    }

    private void Run()
    {
        while (true)
        {
            _requested.WaitOne();
            if (Volatile.Read(ref _stopped)) return;
            try { _check(); }
            catch (Exception error)
            {
                // Keep bounded evidence without calling the logger/metric listener that may have failed.
                // Messages and stacks can contain request data and are deliberately excluded.
                var name = error.GetType().Name;
                Volatile.Write(ref _lastErrorType, name.Length <= 128 ? name : name.Substring(0, 128));
                Interlocked.Increment(ref _failedChecks);
            }
        }
    }

    internal void Stop()
    {
        Thread thread;
        lock (_lifetime)
        {
            if (_stopped) return;
            Volatile.Write(ref _stopped, true);
            _requested.Set();
            thread = _thread;
        }
        if (thread != null && thread != Thread.CurrentThread) thread.Join(TimeSpan.FromMilliseconds(250));
    }
}
