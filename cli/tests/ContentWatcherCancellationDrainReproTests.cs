using System;
using cli.Content;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using cli.Services;
using NUnit.Framework;

namespace tests;

public class ContentWatcherCancellationDrainReproTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [Test]
    public async Task Watcher_completion_does_not_drain_an_already_started_remote_sync()
    {
        // Reuse the existing loopback-only fixture without modifying it or duplicating its protocol server.
        var fixtureType = typeof(ContentPsNoAutoSyncWatchTests).GetNestedType("Fixture", BindingFlags.NonPublic)!;
        var fixture = Activator.CreateInstance(fixtureType, Fields, null, new object[] { false, false, false }, null)!;
        await using var cleanup = (IAsyncDisposable)fixture;
        await (Task)fixtureType.GetMethod("Start")!.Invoke(fixture, null)!;
        var service = (ContentService)Get(fixture, "service");
        var lifecycle = (AppLifecycle)Get(fixture, "lifecycle");
        var command = (Task)Get(fixture, "commandTask");
        var server = Get(fixture, "Server");
        var semaphore = (SemaphoreSlim)Get(service, "_fileSystemOperationSemaphore");
        await semaphore.WaitAsync();
        var held = true;
        try
        {
            await (Task)server.GetType().GetMethod("Publish")!.Invoke(server, null)!;
            // Test-only runtime inspection makes the gate deterministic: the real sync has completed
            // reaching its production filesystem semaphore during local-state analysis.
            var asyncHead = typeof(SemaphoreSlim).GetField("m_asyncHead", Fields)
                ?? typeof(SemaphoreSlim).GetField("_asyncHead", Fields);
            Assert.That(asyncHead, Is.Not.Null, "Unsupported runtime: cannot observe the controlled semaphore gate.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (asyncHead!.GetValue(semaphore) == null) await Task.Delay(10, timeout.Token);


            lifecycle.Cancel();
            await command.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.That(command.IsCompletedSuccessfully, Is.True);
            Assert.That(asyncHead.GetValue(semaphore), Is.Not.Null,
                "The watcher returned while its remote sync was still waiting to enter the write phase.");

            semaphore.Release();
            held = false;
            // Wait only for the local-state critical section; the handler itself is not exposed.
            await semaphore.WaitAsync(timeout.Token);
            semaphore.Release();
            TestContext.Progress.WriteLine("CONFIRMED: content ps completed after cancellation while a publication-triggered operation remained queued at its filesystem semaphore. This proves missing drainage, not a late write.");
        }
        finally { if (held) semaphore.Release(); }
    }

    private static object Get(object instance, string name) => instance.GetType().GetField(name, Fields)!.GetValue(instance)!;
}



