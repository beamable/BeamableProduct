using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Content;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	[TestFixture]
	public class LeaderboardTests : CommonTest
	{

		[Test]
		[NonParallelizable]
		public async Task Call_LeaderboardCreate_FromTemplate()
		{

			TestSocket testSocket = null;
			var version = "623";
			var contentId = "leaderboards.test";

			var leaderboardTemplate =
				$"{{\"id\":\"{contentId}\",\"version\":\"{version}\",\"properties\":{{\"permissions\":{{\"data\":{{\"write_self\":true}}}}}}}}";
			var expectedLeaderboardBody = "{\"permissions\": { \"write_self\": true}}";
			var expectedJObject = JObject.Parse(expectedLeaderboardBody);
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddInitialContentMessageHandler(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST,
						new ContentReference
						{
							id = contentId, version = version, visibility = Constants.Features.Content.PUBLIC
						})
					.AddMessageHandler(
						MessageMatcher
							.WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n)),
						MessageResponder.Success(new EmptyResponse()),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(MessageMatcher.WithReqId(1), MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce());
			}), new TestContentResolver(x => Task.FromResult(leaderboardTemplate)));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample",
				nameof(SimpleMicroservice.LeaderboardCreateFromTemplateCallableTest), 1, 1, "leaderboard", contentId));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_LeaderboardCreate_FromCode()
		{

			TestSocket testSocket = null;

			var expectedLeaderboardBody = "{\"permissions\": { \"write_self\": true}}";
			var expectedJObject = JObject.Parse(expectedLeaderboardBody);
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n)),
						MessageResponder.Success(new EmptyResponse()),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(MessageMatcher.WithReqId(1), MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce())
					;
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample",
				nameof(SimpleMicroservice.LeaderboardCreateFromCodeCallableTest), 1, 1, "leaderboard"));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}


		[Test]
		[NonParallelizable]
		public async Task Call_ListLeaderboards()
		{

			TestSocket testSocket = null;
			var expectedLeaderboardBody = "{\"limit\":50}";
			var expectedJObject = JObject.Parse(expectedLeaderboardBody);
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
						MessageResponder.Success("{\"total\": 2, \"offset\": 3, \"nameList\": [\"howdy\", \"ho\"]}"),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<int>(x => x == 2),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple",
				nameof(SimpleMicroservice.ListLeaderboardIds), 1, 1));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_ListLeaderboards_WithLimit()
		{

			TestSocket testSocket = null;
			var limit = 42;
			var expectedLeaderboardBody = "{\"limit\":" + limit + "}";
			var expectedJObject = JObject.Parse(expectedLeaderboardBody);
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
						MessageResponder.Success("{\"total\": 2, \"offset\": 3, \"nameList\": [\"howdy\", \"ho\"]}"),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<int>(x => x == 2),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple",
				nameof(SimpleMicroservice.ListLeaderboardIdsWithLimit), 1, 1, limit));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_ListLeaderboards_WithSkip()
		{

			TestSocket testSocket = null;
			var skip = 42;
			var expectedLeaderboardBody = "{\"limit\":50,\"skip\":" + skip + "}";
			var expectedJObject = JObject.Parse(expectedLeaderboardBody);
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
						MessageResponder.Success("{\"total\": 2, \"offset\": 3, \"nameList\": [\"howdy\", \"ho\"]}"),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<int>(x => x == 2),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple",
				nameof(SimpleMicroservice.ListLeaderboardIdsWithSkip), 1, 1, skip));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}


		[Test]
		[NonParallelizable]
		public async Task Call_ListLeaderboards_WithSkipAndLimit()
		{

			TestSocket testSocket = null;
			var skip = 42;
			var limit = 13;
			var expectedLeaderboardBody = "{\"limit\":" + limit + ",\"skip\":" + skip + "}";
			var expectedJObject = JObject.Parse(expectedLeaderboardBody);
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
						MessageResponder.Success("{\"total\": 2, \"offset\": 3, \"nameList\": [\"howdy\", \"ho\"]}"),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<int>(x => x == 2),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple",
				nameof(SimpleMicroservice.ListLeaderboardIdsWithSkipAndLimit), 1, 1, skip, limit));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}


		[Test]
		[NonParallelizable]
		public async Task Call_GetPlayerLeaderboards()
		{

			TestSocket testSocket = null;
			var dbid = 43110;
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithRouteContains($"dbid={dbid}")
							.WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST),
						MessageResponder.Success("{\"result\": \"howdy\", \"lbs\": [{},{}]}"),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1)
							.WithStatus(200)
							.WithPayload<int>(x => x == 2),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					);
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple",
				nameof(SimpleMicroservice.GetPlayerLeaderboardViews), 1, 1, dbid));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}

		[Test]
		[NonParallelizable]
		public async Task Call_RemovePlayerEntryFromLeaderboard()
		{

			TestSocket testSocket = null;

			var dbid = 12345;
			var leaderbaordId = "fakeleaderboard";
			var expectedLeaderboardBody = "{\"id\":" + dbid + "}";
			var expectedJObject = JObject.Parse(expectedLeaderboardBody);
			var ms = new TestSetup(new TestSocketProvider(socket =>
			{
				testSocket = socket;
				socket.AddStandardMessageHandlers()
					.AddMessageHandler(
						MessageMatcher
							.WithRouteContains("assignment")
							.WithRouteContains($"boardId={leaderbaordId}")
							.WithMethod(Method.GET),
						MessageResponder.Success(new LeaderboardAssignmentInfo(leaderbaordId, dbid)),
						MessageFrequency.OnlyOnce()
					)
					.AddMessageHandler(
						MessageMatcher
							.WithRouteContains($"object/leaderboards/{leaderbaordId}/entry")
							.WithMethod(Method.DELETE)
							.WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n)),
						MessageResponder.Success(new EmptyResponse()),
						MessageFrequency.OnlyOnce())
					.AddMessageHandler(
						MessageMatcher
							.WithReqId(1),
						MessageResponder.NoResponse(),
						MessageFrequency.OnlyOnce()
					)
					;
			}));

			await ms.Start<SimpleMicroservice>(new TestArgs());
			Assert.IsTrue(ms.HasInitialized);

			testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple",
				nameof(SimpleMicroservice.RemovePlayerEntry), 1, 1, leaderbaordId, dbid));

			// simulate shutdown event...
			await ms.OnShutdown(this, null);
			Assert.IsTrue(testSocket.AllMocksCalled());
		}
	}
}
