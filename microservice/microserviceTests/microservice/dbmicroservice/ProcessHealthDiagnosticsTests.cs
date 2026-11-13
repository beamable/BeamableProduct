using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Beamable.Server;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice;

[TestFixture]
public class ProcessHealthDiagnosticsTests
{
    private static ProcessHealthSnapshot Unhealthy(string id) => ProcessHealthSnapshot.Starting with
    {
        State = ProcessHealthState.Unhealthy, Reason = "thread_pool_heartbeat_missed", IncidentId = id
    };

    [Test]
    public void Incident_IsRecordedOnce_AndRecoveryRearmsCapture()
    {
        var records = new ConcurrentQueue<(bool error, string text)>();
        using var diagnostics = new ProcessHealthDiagnostics((error, text) => records.Enqueue((error, text)));
        diagnostics.Start();
        var first = Unhealthy("first");
        diagnostics.Record(ProcessHealthSnapshot.Starting, first);
        var changed = first with { Reason = "isolated_socket_recovery_exhausted" };
        diagnostics.Record(first, changed);
        diagnostics.Record(changed, changed);
        diagnostics.Record(changed, first);
        Assert.That(diagnostics.LastIncident, Is.SameAs(first));
        var recovered = first with { State = ProcessHealthState.Healthy, IncidentId = null };
        diagnostics.Record(first, recovered);
        diagnostics.Record(recovered, Unhealthy("second"));
        Assert.That(SpinWait.SpinUntil(() => records.Any(x => x.text.Contains("incident second:")), 2000), Is.True);
        const string prefix = "Process health transition: ";
        var updates = records.Where(x => !x.error && x.text.StartsWith(prefix, StringComparison.Ordinal))
            .Select(x => JObject.Parse(x.text.Substring(prefix.Length)))
            .Where(x => (string)x["IncidentId"] == first.IncidentId);
        Assert.That(updates.Select(x => (string)x["Reason"]),
            Is.EqualTo(new[] { "isolated_socket_recovery_exhausted", "thread_pool_heartbeat_missed" }));
        Assert.That(records.Count(x => x.error), Is.EqualTo(2));
        Assert.That(records.Count(x => x.text.Contains("managed_stacks_unavailable")), Is.EqualTo(2));
        Assert.That(records.Any(x => x.text.Contains("recovered")), Is.True);
        Assert.That(diagnostics.LastIncident.IncidentId, Is.EqualTo("second"));
    }

    [Test]
    public void BlockedSink_CannotBlockVerdicts_AndPendingRecordsAreBounded()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var diagnostics = new ProcessHealthDiagnostics((_, _) => { entered.Set(); release.Wait(); });
        diagnostics.Start();
        diagnostics.Record(ProcessHealthSnapshot.Starting, Unhealthy("blocked"));
        Assert.That(entered.Wait(2000), Is.True);
        try
        {
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < 1000; i++)
                diagnostics.Record(ProcessHealthSnapshot.Starting, Unhealthy(i.ToString()));
            Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(1)));
            Assert.That(diagnostics.PendingRecords, Is.LessThanOrEqualTo(8));
            Assert.That(diagnostics.DroppedRecords, Is.GreaterThan(0));
            Assert.That(diagnostics.LastIncident.IncidentId, Is.EqualTo("999"));
            watch.Restart();
            diagnostics.Stop();
            Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(1)));
        }
        finally { release.Set(); }
    }

    [Test]
    public void RepeatedAlarms_BehindABlockedSink_LeaveRoomForAnIncident()
    {
        var records = new ConcurrentQueue<(bool error, string text)>();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var diagnostics = new ProcessHealthDiagnostics((error, text) =>
        {
            entered.Set();
            release.Wait();
            records.Enqueue((error, text));
        });
        diagnostics.Start();
        try
        {
            // the sink blocks on the first alarm, and the condition keeps repeating behind it.
            diagnostics.Alarm("alarm 0: ", ProcessHealthSnapshot.Starting);
            Assert.That(entered.Wait(2000), Is.True);
            for (var i = 1; i < 10; i++) diagnostics.Alarm($"alarm {i}: ", ProcessHealthSnapshot.Starting);
            Assert.That(diagnostics.PendingRecords, Is.EqualTo(1), "one alarm waits; the repeats behind it are dropped");
            Assert.That(diagnostics.DroppedRecords, Is.EqualTo(8));

            diagnostics.Record(ProcessHealthSnapshot.Starting, Unhealthy("incident"));
            Assert.That(diagnostics.DroppedRecords, Is.EqualTo(8), "the incident must still fit in the queue");
        }
        finally { release.Set(); }

        Assert.That(SpinWait.SpinUntil(() => records.Any(x => x.text.Contains("incident incident:")), 2000), Is.True);
        Assert.That(records.Count(x => x.text.StartsWith("alarm ", StringComparison.Ordinal)), Is.EqualTo(2));
        Assert.That(records.Any(x => x.error && x.text.StartsWith("Process health transition: ", StringComparison.Ordinal)), Is.True);

        // once the queue has drained, the next alarm is queued again.
        diagnostics.Alarm("alarm after: ", ProcessHealthSnapshot.Starting);
        Assert.That(SpinWait.SpinUntil(() => records.Any(x => x.text.StartsWith("alarm after: ", StringComparison.Ordinal)), 2000), Is.True);
    }

    [Test]
    public void ThrowingSink_DoesNotKillDiagnosticsWorker()
    {
        var calls = 0;
        using var diagnostics = new ProcessHealthDiagnostics((_, _) =>
        {
            Interlocked.Increment(ref calls);
            throw new InvalidOperationException("sink unavailable");
        });
        diagnostics.Start();
        diagnostics.Record(ProcessHealthSnapshot.Starting, Unhealthy("first"));
        diagnostics.Record(ProcessHealthSnapshot.Starting, Unhealthy("second"));
        Assert.That(SpinWait.SpinUntil(() => Volatile.Read(ref calls) >= 2, 2000), Is.True);
        Assert.That(diagnostics.FailedRecords, Is.GreaterThan(0));
    }

    [Test]
    public void SocketCheckFailure_IsRetainedWhileLaterHandoffsContinue()
    {
        using var completed = new ManualResetEventSlim();
        var attempts = 0;
        var worker = new ProcessHealthSocketChecks(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1) throw new InvalidOperationException("not exported");
            completed.Set();
        });
        try
        {
            worker.Start();
            worker.Request();
            Assert.That(SpinWait.SpinUntil(() => worker.FailedChecks == 1, 2000), Is.True);
            worker.Request();
            Assert.That(completed.Wait(2000), Is.True);
            Assert.That(worker.FailedChecks, Is.EqualTo(1));
            Assert.That(worker.LastErrorType, Is.EqualTo("InvalidOperationException"));
        }
        finally { worker.Stop(); }
    }

    [Test]
    public void IncidentOutput_IsBounded_AndPreservesValidJson()
    {
        var record = Unhealthy("bounded") with
        {
            Instances = Array.AsReadOnly(Enumerable.Range(0, 1000).Select(i =>
                new InstanceProcessHealth(i, true, true, true, 1, 0, 0, null, null, false)).ToArray())
        };
        var output = ProcessHealthDiagnostics.Format(record);
        Assert.That(output.Length, Is.LessThanOrEqualTo(16384));
        var json = Newtonsoft.Json.Linq.JObject.Parse(output);
        Assert.That((int)json["InstanceCount"], Is.EqualTo(1000));
        Assert.That((bool)json["Truncated"], Is.True);
    }
}
