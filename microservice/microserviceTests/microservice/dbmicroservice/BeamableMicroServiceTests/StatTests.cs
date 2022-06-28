using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using microserviceTests.microservice.Util;
using NUnit.Framework;
using System.Threading.Tasks;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	[TestFixture]
	public class StatTests
	{
		[SetUp]
		[TearDown]
		public void ResetContentInstance()
		{
			ContentApi.Instance = new Promise<IContentApi>();
		}

		[Microservice("statservice")]
		public class StatMicroservice : Microservice
		{
			[ClientCallable]
			public async Task<string> GetPrivateStats(string statKey)
			{
				var stat = await Services.Stats.GetProtectedPlayerStat(Context.UserId, statKey);
				return stat;
			}

			[ClientCallable]
			public async Task<string> GetStats(string domain, string access, string type)
			{
				var stats = await Services.Stats.GetStats(domain, access, type, Context.UserId);
				return string.Join(",", stats.Values);
			}
		}

		[Test]
		[NonParallelizable]
		public async Task Call_BatchStats_TRIALS()
		{
			LoggingUtil.Init();
			TestSocket testSocket = null;
			var ms = new BeamableMicroService(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(-5),
				   MessageResponder.Success("{\"results\": [ {\"id\": 1, \"stats\": [ {\"k\": \"TRIALS\", \"v\": [] },  {\"k\": \"tuna\", \"v\": \"fish\" },  {\"k\": \"num\", \"v\": 1 } ] }] }"),
				   MessageFrequency.OnlyOnce())
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(1)
					  .WithStatus(200)
					  .WithPayload("[],fish,1"),
				   MessageResponder.NoResponse(),
				   MessageFrequency.OnlyOnce()
				);
			}));

			await ms.Start<StatMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetStats), 1, 1, "game", "private", "player"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_BatchStats_TRIALS_WithStuff()
		{
			LoggingUtil.Init();
			TestSocket testSocket = null;
			var ms = new BeamableMicroService(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(-5),
				   MessageResponder.Success("{\"results\": [ {\"id\": 1, \"stats\": [ {\"k\": \"TRIALS\", \"v\": [\"123\",3] },  {\"k\": \"tuna\", \"v\": \"fish\" },  {\"k\": \"num\", \"v\": 1 } ] }] }"),
				   MessageFrequency.OnlyOnce())
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(1)
					  .WithStatus(200)
					  .WithPayload("[\"123\",3],fish,1"),
				   MessageResponder.NoResponse(),
				   MessageFrequency.OnlyOnce()
				);
			}));

			await ms.Start<StatMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetStats), 1, 1, "game", "private", "player"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_TRIALS()
		{
			LoggingUtil.Init();
			TestSocket testSocket = null;
			var ms = new BeamableMicroService(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(-5),
				   MessageResponder.Success("{\"id\": 1, \"stats\": {\"TRIALS\":[], \"tuna\": 2} }"),
				   MessageFrequency.OnlyOnce())
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(1)
					  .WithStatus(200)
					  .WithPayload("[]"),
				   MessageResponder.NoResponse(),
				   MessageFrequency.OnlyOnce()
				);
			}));

			await ms.Start<StatMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetPrivateStats), 1, 1, "TRIALS"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_TRIALS_WithStuff()
		{
			LoggingUtil.Init();
			TestSocket testSocket = null;
			var ms = new BeamableMicroService(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(-5),
				   MessageResponder.Success("{\"id\": 1, \"stats\": {\"TRIALS\":[\"123\", 3], \"tuna\": 2} }"),
				   MessageFrequency.OnlyOnce())
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(1)
					  .WithStatus(200)
					  .WithPayload("[\"123\",3]"),
				   MessageResponder.NoResponse(),
				   MessageFrequency.OnlyOnce()
				);
			}));

			await ms.Start<StatMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetPrivateStats), 1, 1, "TRIALS"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_NormalStringStat()
		{
			LoggingUtil.Init();
			TestSocket testSocket = null;
			var ms = new BeamableMicroService(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(-5),
				   MessageResponder.Success("{\"id\": 1, \"stats\": {\"Session\":\"fish\", \"tuna\": 2} }"),
				   MessageFrequency.OnlyOnce())
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(1)
					  .WithStatus(200)
					  .WithPayload("fish"),
				   MessageResponder.NoResponse(),
				   MessageFrequency.OnlyOnce()
				);
			}));

			await ms.Start<StatMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetPrivateStats), 1, 1, "Session"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_NormalNumber()
		{
			LoggingUtil.Init();
			TestSocket testSocket = null;
			var ms = new BeamableMicroService(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(-5),
				   MessageResponder.Success("{\"id\": 1, \"stats\": {\"Session\":\"fish\", \"tuna\": 2} }"),
				   MessageFrequency.OnlyOnce())
				.AddMessageHandler(
				   MessageMatcher
					  .WithReqId(1)
					  .WithStatus(200)
					  .WithPayload("2"),
				   MessageResponder.NoResponse(),
				   MessageFrequency.OnlyOnce()
				);
			}));

			await ms.Start<StatMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetPrivateStats), 1, 1, "tuna"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}
	}
}
