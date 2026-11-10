using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace tests;

public partial class ContentPsNoAutoSyncWatchTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Cancellation_drains_an_inflight_publication(bool history, bool repeat)
    {
        await using var fixture = new Fixture(false, history: history, holdPublication: true);
        await fixture.Start();
        await fixture.Server.Publish();
        await fixture.PublicationEntered.Task.WaitAsync(Timeout);
        fixture.Cancel();
        if (repeat) fixture.Cancel();
        try
        {
            // A negative completion check is bounded; the producer remains held until finally.
            var settled = await Task.WhenAny(fixture.Completion, Task.Delay(250));
            Assert.That(settled, Is.Not.SameAs(fixture.Completion), "Watcher returned before its publication handler was released.");
        }
        finally { fixture.ReleasePublication(); }
        await fixture.Completion.WaitAsync(Timeout);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Publication_failure_terminates_the_watcher_and_its_siblings(bool history)
    {
        var fixture = new Fixture(false, history: history, holdPublication: true);
        try
        {
            await fixture.Start();
            await fixture.Server.Publish();
            await fixture.PublicationEntered.Task.WaitAsync(Timeout);
            fixture.HeldPublication.CompleteError(new InvalidOperationException("controlled manifest failure"));
            var error = Assert.ThrowsAsync<InvalidOperationException>(async () => await fixture.Completion.WaitAsync(Timeout));
            Assert.That(error!.Message, Is.EqualTo("controlled manifest failure"));
        }
        finally
        {
            try { await fixture.DisposeAsync(); }
            catch (InvalidOperationException error) when (error.Message == "controlled manifest failure") { }
        }
    }
}
