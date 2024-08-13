using System.Threading.Tasks;
using Beamable.Common.Api;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests;

[TestFixture]
public class AssumeUserTests : CommonTest
{
	
		
	[TestCase(true)]
	[TestCase(false)]
	[NonParallelizable]
	public async Task AssumeUserAsAdmin_SubCallRequiresUser(bool forceCheck)
	{

		TestSocket testSocket = null;
		var ms = new TestSetup(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST)
						.WithFrom(2)
					,
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

		testSocket.SendToClient(
			ClientRequest.ClientCallableAsAdmin("micro_simple", nameof(SimpleMicroservice.TestAssumeUser_RequestRequiresUser), 1, 1, 2, forceCheck));

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}
		
	[TestCase(true)]
	[TestCase(false)]
	[NonParallelizable]
	public async Task AssumeUserAsAdmin(bool forceCheck)
	{

		TestSocket testSocket = null;
		var ms = new TestSetup(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST)
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

		testSocket.SendToClient(
			ClientRequest.ClientCallableAsAdmin("micro_simple", "TestAssumeUser", 1, 1, 2, forceCheck));

		// simulate shutdown event...
		await ms.OnShutdown(this, null);
		Assert.IsTrue(testSocket.AllMocksCalled());
	}

	[Test]
	[NonParallelizable]
	public async Task AssumeUserAsNormal_WithNoCheck()
	{

		TestSocket testSocket = null;
		var ms = new TestSetup(new TestSocketProvider(socket =>
		{
			testSocket = socket;
			socket.AddStandardMessageHandlers()
				.AddMessageHandler(
					MessageMatcher
						.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST)
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

		TestSocket testSocket = null;
		var ms = new TestSetup(new TestSocketProvider(socket =>
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
		allowErrorLogs = true;
		AssertMissingScopeError();
		Assert.IsTrue(testSocket.AllMocksCalled());
	}
}
