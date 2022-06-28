using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Content;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using microserviceTests.microservice.Util;
using NUnit.Framework;
using System.Threading.Tasks;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

[TestFixture]
public class AssumeUserTests
{
	[SetUp]
	[TearDown]
	public void ResetContentInstance()
	{
		ContentApi.Instance = new Promise<IContentApi>();
	}


	[TestCase(true)]
	[TestCase(false)]
	[NonParallelizable]
	public async Task AssumeUserAsAdmin(bool forceCheck)
	{
		LoggingUtil.Init();
		TestSocket testSocket = null;
		var ms = new BeamableMicroService(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(-5)
						.WithFrom(2),
					MessageResponder.Success(new EmptyResponse()),
					MessageFrequency.OnlyOnce())
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(1)
						.WithStatus(200),
					MessageResponder.NoResponse(),
					MessageFrequency.OnlyOnce()
				);
		}));

		await ms.Start<SimpleMicroservice>(new TestArgs());
		Assert.IsTrue(ms.HasInitialized);

		testSocket.SendToClient(ClientRequest.ClientCallableAsAdmin("micro_simple", "TestAssumeUser", 1, 1, 2, forceCheck));

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}

	[Test]
	[NonParallelizable]
	public async Task AssumeUserAsNormal_WithNoCheck()
	{
		LoggingUtil.Init();
		TestSocket testSocket = null;
		var ms = new BeamableMicroService(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(-5)
						.WithFrom(2),
					MessageResponder.Success(new EmptyResponse()),
					MessageFrequency.OnlyOnce())
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(1)
						.WithStatus(200),
					MessageResponder.NoResponse(),
					MessageFrequency.OnlyOnce()
				);
		}));

		await ms.Start<SimpleMicroservice>(new TestArgs());
		Assert.IsTrue(ms.HasInitialized);

		testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", "TestAssumeUser", 1, 1, 2, false));

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}

	[Test]
	[NonParallelizable]
	public async Task AssumeUserAsNormal_WithCheck()
	{
		LoggingUtil.Init();
		TestSocket testSocket = null;
		var ms = new BeamableMicroService(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(1)
						.WithStatus(403),
					MessageResponder.NoResponse(),
					MessageFrequency.OnlyOnce()
				);
		}));

		await ms.Start<SimpleMicroservice>(new TestArgs());
		Assert.IsTrue(ms.HasInitialized);

		testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", "TestAssumeUser", 1, 1, 2, true));

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}
}
