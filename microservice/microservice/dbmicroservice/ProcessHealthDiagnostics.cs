using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using ZLogger;

namespace Beamable.Server;

/// <summary>
/// Bounded transition delivery on one dedicated thread. A slow or broken sink cannot hold the watchdog.
/// Keep the latest incident in memory even if the diagnostic queue or sink is unavailable.
/// </summary>
internal sealed class ProcessHealthDiagnostics : IDisposable
{
    /// <summary><paramref name="Alarm"/> is the text of an error that is not a transition, or null for a transition.</summary>
    private sealed record Transition(ProcessHealthSnapshot Snapshot, bool Incident, bool Recovered, string Alarm = null);
    private readonly BlockingCollection<Transition> _pending = new(8);
    private readonly Action<bool, string> _write;
    private readonly object _lifetime = new();
    private Thread _thread;
    private bool _stopped;
    private long _dropped;
    private long _failed;
    // 1 while an alarm waits in the queue.
    private int _alarmQueued;
    private ProcessHealthSnapshot _lastIncident;

    internal ProcessHealthDiagnostics(Action<bool, string> write = null)
    {
        _write = write ?? ((error, text) =>
        {
            if (error) BeamableZLoggerProvider.LogContext.Value.ZLogError($"{text}");
            else BeamableZLoggerProvider.LogContext.Value.ZLogInformation($"{text}");
        });
    }

    internal ProcessHealthSnapshot LastIncident => Volatile.Read(ref _lastIncident);
    internal int PendingRecords => _pending.Count;
    internal long DroppedRecords => Interlocked.Read(ref _dropped);
    internal long FailedRecords => Interlocked.Read(ref _failed);


    internal void Start()
    {
        lock (_lifetime)
        {
            if (_stopped || _thread != null) return;
            _thread = new Thread(Run) { IsBackground = true, Name = "beam-health-diagnostics" };
            _thread.Start();
        }
    }

    internal void Record(ProcessHealthSnapshot prior, ProcessHealthSnapshot next)
    {
        if (prior.State == next.State && prior.Reason == next.Reason && prior.IncidentId == next.IncidentId) return;
        var incident = next.IncidentId != null && next.IncidentId != prior.IncidentId;
        var recovered = prior.IncidentId != null && next.IncidentId == null && next.State == ProcessHealthState.Healthy;
        if (incident) Volatile.Write(ref _lastIncident, next);
        Enqueue(new Transition(next, incident, recovered));
    }

    /// <summary>
    /// Queues an error for a condition that lasts, which the caller repeats while it does. It is bounded and dropped
    /// like a transition record, so it can't hold the watchdog either. Only one waits in the queue at a time: behind a
    /// slow sink the repeats would otherwise fill it, and the record of an incident would be the one that is dropped.
    /// </summary>
    internal void Alarm(string text, ProcessHealthSnapshot snapshot)
    {
        if (Interlocked.Exchange(ref _alarmQueued, 1) == 1)
        {
            Interlocked.Increment(ref _dropped);
            return;
        }

        if (!Enqueue(new Transition(snapshot, false, false, text)))
            Volatile.Write(ref _alarmQueued, 0);
    }

    /// <summary>Returns whether the record was queued. One that was not is counted as dropped, unless this has stopped.</summary>
    private bool Enqueue(Transition record)
    {
        if (Volatile.Read(ref _stopped)) return false;
        try
        {
            if (_pending.TryAdd(record)) return true;
        }
        catch (InvalidOperationException)
        {
            // the queue was completed by a concurrent stop.
        }

        Interlocked.Increment(ref _dropped);
        return false;
    }

    private void Run()
    {
        while (!_pending.IsCompleted)
        {
            try
            {
                if (_pending.TryTake(out var transition, Timeout.Infinite))
                {
                    if (transition.Alarm != null) Volatile.Write(ref _alarmQueued, 0);
                    var prefix = transition.Alarm ??
                                 (transition.Recovered ? "Process health recovered: " : "Process health transition: ");
                    _write(transition.Incident || transition.Alarm != null, prefix + Format(transition.Snapshot));
                    if (transition.Incident)
                    {
                        // Do not confuse this worker's Environment.StackTrace with other managed threads.
                        // Image support, collection permissions and durable storage precede enabling a collector.
                        _write(false, $"Process health incident {transition.Snapshot.IncidentId}: managed_stacks_unavailable; collector_not_validated");
                    }
                }
            }
            catch (Exception) { Interlocked.Increment(ref _failed); }
        }
    }

    internal static string Format(ProcessHealthSnapshot snapshot) => JsonConvert.SerializeObject(new
    {
        snapshot.ObservationOnly,
        snapshot.State,
        snapshot.Reason,
        snapshot.IncidentId,
        snapshot.TransitionTimestamp,
        snapshot.HeartbeatAgeSeconds,
        snapshot.PoolPendingWork,
        snapshot.PoolCompletedWork,
        snapshot.PoolThreads,
        snapshot.SocketCheckFailures,
        snapshot.LastSocketCheckErrorType,
        InstanceCount = snapshot.Instances.Count,
        Truncated = snapshot.Instances.Count > 32,
        Instances = snapshot.Instances.OrderByDescending(x => !x.Connected || !x.Authenticated || x.ReportedBroken).Take(32)
    });

    internal void Stop()
    {
        Thread thread;
        lock (_lifetime)
        {
            if (_stopped) return;
            Volatile.Write(ref _stopped, true);
            _pending.CompleteAdding();
            thread = _thread;
        }
        if (thread != null && thread != Thread.CurrentThread) thread.Join(TimeSpan.FromMilliseconds(250));
    }

    public void Dispose() => Stop();
}
