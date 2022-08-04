using Beamable.Common;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using microserviceTests.microservice.Util;
using NUnit.Framework;
using System.Threading.Tasks;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

public class RequestContextTests : CommonTest
{
	public void Test()
	{
		//GetVersionHeaders
	}

	[Test]
	[NonParallelizable]
	public async Task GetAllHeaders()
	{
		TestSocket testSocket = null;
		var gameVersion = "game";
		var sdkVersion = "sdk";
		var engineVersion = "engine";
		var clientType = "client";

		var ms = new BeamableMicroService(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(1)
						.WithStatus(200)
						.WithPayload<string>(n => n == $"h{gameVersion}/{sdkVersion}/{engineVersion}/{clientType}"),
					MessageResponder.NoResponse(),
					MessageFrequency.OnlyOnce()
				);
		}));

		await ms.Start<SimpleMicroservice>(new TestArgs());
		Assert.IsTrue(ms.HasInitialized);

		testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", nameof(SimpleMicroservice.GetVersionHeaders), 1, 1)
			.WithHeader(Constants.Requester.HEADER_UNITY_VERSION, engineVersion)
			.WithHeader(Constants.Requester.HEADER_ENGINE_TYPE, clientType)
			.WithHeader(Constants.Requester.HEADER_BEAMABLE_VERSION, sdkVersion)
			.WithHeader(Constants.Requester.HEADER_APPLICATION_VERSION, gameVersion)
		);

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}

	[Test]
	[NonParallelizable]
	public async Task GetMissingHeader()
	{
		TestSocket testSocket = null;
		var gameVersion = "game";
		var engineVersion = "engine";

		var ms = new BeamableMicroService(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(1)
						.WithStatus(200)
						.WithPayload<string>(n => n == $"h{gameVersion}//{engineVersion}/"),
					MessageResponder.NoResponse(),
					MessageFrequency.OnlyOnce()
				);
		}));

		await ms.Start<SimpleMicroservice>(new TestArgs());
		Assert.IsTrue(ms.HasInitialized);

		testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", nameof(SimpleMicroservice.GetVersionHeaders), 1, 1)
			.WithHeader(Constants.Requester.HEADER_UNITY_VERSION, engineVersion)
			.WithHeader(Constants.Requester.HEADER_APPLICATION_VERSION, gameVersion)
		);

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}


	[Test]
	[NonParallelizable]
	public async Task GetNoHeaders()
	{
		TestSocket testSocket = null;
		var ms = new BeamableMicroService(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(1)
						.WithStatus(200)
						.WithPayload<string>(n => n == $"h{null}/{null}/{null}/{null}")
						,
					MessageResponder.NoResponse(),
					MessageFrequency.OnlyOnce()
				);
		}));

		await ms.Start<SimpleMicroservice>(new TestArgs());
		Assert.IsTrue(ms.HasInitialized);

		testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", nameof(SimpleMicroservice.GetVersionHeaders), 1, 1)
		);

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}
}
