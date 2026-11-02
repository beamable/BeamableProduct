using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice;

[TestFixture]
public class ProcessHealthMonitorTests
{
    private static long At(double seconds) => 1 + (long)(seconds * Stopwatch.Frequency);
    private static InstanceProcessHealth Instance(int index, bool initialized = true, long generation = 1,
        bool connected = true, bool authenticated = true, double? replyAge = null,
        double? unanswered = null, bool broken = false) =>
        new(index, initialized, connected, authenticated, generation, 0, 0, replyAge, unanswered, broken);

    private sealed class Harness : IDisposable
    {
        public InstanceProcessHealth[] Instances = { Instance(0), Instance(1) };
        public readonly List<Action> Probes = new();
        public readonly ProcessHealthMonitor Monitor;
        public Harness(ProcessHealthDiagnostics diagnostics = null)
        {
            Monitor = new ProcessHealthMonitor(Array.Empty<IBeamableService>(), 2,
                new ProcessHealthOptions(), action => Probes.Add(action), _ => Instances, diagnostics);
        }
        public void Check(double seconds, bool acknowledge = true)
        {
            if (acknowledge && Probes.Count > 0) Probes[^1]();
            Monitor.Check(At(seconds));
        }
        public void Dispose() => Monitor.Stop();
    }

    [Test]
    public void MissingNonPrimaryInitialization_DoesNotArmHeartbeat()
    {
        using var h = new Harness();
        h.Instances[1] = Instance(1, initialized: false);
        h.Check(0);
        h.Check(90);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Starting));
        Assert.That(h.Probes, Is.Empty);
        h.Instances[1] = Instance(1);
        h.Check(91);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy));
        Assert.That(h.Probes.Count, Is.EqualTo(1));
    }

    [Test]
    public void MissedHeartbeat_IsUnhealthy_WithOneOutstandingProbe_AndHystereticRecovery()
    {
        using var h = new Harness();
        h.Check(0);
        h.Check(29, false);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy));
        h.Check(30, false);
        Assert.That(h.Monitor.Snapshot.Reason, Is.EqualTo("thread_pool_heartbeat_missed"));
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Unhealthy));
        Assert.That(h.Monitor.Snapshot.HeartbeatAgeSeconds, Is.EqualTo(30));
        h.Check(60, false);
        Assert.That(h.Probes.Count, Is.EqualTo(1));
        var incident = h.Monitor.Snapshot.IncidentId;
        h.Check(61);
        h.Check(65);
        Assert.That(h.Monitor.Snapshot.IncidentId, Is.EqualTo(incident));
        h.Check(66);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy));
        h.Check(96, false);
        Assert.That(h.Monitor.Snapshot.IncidentId, Is.Not.EqualTo(incident));
    }

    [Test]
    public void HeartbeatRecovery_RequiresProgressThroughoutTheRecoveryInterval()
    {
        using var h = new Harness();
        h.Check(0);
        h.Check(30, false);
        var incident = h.Monitor.Snapshot.IncidentId;
        h.Check(31);
        // A single worker was released, but the next heartbeat is still stuck.
        h.Check(36, false);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Unhealthy));
        Assert.That(h.Monitor.Snapshot.IncidentId, Is.EqualTo(incident));
        h.Check(37);
        h.Check(38);
        h.Check(39, false);
        h.Check(40);
        h.Check(41);
        h.Check(42);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Unhealthy),
            "missing another acknowledgement restarts the recovery interval");
        h.Check(43);
        h.Check(44);
        h.Check(45);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy));
        Assert.That(h.Monitor.Snapshot.IncidentId, Is.Null);
    }

    [Test]
    public void Stop_IsTerminal_EvenWhenQueuedHeartbeatAcknowledgesLater()
    {
        using var h = new Harness();
        h.Check(0);
        h.Monitor.Stop();
        h.Monitor.Stop();
        h.Check(60);
        h.Monitor.Start();
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Stopping));
        Assert.That(h.Probes.Count, Is.EqualTo(1));
    }

    [Test]
    public void Snapshot_IsStable_WhenTheNextSampleChanges()
    {
        using var h = new Harness();
        h.Check(0);
        var prior = h.Monitor.Snapshot;
        h.Instances[1] = Instance(1, connected: false);
        h.Check(1);
        Assert.That(prior.Instances[1].Connected, Is.True);
        Assert.That(h.Monitor.Snapshot.Instances[1].Connected, Is.False);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Degraded));
    }

    [Test]
    public void NonPrimaryFailure_EscalatesOnlyWithRecentSiblingReplies_AcrossFailedReconnections()
    {
        using var h = new Harness();
        h.Check(0);
        h.Instances[0] = Instance(0, replyAge: 0);
        h.Instances[1] = Instance(1, broken: true, unanswered: 60);
        h.Check(1);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Degraded));
        h.Instances[1] = Instance(1, generation: 2, authenticated: false);
        h.Check(61);
        h.Instances[1] = Instance(1, generation: 3, connected: false, authenticated: false);
        h.Check(121);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Unhealthy));
        Assert.That(h.Monitor.Snapshot.Reason, Is.EqualTo("isolated_socket_recovery_exhausted"));
        Assert.That(h.Monitor.Snapshot.Instances[1].RecoverySeconds, Is.EqualTo(120));
    }

    [Test]
    public void StaleSiblingEvidence_DoesNotCloseOrRecaptureAnUnresolvedIncident()
    {
        var records = new ConcurrentQueue<(bool error, string text)>();
        using var diagnostics = new ProcessHealthDiagnostics((error, text) => records.Enqueue((error, text)));
        diagnostics.Start();
        using var h = new Harness(diagnostics);
        h.Instances[0] = Instance(0, replyAge: 0);
        h.Instances[1] = Instance(1, broken: true);
        h.Check(0);
        h.Check(120);
        var incident = h.Monitor.Snapshot.IncidentId;
        var firstEvidence = h.Monitor.LastIncident;
        Assert.That(incident, Is.Not.Null);

        h.Instances[0] = Instance(0, replyAge: 31);
        h.Check(151);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Degraded),
            "stale corroborating evidence makes the verdict ambiguous, not recovered");
        Assert.That(h.Monitor.Snapshot.IncidentId, Is.EqualTo(incident));
        h.Instances[0] = Instance(0, replyAge: 0);
        h.Check(152);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Unhealthy));
        Assert.That(h.Monitor.Snapshot.IncidentId, Is.EqualTo(incident));
        Assert.That(h.Monitor.LastIncident, Is.SameAs(firstEvidence));

        h.Instances[1] = Instance(1, generation: 2);
        h.Check(153);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy));
        Assert.That(h.Monitor.Snapshot.IncidentId, Is.Null);
        Assert.That(SpinWait.SpinUntil(() => records.Any(x => x.text.Contains("recovered")), 2000), Is.True);
        Assert.That(records.Count(x => x.error), Is.EqualTo(1));
        Assert.That(records.Count(x => x.text.Contains("recovered")), Is.EqualTo(1));
        Assert.That(records.Single(x => x.text.Contains("recovered")).text, Does.Contain("Healthy"));
    }

    [Test]
    public void AuthenticatedReplacement_EndsRecoveryEpisode()
    {
        using var h = new Harness();
        h.Check(0);
        h.Instances[1] = Instance(1, broken: true);
        h.Check(1);
        h.Instances[1] = Instance(1, generation: 2);
        h.Check(121);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy));
        h.Instances[1] = Instance(1, generation: 2, broken: true);
        h.Check(122);
        Assert.That(h.Monitor.Snapshot.Instances[1].RecoverySeconds, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SharedSilence_OrStaleSiblingProgress_NeverEscalatesSocketSilence(bool bothBroken)
    {
        using var h = new Harness();
        h.Check(0);
        h.Instances[0] = Instance(0, replyAge: 90, broken: bothBroken);
        h.Instances[1] = Instance(1, broken: true);
        h.Check(1);
        h.Check(500);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Degraded));
        Assert.That(h.Monitor.Snapshot.Reason, Is.EqualTo("socket_recovery_pending"));
    }

    [Test]
    public void IdleAndLegitimatelySlowRequests_AreNotProcessFailureEvidence()
    {
        using var h = new Harness();
        h.Check(0);
        h.Instances[1] = Instance(1, replyAge: 1000, unanswered: 1000);
        h.Check(1000);
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy),
            "upstream stall policy must report a broken socket; request age alone is insufficient");
    }

    [Test]
    public void SingleSocketFailure_RemainsAmbiguous_AfterRecoveryWindow()
    {
        var instances = new[] { Instance(0, broken: true) };
        var monitor = new ProcessHealthMonitor(Array.Empty<IBeamableService>(), 1,
            new ProcessHealthOptions(), action => action(), _ => instances);
        try
        {
            monitor.Check(At(0));
            monitor.Check(At(1000));
            Assert.That(monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Degraded));
            Assert.That(monitor.Snapshot.IncidentId, Is.Null);
        }
        finally { monitor.Stop(); }
    }

    [Test]
    public void ConcurrentHeartbeatAcknowledgement_DoesNotQueueMultipleOutstandingProbes()
    {
        using var h = new Harness();
        h.Check(0);
        for (var i = 1; i < 100; i++)
        {
            var acknowledge = new Thread(() => h.Probes[^1]());
            acknowledge.Start();
            h.Check(i, false);
            acknowledge.Join();
            h.Check(i + 0.1, false);
        }
        Assert.That(h.Monitor.Snapshot.State, Is.EqualTo(ProcessHealthState.Healthy));
        Assert.That(h.Probes.Count, Is.LessThanOrEqualTo(100));
    }

    [Test]
    public void FailingSocketSampling_CannotHideAMissedPoolHeartbeat()
    {
        var samples = 0;
        using var diagnostics = new ProcessHealthDiagnostics((_, _) => { });
        var monitor = new ProcessHealthMonitor(Array.Empty<IBeamableService>(), 1,
            new ProcessHealthOptions { HeartbeatTimeout = TimeSpan.FromMilliseconds(50), SampleInterval = TimeSpan.FromMilliseconds(10) },
            _ => { }, _ => Interlocked.Increment(ref samples) == 1 ? new[] { Instance(0) } : throw new InvalidOperationException(), diagnostics);
        try
        {
            monitor.Start();
            Assert.That(SpinWait.SpinUntil(() => monitor.Snapshot.State == ProcessHealthState.Unhealthy, 1000), Is.True);
            Assert.That(monitor.Snapshot.Reason, Is.EqualTo("thread_pool_heartbeat_missed"));
            var incident = monitor.Snapshot.IncidentId;
            Thread.Sleep(30);
            Assert.That(monitor.Snapshot.IncidentId, Is.EqualTo(incident));
        }
        finally { monitor.Stop(); }
    }
}
