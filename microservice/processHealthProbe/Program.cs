using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

// Run as a separate process: this intentionally starves its entire thread pool for 30 seconds.
// Windows: set DOTNET_ThreadPool_UseWindowsThreadPool=0 before launching if the native pool is enabled.
BeamableZLoggerProvider.LogContext.Value = NullLogger.Instance;
ThreadPool.GetMinThreads(out var oldMin, out var minIo);
ThreadPool.GetMaxThreads(out var oldMax, out var maxIo);
using var entered = new ManualResetEventSlim();
using var release = new ManualResetEventSlim();
using var finished = new ManualResetEventSlim();
var monitor = new ProcessHealthMonitor(new IBeamableService[] { new ProbeService() });
try
{
    if (!ThreadPool.SetMinThreads(1, minIo) || !ThreadPool.SetMaxThreads(1, maxIo))
        throw new InvalidOperationException("Runtime rejected a bounded one-worker pool; starvation was not injected.");
    ThreadPool.QueueUserWorkItem(_ =>
    {
        entered.Set();
        release.Wait();
        finished.Set();
    });
    if (!entered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Blocking work did not start.");
    var elapsed = Stopwatch.StartNew();
    monitor.Start();
    monitor.Start();
    WaitFor(() => monitor.Snapshot.State == ProcessHealthState.Unhealthy, TimeSpan.FromSeconds(40));
    Console.WriteLine(JsonConvert.SerializeObject(new { Phase = "starved", Seconds = elapsed.Elapsed.TotalSeconds, monitor.Snapshot }));
    if (monitor.Snapshot.Reason != "thread_pool_heartbeat_missed") throw new Exception("Wrong failure reason.");
    if (monitor.Snapshot.PoolPendingWork < 1) throw new Exception("No real queued pool work was observed.");
    if (monitor.Snapshot.HeartbeatAgeSeconds < 30) throw new Exception("Heartbeat failed before its observation threshold.");
    release.Set();
    WaitFor(() => monitor.Snapshot.State != ProcessHealthState.Unhealthy, TimeSpan.FromSeconds(12));
    Console.WriteLine(JsonConvert.SerializeObject(new { Phase = "recovered", Seconds = elapsed.Elapsed.TotalSeconds, monitor.Snapshot }));
    monitor.Stop();
    if (monitor.Snapshot.State != ProcessHealthState.Stopping) throw new Exception("Stop did not publish a terminal snapshot.");
    Console.WriteLine("PASS: dedicated-thread detection, late pool recovery, idempotent start and bounded stop.");
    return 0;
}
finally
{
    release.Set();
    finished.Wait(TimeSpan.FromSeconds(5));
    ThreadPool.SetMaxThreads(oldMax, maxIo);
    ThreadPool.SetMinThreads(oldMin, minIo);
    monitor.Stop();
}

static void WaitFor(Func<bool> condition, TimeSpan timeout)
{
    var elapsed = Stopwatch.StartNew();
    while (!condition())
    {
        if (elapsed.Elapsed > timeout) throw new TimeoutException("Process verdict did not transition within the probe bound.");
        Thread.Sleep(10);
    }
}

sealed class ProbeService : IBeamableService
{
    public SocketRequesterContext SocketContext => null;
    public bool HasInitialized => true;
    public IDependencyProvider Provider => null;
    public Task OnShutdown(object sender, EventArgs args) => Task.CompletedTask;
}
