using Beamable.Common.Api.Stats;
using System.Threading.Tasks;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	[TestFixture]
	public class StatTests : CommonTest
	{

		[Microservice("statservice", EnableEagerContentLoading = false)]
		public class StatMicroservice : Microservice
		{
			[ClientCallable]
			public async Task<string> GetPrivateStats(string statKey)
			{
				var stat = await Services.Stats.GetStat(StatsDomainType.Game, StatsAccessType.Private, Context.UserId, statKey);
				return stat;
			}

			[ClientCallable]
			public async Task<string> GetStats(StatsDomainType domain, StatsAccessType access)
			{
				var stats = await Services.Stats.GetStats(domain, access, Context.UserId);
				return string.Join(",", stats.Values);
			}
		}

		[Test]
		[NonParallelizable]
		public async Task Call_BatchStats_TRIALS()
		{
			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
						MessageResponder.Success(
							"{ \"id\": 1, \"stats\": { \"TRIALS\": [] , \"tuna\": \"fish\", \"num\": 1 } }"),
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

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetStats),
				1, 1, StatsDomainType.Game, StatsAccessType.Private));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_BatchStats_TRIALS_WithStuff()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
						MessageResponder.Success(
							"{\"id\":1,\"stats\":{\"TRIALS\":[\"123\",3],\"tuna\":\"fish\",\"num\":1}}"),
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

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice", nameof(StatMicroservice.GetStats),
				1, 1, StatsDomainType.Game, StatsAccessType.Private));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_TRIALS()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
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

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice",
				nameof(StatMicroservice.GetPrivateStats), 1, 1, "TRIALS"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_TRIALS_WithStuff()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
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

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice",
				nameof(StatMicroservice.GetPrivateStats), 1, 1, "TRIALS"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_NormalStringStat()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
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

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice",
				nameof(StatMicroservice.GetPrivateStats), 1, 1, "Session"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_PrivateStats_NormalNumber()
		{

			TestSocket testSocket = null;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
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

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_statservice",
				nameof(StatMicroservice.GetPrivateStats), 1, 1, "tuna"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}
	}
}
