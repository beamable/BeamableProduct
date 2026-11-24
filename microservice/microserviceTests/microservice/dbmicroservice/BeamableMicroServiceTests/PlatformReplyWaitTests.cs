using System;
using System.Threading;
using Beamable.Common;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

[TestFixture]
public class PlatformReplyWaitTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void PlatformRejectionAtDeadlineRetainsItsOriginalError(bool throwDuringSend)
    {
        using var deadline = new CancellationTokenSource();
        var wait = new PlatformReplyWait(CancellationToken.None, TimeSpan.FromMinutes(2));
        var rejection = new WebsocketRequesterException("GET", "basic/realms/config", 500, "{}");

        Promise<int> Send()
        {
            // Expire the attempt after dispatch starts but before the coordinator observes the rejection.
            deadline.Cancel();
            if (throwDuringSend) throw rejection;
            return Promise<int>.Failed(rejection);
        }

        var error = Assert.CatchAsync(async () => await wait.WaitForStartupReply("realm config", Send, deadline.Token));
        Assert.That(error, Is.SameAs(rejection));
    }

    [Test]
    public void UnrelatedCancellationAtDeadlineRetainsItsOwnToken()
    {
        using var deadline = new CancellationTokenSource();
        var otherToken = new CancellationToken(canceled: true);
        var wait = new PlatformReplyWait(CancellationToken.None, TimeSpan.FromMinutes(2));

        var error = Assert.CatchAsync<OperationCanceledException>(async () =>
            await wait.WaitForStartupReply<int>("realm config", () =>
            {
                deadline.Cancel();
                throw new OperationCanceledException(otherToken);
            }, deadline.Token));
        Assert.That(error.CancellationToken, Is.EqualTo(otherToken));
    }
}
