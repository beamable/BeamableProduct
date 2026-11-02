using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;

namespace Beamable.Server;

/// <summary>
/// One dedicated process thread samples all connections and pool progress. Socket recovery remains owned by
/// SocketRequesterContext. No process replacement or self-exit policy is enabled by this monitor.
/// </summary>
public class ProcessHealthMonitor
{
    private readonly IReadOnlyList<IBeamableService> _services;
    private readonly int _expectedInstances;
    private readonly ProcessHealthOptions _options;
    private readonly Action<Action> _queueHeartbeat;
    private readonly Func<long, IReadOnlyList<InstanceProcessHealth>> _sample;
    private readonly object _lifetime = new();
    private readonly object _sampling = new();
    private readonly ManualResetEvent _stop = new(false);
    private readonly IConnection[] _connections;
    private readonly long[] _generations;
    private readonly Episode[] _episodes;
    private Thread _thread;
    private bool _stopped;
    private bool _armed;
    private Probe _probe;
    private long? _poolRecoverySince;
    private bool _poolUnhealthy;
    private ProcessHealthSnapshot _snapshot = ProcessHealthSnapshot.Starting;
    private readonly ProcessHealthDiagnostics _diagnostics;
    private readonly ProcessHealthSocketChecks _socketChecks;

    private sealed class Probe { public long QueuedAt; public int Acknowledged; }
    private sealed class Episode { public long StartedAt; public long Generation; }

    public ProcessHealthMonitor(IReadOnlyList<IBeamableService> services, int? expectedInstances = null)
        : this(services, expectedInstances ?? services.Count, new ProcessHealthOptions()) { }

    internal ProcessHealthMonitor(IReadOnlyList<IBeamableService> services, int expectedInstances,
        ProcessHealthOptions options, Action<Action> queueHeartbeat = null,
        Func<long, IReadOnlyList<InstanceProcessHealth>> sample = null, ProcessHealthDiagnostics diagnostics = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        if (expectedInstances < 0) throw new ArgumentOutOfRangeException(nameof(expectedInstances));
        _expectedInstances = expectedInstances;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (options.HeartbeatTimeout <= TimeSpan.Zero || options.HeartbeatRecovery < TimeSpan.Zero ||
            options.SocketRecovery <= TimeSpan.Zero || options.SiblingProgress <= TimeSpan.Zero ||
            options.SampleInterval <= TimeSpan.Zero || options.SampleInterval.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(options));
        _queueHeartbeat = queueHeartbeat ?? (action => ThreadPool.QueueUserWorkItem(_ => action()));
        _sample = sample ?? CaptureInstances;
        _connections = new IConnection[expectedInstances];
        _generations = new long[expectedInstances];
        _episodes = new Episode[expectedInstances];
        _diagnostics = diagnostics ?? new ProcessHealthDiagnostics();
        _socketChecks = new ProcessHealthSocketChecks(CheckSockets);
    }

    public ProcessHealthSnapshot Snapshot => Volatile.Read(ref _snapshot);
    public ProcessHealthSnapshot LastIncident => _diagnostics.LastIncident;
    public long DroppedDiagnosticRecords => _diagnostics.DroppedRecords;
    public long FailedDiagnosticRecords => _diagnostics.FailedRecords;

    /// <summary>Idempotent; a stopped monitor cannot be restarted.</summary>
    public void Start()
    {
        lock (_lifetime)
        {
            if (_stopped || _thread != null) return;
            _diagnostics.Start();
            _socketChecks.Start();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            _thread = new Thread(Run) { IsBackground = true, Name = "beam-health-monitor" };
            _thread.Start();
        }
    }

    private void OnProcessExit(object sender, EventArgs args) => Stop();

    /// <summary>Wake immediately and join within one second; never wait on the pool or logging sink.</summary>
    public void Stop()
    {
        Thread thread;
        lock (_lifetime)
        {
            if (_stopped) return;
            Volatile.Write(ref _stopped, true);
            _stop.Set();
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            thread = _thread;
            Volatile.Write(ref _snapshot, Snapshot with { State = ProcessHealthState.Stopping, Reason = "stopping",
                TransitionTimestamp = Stopwatch.GetTimestamp() });
        }
        if (thread != null && thread != Thread.CurrentThread) thread.Join(TimeSpan.FromSeconds(1));
        _diagnostics.Stop();
        _socketChecks.Stop();
    }

    private void Run()
    {
        while (!_stop.WaitOne(0))
        {
            try { Check(Stopwatch.GetTimestamp()); }
            catch (Exception)
            {
                // Socket sampling must not hide pool starvation or claim recovery from a prior incident.
                var now = Stopwatch.GetTimestamp();
                var age = CheckHeartbeat(now);
                var prior = Snapshot;
                Publish(prior with
                {
                    State = _poolUnhealthy || prior.State == ProcessHealthState.Unhealthy
                        ? ProcessHealthState.Unhealthy : ProcessHealthState.Degraded,
                    Reason = _poolUnhealthy ? "thread_pool_heartbeat_missed" :
                        prior.State == ProcessHealthState.Unhealthy ? prior.Reason : "monitor_sample_failed",
                    HeartbeatAgeSeconds = age,
                    TransitionTimestamp = now
                });
            }
            _stop.WaitOne(_options.SampleInterval);
        }
    }

    private IReadOnlyList<InstanceProcessHealth> CaptureInstances(long now)
    {
        var result = new InstanceProcessHealth[_expectedInstances];
        for (var i = 0; i < result.Length; i++)
        {
            var service = i < _services.Count ? _services[i] : null;
            var context = service?.SocketContext;
            var connection = context?.CurrentConnection;
            // Retain the last identity across reconnect gaps. A replacement gets a new observed generation.
            if (connection != null && !ReferenceEquals(connection, _connections[i]))
            {
                _connections[i] = connection;
                _generations[i]++;
            }
            var health = context?.GetHealth(now);
            var authenticated = context?.IsCurrentConnectionAuthenticated ?? false;
            result[i] = new InstanceProcessHealth(i, service?.HasInitialized ?? false,
                connection?.State == WebSocketState.Open, authenticated,
                _generations[i], health?.PendingRequests ?? 0, health?.OldestPendingRequest.TotalSeconds ?? 0,
                health?.SinceLastReply?.TotalSeconds, health?.WaitingForReply?.TotalSeconds,
                health?.ReportedBroken ?? false);
        }
        return result;
    }

    /// <summary>One sampling round using monotonic Stopwatch ticks.</summary>
    internal void Check(long now)
    {
        lock (_sampling)
        {
            if (Volatile.Read(ref _stopped)) return;
            _socketChecks.Request();
            var instances = _sample(now).ToArray();
            var initialized = instances.Length == _expectedInstances && instances.Length > 0 && instances.All(x => x.Initialized);
            _armed |= initialized;
            var heartbeatAge = CheckHeartbeat(now);
            var state = ProcessHealthState.Starting;
            var reason = "initializing";
            if (_armed)
            {
                state = ProcessHealthState.Healthy;
                reason = "progress_observed";
                for (var i = 0; i < instances.Length; i++)
                {
                    var instance = instances[i];
                    var problem = !instance.Initialized || !instance.Connected || !instance.Authenticated || instance.ReportedBroken;
                    var episode = _episodes[i];
                    var replySinceEpisode = episode != null && instance.ReplyAgeSeconds.HasValue &&
                        instance.ReplyAgeSeconds.Value < Seconds(episode.StartedAt, now);
                    var authenticatedReplacement = episode != null && instance.Generation != episode.Generation &&
                        instance.Connected && instance.Authenticated && !instance.ReportedBroken;
                    if (replySinceEpisode || authenticatedReplacement || !problem)
                        _episodes[i] = episode = null;
                    if (problem)
                    {
                        episode ??= _episodes[i] = new Episode { StartedAt = now, Generation = instance.Generation };
                        var age = Seconds(episode.StartedAt, now);
                        instances[i] = instance with { RecoverySeconds = age };
                        if (state != ProcessHealthState.Unhealthy)
                        {
                            state = ProcessHealthState.Degraded;
                            reason = "socket_recovery_pending";
                        }
                        var siblingProgress = instances.Any(x => x.Index != instance.Index && x.Initialized &&
                            x.Connected && x.Authenticated && !x.ReportedBroken &&
                            x.ReplyAgeSeconds.HasValue && x.ReplyAgeSeconds.Value <= _options.SiblingProgress.TotalSeconds);
                        if (age >= _options.SocketRecovery.TotalSeconds && siblingProgress)
                        {
                            state = ProcessHealthState.Unhealthy;
                            reason = "isolated_socket_recovery_exhausted";
                        }
                    }
                }
                if (_poolUnhealthy)
                {
                    state = ProcessHealthState.Unhealthy;
                    reason = "thread_pool_heartbeat_missed";
                }
            }
            Publish(new ProcessHealthSnapshot(state, reason, now, heartbeatAge, ThreadPool.PendingWorkItemCount,
                ThreadPool.CompletedWorkItemCount, ThreadPool.ThreadCount, Array.AsReadOnly(instances), null));
        }
    }

    private double CheckHeartbeat(long now)
    {
        if (!_armed) return 0;
        var acknowledged = _probe == null || Volatile.Read(ref _probe.Acknowledged) != 0;
        var age = acknowledged ? 0 : Seconds(_probe.QueuedAt, now);
        if (!acknowledged && age >= _options.HeartbeatTimeout.TotalSeconds)
        {
            _poolUnhealthy = true;
            _poolRecoverySince = null;
        }
        else if (_poolUnhealthy)
        {
            _poolRecoverySince ??= now;
            if (Seconds(_poolRecoverySince.Value, now) >= _options.HeartbeatRecovery.TotalSeconds)
                _poolUnhealthy = false;
        }
        if (acknowledged)
        {
            var probe = new Probe { QueuedAt = now };
            _probe = probe;
            _queueHeartbeat(() => Volatile.Write(ref probe.Acknowledged, 1));
        }
        return age;
    }

    private void CheckSockets()
    {
        var now = Stopwatch.GetTimestamp();
        for (var i = 0; i < _services.Count && !Volatile.Read(ref _stopped); i++)
            _services[i]?.SocketContext?.CheckForStall(now);
    }

    private void Publish(ProcessHealthSnapshot next)
    {
        lock (_lifetime)
        {
            if (_stopped) return;
            var prior = Snapshot;
            var transition = next.State != prior.State || next.Reason != prior.Reason;
            next = next with
            {
                TransitionTimestamp = transition ? next.TransitionTimestamp : prior.TransitionTimestamp,
                IncidentId = next.State == ProcessHealthState.Unhealthy ? prior.IncidentId ?? Guid.NewGuid().ToString("N") : null
            };
            Volatile.Write(ref _snapshot, next);
            if (transition) _diagnostics.Record(prior, next);
        }
    }

    private static double Seconds(long from, long to) => Math.Max(0, (to - from) / (double)Stopwatch.Frequency);
}
