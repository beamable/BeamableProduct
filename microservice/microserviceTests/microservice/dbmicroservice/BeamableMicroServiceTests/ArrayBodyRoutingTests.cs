using System.Threading.Tasks;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Newtonsoft.Json;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

[TestFixture]
public class ArrayBodyRoutingTests : CommonTest
{
    [TestCase("[]", 0)]
    [TestCase("[10,20]", 2)]
    [NonParallelizable]
    public async Task ArrayBody_ReachesParameterlessHandler(string body, int expectedCount)
    {
        TestSocket testSocket = null;
        var setup = new TestSetup(new TestSocketProvider(socket =>
        {
            testSocket = socket;
            socket.AddStandardMessageHandlers().AddMessageHandler(
                MessageMatcher.WithReqId(1).WithStatus(200)
                    .WithPayload<int>(count => count == expectedCount),
                MessageResponder.NoResponse(),
                MessageFrequency.OnlyOnce());
        }));

        try
        {
            await setup.Start<ArrayBodyTestMicroservice>(new TestArgs());
            Assert.That(setup.HasInitialized, Is.True);

            var request = ClientRequest.ClientCallable(
                "micro_arrayBody", nameof(ArrayBodyTestMicroservice.CountItems), 1, 1);
            // Send an actual array in the gateway envelope, not a legacy payload wrapper.
            request.body = JsonConvert.DeserializeObject<int[]>(body);
            testSocket.SendToClient(request);
        }
        finally
        {
            await setup.OnShutdown(this, null);
        }

        Assert.That(testSocket.AllMocksCalled(), Is.True);
    }
}

[Microservice("arrayBody", EnableEagerContentLoading = false)]
public class ArrayBodyTestMicroservice : Microservice
{
    [ClientCallable]
    public int CountItems()
    {
        return JsonConvert.DeserializeObject<int[]>(Context.Body).Length;
    }
}
