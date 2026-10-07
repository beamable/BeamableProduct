using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

[TestFixture]
public class TupleCallableRoutingTests : CommonTest
{
    [Test]
    [NonParallelizable]
    public async Task TupleParameter_ReachesCallableThroughGatewayRouting()
    {
        TestSocket socket = null;
        var setup = new TestSetup(new TestSocketProvider(s =>
        {
            socket = s;
            s.AddStandardMessageHandlers().AddMessageHandler(
                MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(value => value == "7:hello"),
                MessageResponder.NoResponse(), MessageFrequency.OnlyOnce());
        }));
        try
        {
            await setup.Start<TupleCallableTestMicroservice>(new TestArgs());
            Assert.That(setup.HasInitialized, Is.True);
            var request = ClientRequest.ClientCallable("micro_tupleCallable", nameof(TupleCallableTestMicroservice.TestTuple), 1, 1);
            // The generated Unity client uses SmallerJSON for these request fields.
            var json = Beamable.Serialization.SmallerJSON.Json.Serialize(
                new Dictionary<string, object> { ["input"] = (7, "hello") }, new StringBuilder());
            request.body = JObject.Parse(json);
            socket.SendToClient(request);
        }
        finally { await setup.OnShutdown(this, null); }
        Assert.That(socket.AllMocksCalled(), Is.True);
    }
}

[Microservice("tupleCallable", EnableEagerContentLoading = false)]
public class TupleCallableTestMicroservice : Microservice
{
    [ClientCallable]
    public async Promise<string> TestTuple((int count, string label) input)
    {
        await Task.Delay(1);
        return $"{input.count}:{input.label}";
    }
}
