using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Content;
using microserviceTests.microservice.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
	[TestFixture]
	public class LeaderboardTests
	{
		[SetUp]
		[TearDown]
		public void ResetContentInstance()
		{
			ContentApi.Instance = new Promise<IContentApi>();
		}


      [Test]
      [NonParallelizable]
      public async Task Call_LeaderboardCreate_FromTemplate()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;
         var version = "623";
         var contentId = "leaderboards.test";

         var leaderboardTemplate =
            $"{{\"id\":\"{contentId}\",\"version\":\"{version}\",\"properties\":{{\"permissions\":{{\"data\":{{\"write_self\":true}}}}}}}}";
         var expectedLeaderboardBody = "{\"permissions\": { \"write_self\": true}}";
         var expectedJObject = JObject.Parse(expectedLeaderboardBody);
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddInitialContentMessageHandler(-5, new ContentReference
               {
                  id = contentId,
                  version = version,
                  visibility = Constants.Features.Content.PUBLIC
               })
               .AddMessageHandler(
                  MessageMatcher
                     .WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n)),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce());
         }), new TestContentResolver(x => Task.FromResult(leaderboardTemplate)));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", nameof(SimpleMicroservice.LeaderboardCreateFromTemplateCallableTest), 1, 1, "leaderboard", contentId));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_LeaderboardCreate_FromCode()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;

         var expectedLeaderboardBody = "{\"permissions\": { \"write_self\": true}}";
         var expectedJObject = JObject.Parse(expectedLeaderboardBody);
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n)),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce());
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", nameof(SimpleMicroservice.LeaderboardCreateFromCodeCallableTest), 1, 1, "leaderboard"));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }


      [Test]
      [NonParallelizable]
      public async Task Call_ListLeaderboards()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;
         var expectedLeaderboardBody = "{\"limit\":50}";
         var expectedJObject = JObject.Parse(expectedLeaderboardBody);
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
                     .WithReqId(-5),
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.ListLeaderboardIds), 1, 1));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_ListLeaderboards_WithLimit()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;
         var limit = 42;
         var expectedLeaderboardBody = "{\"limit\":"+limit+"}";
         var expectedJObject = JObject.Parse(expectedLeaderboardBody);
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
                     .WithReqId(-5),
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.ListLeaderboardIdsWithLimit), 1, 1, limit));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_ListLeaderboards_WithSkip()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;
         var skip = 42;
         var expectedLeaderboardBody = "{\"limit\":50,\"skip\":"+skip+"}";
         var expectedJObject = JObject.Parse(expectedLeaderboardBody);
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
                     .WithReqId(-5),
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.ListLeaderboardIdsWithSkip), 1, 1, skip));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }


      [Test]
      [NonParallelizable]
      public async Task Call_ListLeaderboards_WithSkipAndLimit()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;
         var skip = 42;
         var limit = 13;
         var expectedLeaderboardBody = "{\"limit\":"+limit+",\"skip\":"+skip+"}";
         var expectedJObject = JObject.Parse(expectedLeaderboardBody);
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
                     .WithReqId(-5),
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.ListLeaderboardIdsWithSkipAndLimit), 1, 1, skip, limit));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }


      [Test]
      [NonParallelizable]
      public async Task Call_GetPlayerLeaderboards()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;
         var dbid = 43110;
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithRouteContains($"dbid={dbid}")
                     .WithReqId(-5),
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.GetPlayerLeaderboardViews), 1, 1, dbid));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_RemovePlayerEntryFromLeaderboard()
      {
	      LoggingUtil.Init();
	      TestSocket testSocket = null;
	      
	      var dbid = 12345;
	      var expectedLeaderboardBody = "{\"id\":"+dbid+"}";
	      var expectedJObject = JObject.Parse(expectedLeaderboardBody);
	      var ms = new BeamableMicroService(new TestSocketProvider(socket =>
	      {
		      testSocket = socket;
		      socket.AddStandardMessageHandlers()
		            .AddMessageHandler(
			            MessageMatcher
				            .WithBody<JObject>(n => JToken.DeepEquals(expectedJObject, n))
				            .WithReqId(-3),
			            MessageResponder.NoResponse(),
			            MessageFrequency.OnlyOnce());
	      }));

	      await ms.Start<SimpleMicroservice>(new TestArgs());
	      Assert.IsTrue(ms.HasInitialized);

	      testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.RemovePlayerEntry), 1, 1, "leaderboard.test", dbid));

	      // simulate shutdown event...
	      await ms.OnShutdown(this, null);
	      Assert.IsTrue(testSocket.AllMocksCalled());
      }
	}
}
