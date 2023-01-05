using System.Threading.Tasks;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	[TestFixture]
	public class DependencyInjectionTests : CommonTest
	{

		[Test]
		[NonParallelizable]
		public async Task HandleSimpleTraffic()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<bool>(n => n),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<ServiceWithDependency>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_serviceWithDeps", "NoNulls", 1, 1));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task HandleUnauthorizedClientCallable()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(MessageMatcher
							.WithReqId(1)
							.WithStatus(401),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<ServiceWithDependency>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);


			testSocket.SendToClient(ClientRequest.ClientCallable("micro_serviceWithDeps", "NoNulls", 1, 0));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			allowErrorLogs = true;
			AssertUnauthorizedException();
			Assert.IsTrue(testSocket.AllMocksCalled());
		}


	}
}
