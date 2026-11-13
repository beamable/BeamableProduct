using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Beamable.Server;

[JsonConverter(typeof(StringEnumConverter))]
public enum ProcessHealthState { Starting, Healthy, Degraded, Unhealthy, Stopping }

/// <summary>Bounded metadata only: no request payloads, identities, or credentials.</summary>
public sealed record InstanceProcessHealth(int Index, bool Initialized, bool Connected, bool Authenticated,
    long Generation, int PendingRequests, double OldestPendingSeconds, double? ReplyAgeSeconds,
    double? UnansweredSeconds, bool ReportedBroken, double? RecoverySeconds = null);

/// <summary>Immutable observation; never changes the legacy HTTP status or exits the process.</summary>
public sealed record ProcessHealthSnapshot(ProcessHealthState State, string Reason, long TransitionTimestamp,
    double HeartbeatAgeSeconds, long PoolPendingWork, long PoolCompletedWork, int PoolThreads,
    IReadOnlyList<InstanceProcessHealth> Instances, string IncidentId)
{
    public bool ObservationOnly => true;
    public long SocketCheckFailures { get; init; }
    public string LastSocketCheckErrorType { get; init; }
    public static ProcessHealthSnapshot Starting { get; } = new(ProcessHealthState.Starting, "initializing", 0,
        0, 0, 0, 0, Array.AsReadOnly(Array.Empty<InstanceProcessHealth>()), null);
}

/// <summary>Initial observation values, not approved production replacement policies.</summary>
internal sealed class ProcessHealthOptions
{
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan HeartbeatRecovery { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan SocketRecovery { get; init; } = TimeSpan.FromSeconds(120);
    public TimeSpan SiblingProgress { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan SampleInterval { get; init; } = TimeSpan.FromSeconds(1);
    /// <summary>How long no instance may have a live connection before an error reports it, and how often it repeats.</summary>
    public TimeSpan NoLiveConnectionsAlarm { get; init; } = TimeSpan.FromSeconds(30);
}
