using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Inventory;
using Beamable.Common.Leaderboards;
using Beamable.Server;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server.Content;
using microserviceTests.microservice.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using System;
using System.Diagnostics;
using System.Linq;
using ClientRequest = Beamable.Microservice.Tests.Socket.ClientRequest;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
    [TestFixture]
    public class StartTests
    {
        [SetUp]
        [TearDown]
        public void ResetContentInstance()
        {
	        ContentApi.Instance = new Promise<IContentApi>();
	        BeamableMicroService._contentService = null;
        }

        [Test]
        [NonParallelizable]
        public async Task InitializeAndShutdown()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket.AddStandardMessageHandlers();
            }));

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleSimpleTraffic()
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
                      .WithStatus(200)
                      .WithPayload<int>(n => n == 3),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                   );
            }));

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Add", 1, 1, 1, 2));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleScopedRoute_FailsWithoutPropertyScope()
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

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "AdminOnly", 1, 1));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleScopedRoute_PassesWithAdminScope()
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
                      .WithStatus(200),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );
            }));

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallableWithScopes("micro_sample", "AdminOnly", 1, 1, new[] { "*" }));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleScopedRoute_PassesWithProperScopes()
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
                      .WithStatus(200),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );
            }));

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallableWithScopes("micro_sample", "AdminOnly", 1, 1, new[] { "someScope", "extra" }));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleSimple_Inventory()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            const int testCount = 3000;
            var contentResolver = new TestContentResolver(async uri =>
            {
                return "{\"id\": \"items.test\", \"version\": \"1\", \"properties\": {}}";
            });
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket.WithName("Test Socket");
                socket.AddStandardMessageHandlers()
                .AddInitialContentMessageHandler(id => true, new ContentReference
                {
                    id = "items.test",
                    visibility = "public",
                    uri = "testuri"
                })
                .AddMessageHandler(
                   MessageMatcher
                      .WithGet()
                      .WithRouteContains("object/inventory"),
                   MessageResponder.Success(new InventoryResponse
                   {
                       scope = "items.test",
                       items = new List<ItemGroup>
                        {
                        new ItemGroup
                        {
                           id ="items.test",
                           items = new List<Item>
                           {
                              new Item
                              {
                                 createdAt = 1,
                                 id = "1",
                                 properties = new List<ItemProperty>(),
                                 updatedAt = 1
                              }
                           }
                        },
                        new ItemGroup
                        {
                           id ="items.test",
                           items = new List<Item>
                           {
                              new Item
                              {
                                 createdAt = 2,
                                 id = "2",
                                 properties = new List<ItemProperty>(),
                                 updatedAt = 2
                              }
                           }
                        }
                        }
                   }),
                   MessageFrequency.Exactly(testCount)
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithStatus(200)
                      .WithPayload<string>(n => n == "items.test")
                      .And(req => req.id >= 1),
                   MessageResponder.NoResponse(),
                   MessageFrequency.Exactly(testCount)
                );
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            var tasks = new List<Task>();
            for (var i = 0; i < testCount; i++)
            {
                var index = i + 1;
                tasks.Add(Task.Run(() =>
                {
                    if (!testSocket.Name.Equals("Test Socket"))
                    {
                        Assert.Fail("Not the right socket..");
                    }
                    testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "InventoryTest", index, 1,
                   new ItemRef("items.test")));
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(10);
            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        private static object[] sAdminCreateLeaderboardTestCases =
        {
         new object[]
         {
            JObject.Parse(@"{
               ""id"":""leaderboards.New_LeaderboardContent"",
               ""version"":""aac218bb99e7cd3b30d9b10165aeec7b262eef9f"",
               ""properties"":{
                  ""permissions"":{
                     ""data"":{""write_self"":false}
                  },
                  ""partitioned"":{
                     ""data"":true
                  },
                  ""max_entries"":{
                     ""data"":100
                  }
               }
            }"),
            JObject.Parse(@"{""maxEntries"":100,""partitioned"":true,""permissions"":{""write_self"":false}}")
         },
         new object[]
         {
            JObject.Parse(@"{
               ""id"":""leaderboards.New_LeaderboardContent"",
               ""version"":""dfe16c142f6c9451e61117ee63bc04f47f6120fd"",
               ""properties"":{
                  ""cohortSettings"":{
                     ""data"":{
                        ""cohorts"":[
                           {
                              ""id"":""stat_a"",
                              ""description"":""stat_a_description"",
                              ""statRequirements"":[{""domain"":""game"",""access"":""public"",""stat"":""stat_a"",""constraint"":""eq"",""value"":2}]
                           }
                        ]
                     }
                  },
                  ""permissions"":{""data"":{""write_self"":false}},
                  ""partitioned"":{""data"":true},
                  ""max_entries"":{""data"":100}
                  }
            }"),
            JObject.Parse(@"{""maxEntries"":100,""partitioned"":true,""cohortSettings"":{""cohorts"":[{""id"":""stat_a"",""description"":""stat_a_description"",""statRequirements"":[{""domain"":""game"",""access"":""public"",""constraint"":""eq"",""stat"":""stat_a"",""value"":2}]}]},""permissions"":{""write_self"":false}}")
         }
      };

        [Test, TestCaseSource(nameof(sAdminCreateLeaderboardTestCases))]
        [NonParallelizable]
        public async Task HandleSimple_AdminCreateLeaderboard(JObject leaderboardTemplateContent, JObject expectedRequestBody)
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            const int testCount = 1;

            // Leaderboard content
            var contentResolver = new TestContentResolver(async uri => leaderboardTemplateContent.ToString());
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket.WithName("Test Socket");
                socket.AddStandardMessageHandlers()
                .AddInitialContentMessageHandler(id => true, new ContentReference
                {
                    id = "leaderboards.New_LeaderboardContent",
                    visibility = "public",
                    uri = "testuri"
                })
                .AddMessageHandler(
                   MessageMatcher
                      .WithPost()
                      .WithRouteContains("object/leaderboards")
                      .WithPayload(expectedRequestBody),
                   MessageResponder.Success(new EmptyResponse()),
                   MessageFrequency.Exactly(testCount)
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithStatus(200)
                      .And(req => req.id >= 1),
                   MessageResponder.NoResponse(),
                   MessageFrequency.Exactly(testCount)
                );
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            var tasks = new List<Task>();
            for (var i = 0; i < testCount; i++)
            {
                var index = i + 1;
                tasks.Add(Task.Run(() =>
                {
                    if (!testSocket.Name.Equals("Test Socket"))
                    {
                        Assert.Fail("Not the right socket..");
                    }

                    var leaderboardRef = new LeaderboardRef();
                    leaderboardRef.SetId("leaderboards.New_LeaderboardContent");
                    testSocket.SendToClient(ClientRequest.ClientCallableWithScopes("micro_sample", "LeaderboardCreateTest", index, 1, new[] { "*" },
                   $"leaderboards.New_LeaderboardContent_{index}",
                   leaderboardRef));
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(10);
            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleSimple_FailGetUserViaAccessToken()
        {
            LoggingUtil.InitTestCorrelator();
            TestSocket testSocket = null;
            const int testCount = 3000;
            var contentResolver = new TestContentResolver(async uri => "{}");
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket.WithName("Test Socket");
                socket.AddStandardMessageHandlers();
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            var tasks = new List<Task>();
            for (var i = 0; i < testCount; i++)
            {
                var index = i + 1;
                tasks.Add(Task.Run(() =>
                {
                    if (!testSocket.Name.Equals("Test Socket"))
                    {
                        Assert.Fail("Not the right socket..");
                    }

                    testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetUserViaAccessToken", index, 1, new TokenResponse()));
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(10);

            var notImplementedLogs = TestCorrelator.GetLogEventsFromCurrentContext()
	            .Where(l => l.Level == LogEventLevel.Error)
	            .Select(l => l.RenderMessage())
	            .Where(l => l.Contains("This version of GetUser is not supported in the Microservice environment"))
	            .ToList();
            Assert.AreEqual(testCount, notImplementedLogs.Count);

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            //Assert.IsTrue(testSocket.AllMocksCalled());

        }

        [Test]
        [NonParallelizable]
        public async Task HandleLazyContentLoad_EventArrivesWhileContentIsRefreshing()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var eventProvided = false;
            var providerDelay = 1;
            var eventDelay = 1;
            var contentResolver = new TestContentResolver(async uri =>
            {
                return "{\"id\": \"content.abc\", \"version\": \"1\", \"properties\": {}}";
            });
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                //.AddStandardMessageHandlers()
                .AddAuthMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithDelete()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.SuccessWithDelay(providerDelay, new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-3)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.SuccessWithDelay(eventDelay, new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-4)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "event"),
                   res =>
                   {
                       eventProvided = true;
                       return res.Succeed(new MicroserviceProviderResponse());
                   },
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("basic/content/manifest")
                      .WithReqId(-5)
                      .WithGet(),
                   MessageResponder.SuccessWithDelay(50, new ContentManifest
                   {
                       id = "global",
                       created = 1,
                       references = new List<ContentReference>
                        {
                        new ContentReference
                        {
                           id = "content.abc",
                           visibility = "public",
                           uri = "testuri"
                        }
                        }
                   }),
                   MessageFrequency.OnlyOnce()
                )
                .AddInitialContentMessageHandler(-6,
                   new ContentReference
                   {
                       id = "content.abc",
                       visibility = "public",
                       uri = "testuri"
                   })
                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == "Echo: content.abc"),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher.WithReqId(3).WithStatus(200),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                )
                ;
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetContent", 1, 1, "content.abc"));

            // send content changed notification...
            testSocket.SendToClient(ClientRequest.Event("content.manifest", 3, new { }));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleLazyContentLoad_EventUpdateComesLater()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var eventProvided = false;
            var providerDelay = 1;
            var eventDelay = 1;
            var contentResolver = new TestContentResolver(async uri =>
            {
                return "{\"id\": \"content.abc\", \"version\": \"1\", \"properties\": {}}";
            });
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                //.AddStandardMessageHandlers()
                .AddAuthMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithDelete()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.SuccessWithDelay(providerDelay, new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-3)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.SuccessWithDelay(eventDelay, new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-4)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "event"),
                   res =>
                   {
                       eventProvided = true;
                       return res.Succeed(new MicroserviceProviderResponse());
                   },
                   MessageFrequency.OnlyOnce()
                )
                .AddInitialContentMessageHandler(-5,
                   new ContentReference
                   {
                       id = "content.abc",
                       visibility = "public",
                       uri = "testuri"
                   })
                .AddInitialContentMessageHandler(-6,
                   new ContentReference
                   {
                       id = "content.abc",
                       visibility = "public",
                       uri = "testuri"
                   })
                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == "Echo: content.abc"),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher.WithReqId(3).WithStatus(200),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetContent", 1, 1, "content.abc"));

            // send content changed notification...
            testSocket.SendToClient(ClientRequest.Event("content.manifest", 3, new { }));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }


        [Test]
        [NonParallelizable]
        public async Task HandleLazyContentLoad_EventInitializesFirst()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var eventProvided = false;
            var providerDelay = 25; // the basic service starts much later...
            var eventDelay = 1;
            var contentResolver = new TestContentResolver(async uri =>
            {
                return "{\"id\": \"content.abc\", \"version\": \"1\", \"properties\": {}}";
            });
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                //.AddStandardMessageHandlers()
                .AddAuthMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithDelete()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.SuccessWithDelay(providerDelay, new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-3)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.SuccessWithDelay(eventDelay, new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-4)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "event"),
                   res =>
                   {
                       eventProvided = true;
                       return res.Succeed(new MicroserviceProviderResponse());
                   },
                   MessageFrequency.OnlyOnce()
                )
                .AddInitialContentMessageHandler(-5,
                   new ContentReference
                   {
                       id = "content.abc",
                       visibility = "public",
                       uri = "testuri"
                   })
                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == "Echo: content.abc"),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher.WithReqId(3).WithStatus(200),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );
            }), contentResolver);

            var startUpTask = ms.Start<SimpleMicroservice>(new TestArgs());

            // send content changed notification...
            testSocket.SendToClient(ClientRequest.Event("content.manifest", 3, new { }));


            await startUpTask;
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetContent", 1, 1, "content.abc"));


            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleLazyContentLoad_WithUnreliableFlag()
        {

	        TestSocket testSocket = null;
	        var contentResolver = new TestContentResolver(async uri =>
	        {
		        return "{\"id\": \"content.abc\", \"version\": \"1\", \"properties\": {}}";
	        });
	        var ms = new BeamableMicroService(new TestSocketProvider(socket =>
	        {
		        testSocket = socket;
		        socket
			        .AddAuthMessageHandlers()
			        .AddMessageHandler(
				        MessageMatcher
					        .WithRouteContains("gateway/provider")
					        .WithReqId(-3)
					        .WithPost()
					        .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
				        MessageResponder.Success(new MicroserviceProviderResponse()),
				        MessageFrequency.OnlyOnce()
			        )
			        .AddProviderShutdownMessageHandler()
			        .AddInitialContentMessageHandler(-4, new ContentReference
			        {
				        id = "content.abc",
				        visibility = "public",
				        uri = "testuri"
			        })
			        .AddMessageHandler(
				        MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == "Echo: content.abc"),
				        MessageResponder.NoResponse(),
				        MessageFrequency.OnlyOnce()
			        );
	        }), contentResolver);

	        await ms.Start<SimpleMicroserviceWithNoEvents>(new TestArgs());
	        Assert.IsTrue(ms.HasInitialized);

	        testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple_no_updates", nameof(SimpleMicroserviceWithNoEvents.GetContent), 1, 1, "content.abc"));

	        // simulate shutdown event...
	        await ms.OnShutdown(this, null);
	        Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleLazyContentLoad()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver(async uri =>
            {
                return "{\"id\": \"content.abc\", \"version\": \"1\", \"properties\": {}}";
            });
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket.AddStandardMessageHandlers()
                .AddInitialContentMessageHandler(-5, new ContentReference
                {
                    id = "content.abc",
                    visibility = "public",
                    uri = "testuri"
                })
                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == "Echo: content.abc"),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetContent", 1, 1, "content.abc"));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleConnectionClose_Cleanly()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var connectionIndex = 0;
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                if (connectionIndex == 0)
                {
                    testSocket = socket;
                    socket
                    .AddStandardMessageHandlers(addShutdownResponder: false)
                    .AddMessageHandler(
                       MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<int>(n => n == 3),
                       MessageResponder.NoResponse(),
                       MessageFrequency.OnlyOnce()
                    );
                }
                else
                {
                    socket
                    .AddStandardMessageHandlers(4)
                    .AddMessageHandler(
                       MessageMatcher.WithReqId(2).WithStatus(200).WithPayload<int>(n => n == 3),
                       MessageResponder.NoResponse(),
                       MessageFrequency.OnlyOnce()
                    );
                }

                connectionIndex++;



            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Add", 1, 1, 1, 2));

            // simulate connection drop.
            Assert.IsTrue(testSocket.AllMocksCalled());

            testSocket.RemoteClose(); // this **should** not trigger a reconnection...

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
        }

        [Test]
        [NonParallelizable]
        public async Task HandleConnectionDrop()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var connectionIndex = 0;
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                if (connectionIndex == 0)
                {
                    socket
                    .AddStandardMessageHandlers(addShutdownResponder: false)
                    .AddMessageHandler(
                       MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<int>(n => n == 3),
                       MessageResponder.NoResponse(),
                       MessageFrequency.OnlyOnce()
                    );
                }
                else
                {
                    socket
                    .AddStandardMessageHandlers(4)
                    .AddMessageHandler(
                       MessageMatcher.WithReqId(2).WithStatus(200).WithPayload<int>(n => n == 3),
                       MessageResponder.NoResponse(),
                       MessageFrequency.OnlyOnce()
                    );
                }

                connectionIndex++;
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Add", 1, 1, 1, 2));

            // simulate connection drop.
            Assert.IsTrue(testSocket.AllMocksCalled());
            testSocket.Fault();

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Add", 2, 1, 1, 2));

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.AreEqual(2, connectionIndex);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleConnectionDrop_WhileMessageInFlight()
        {
            LoggingUtil.Init(LogEventLevel.Verbose);
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var connectionIndex = 0;
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                if (connectionIndex == 0)
                {
                    socket
                    .WithName("first")
                    .AddStandardMessageHandlers(addShutdownResponder: false);
                }
                else
                {
                    socket
                    .WithName("second")
                    .AddAuthMessageHandlersWithDelay(1000, 1000, 4, 1)
                    .AddProviderMessageHandlers(5)
                    .AddMessageHandler(
	                    MessageMatcher
		                    .WithRouteContains("basic/accounts")
		                    .WithBody<dynamic>(d => d.gamerTag == 123),
	                    MessageResponder.SuccessWithDelay(25, new User
	                    {
		                    email = "fakeEmail"
	                    }),
	                    MessageFrequency.OnlyOnce())
                    .AddMessageHandler(
                       MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == "fakeEmail"),
                       MessageResponder.NoResponse(),
                       MessageFrequency.OnlyOnce()
                    );
                }

                connectionIndex++;
            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "DelayThenGetEmail", 1, 1, 500, 123));

            // simulate connection drop.
            Assert.IsTrue(testSocket.AllMocksCalled());
            testSocket.Fault();

            // wait longer than the message's delay..
            await Task.Delay(1550);

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.AreEqual(2, connectionIndex);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleOutboundTrafficToPlatform()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var dbid = 123;
            var fakeEmail = "fake@example.com";
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                .WithName("first")
                .AddStandardMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("basic/accounts")
                      .WithBody<dynamic>(d => d.gamerTag == dbid),
                   MessageResponder.SuccessWithDelay(25, new User
                   {
                       email = fakeEmail
                   }),
                   MessageFrequency.OnlyOnce())
                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == fakeEmail), // TODO rename to "MessageMatcher"
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                )
                ;

            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetUserEmail", 1, 1, dbid));

            // wait for a hot second...
            await Task.Delay(50);

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task Handle_AckAnEvent()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                .WithName("first")
                .AddStandardMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher.WithReqId(3).WithStatus(200),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                )
                ;

            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.Event("abc", 3, new { }));
            // wait for a hot second...
            await Task.Delay(50);

            // simulate shutdown event...
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleManyEventsOnStartup()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            const int eventCount = 100;
            const int basicProviderDelay = 500;
            const int eventProviderDelay = 10;
            var contentResolver = new TestContentResolver();
            var eventProvided = false;
            // var promise
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                .WithName("first")
                .AddAuthMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithDelete()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.Success(new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-3)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "basic"),
                   MessageResponder.SuccessWithDelay(basicProviderDelay, new MicroserviceProviderResponse()),
                   MessageFrequency.OnlyOnce()
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("gateway/provider")
                      .WithReqId(-4)
                      .WithPost()
                      .WithBody<MicroserviceProviderRequest>(body => body.type == "event"),
                   res =>
                   {
                       eventProvided = true;
                       return res.Succeed(new MicroserviceProviderResponse());
                   },
                   MessageFrequency.OnlyOnce()
                )

                .AddMessageHandler(
                   MessageMatcher.WithStatus(200).And(req => req.id >= 1),
                   MessageResponder.NoResponse(),
                   MessageFrequency.Exactly(eventCount)
                )
                ;

            }), contentResolver);

            var initializationTask = ms.Start<SimpleMicroservice>(new TestArgs());

            var tasks = new List<Task>();
            await Task.Delay(10);
            Assert.IsTrue(eventProvided);
            var sends = 0;
            for (var i = 0; i < eventCount; i++)
            {
                var index = i;
                var task = Task.Run(async () =>
                {
                    testSocket.SendToClient(ClientRequest.Event("abc", 1 + index, new { }));
                    Interlocked.Increment(ref sends);
                    await Task.Delay(1);
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(sends, eventCount);

            // wait for a hot second...
            await initializationTask;
            Assert.IsTrue(ms.HasInitialized);

            // simulate shutdown event...
            await ms.OnShutdown(this, null);

            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleShutdown_WithDanglingRequest()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var dbid = 123;
            var fakeEmail = "fake@example.com";
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                .WithName("first")
                .AddStandardMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("basic/accounts")
                      .WithBody<dynamic>(d => d.gamerTag == dbid),
                   MessageResponder.SuccessWithDelay(250, new User // this request will take a quarter second...
                   {
                       email = fakeEmail
                   }),
                   MessageFrequency.OnlyOnce())
                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == fakeEmail),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );

            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetUserEmail", 1, 1, dbid));

            // wait for a hot second...
            await Task.Delay(50);

            // simulate shutdown event...
            Assert.IsFalse(testSocket.AllMocksCalled());

            await ms.OnShutdown(this, null); // one request should be processed durring the shutdown

            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleShutdown_BlockNewRequest()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var dbid = 123;
            var fakeEmail = "fake@example.com";
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                .WithName("first")
                .AddStandardMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("basic/accounts")
                      .WithBody<dynamic>(d => d.gamerTag == dbid),
                   MessageResponder.SuccessWithDelay(250, new User // this request will take a quarter second...
                   {
                       email = fakeEmail
                   }),
                   MessageFrequency.OnlyOnce())
                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == fakeEmail),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );

            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetUserEmail", 1, 1, dbid));

            // wait for a hot second...
            await Task.Delay(50);

            var shutdownTask = ms.OnShutdown(this, null); // one request should be processed durring the shutdown

            // this request should be ignored...
            // also, Thorium should never send it, so this is a bit of defensive programming.
            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetUserEmail", 2, 1, dbid));
            await shutdownTask;

            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        public async Task HandleAuthDrop()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var dbid = 123;
            var fakeEmail = "fake@example.com";
            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;
                socket
                .WithName("first")
                .AddStandardMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("basic/accounts")
                      .WithBody<dynamic>(d => d.gamerTag == dbid),
                   MessageResponder.AuthFailure(),
                   MessageFrequency.OnlyOnce())
                .AddAuthMessageHandlers(5)
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("basic/accounts")
                      .WithBody<dynamic>(d => d.gamerTag == dbid),
                   MessageResponder.Success(new User { email = fakeEmail }),
                   MessageFrequency.OnlyOnce())

                .AddMessageHandler(
                   MessageMatcher.WithReqId(1).WithStatus(200).WithPayload<string>(n => n == fakeEmail),
                   MessageResponder.NoResponse(),
                   MessageFrequency.OnlyOnce()
                );


            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetUserEmail", 1, 1, dbid));

            await Task.Delay(1000); // the auth cycle will take some time to figure itself out, and if we trigger a shutdown too soon, the request id of the shutdown will get bungled with the re-auth flow.
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
        }

        [Test]
        [NonParallelizable]
        [Timeout(120000)]
        public async Task HandleAuthDrop_WithMultipleRequestsInFlight_WithoutHardcodedRequestIds()
        {
            LoggingUtil.Init();
            TestSocket testSocket = null;
            var contentResolver = new TestContentResolver();
            var dbid = 123;
            var fakeEmail = "fake@example.com";
            var nonceDelay = 2500; // needs to be well above how long it takes to issue failureCount requests
            var authDelay = 100;
            const int failureCount = 2000;
            var sent = false;
            var noncePromise = new Promise();
            TestSocketResponseGeneratorAsync nonceSuccess = async res =>
            {
	            await noncePromise;
	            return res.Succeed(new MicroserviceNonceResponse { nonce = "testnonce" });
            };
            int successCount = 0;
            int authFailCount = 0;

            var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
                testSocket = socket;

                socket
                .WithName("first")
                .AddStandardMessageHandlers()
                .AddMessageHandler(
                   MessageMatcher
                      .WithRouteContains("basic/accounts")
                      .WithBody<dynamic>(d => d.gamerTag == dbid),
                   MessageResponder.Custom(res =>
                   {
	                   if (noncePromise.IsCompleted)
	                   {
		                   Interlocked.Increment(ref successCount);
		                   return res.Succeed(new User { email = fakeEmail });
	                   }

	                   Interlocked.Increment(ref authFailCount);
	                   return res.AuthFailure();
                   }),
                   MessageFrequency.AtLeast(1),
                   "original failure"
                   )
                .AddMessageHandler(
                   MessageMatcher.WithRouteContains("nonce"),
                   MessageResponder.CustomAsync(nonceSuccess),
                   MessageFrequency.OnlyOnce(), "re-nonce"
                )
                .AddMessageHandler(
                   MessageMatcher.WithRouteContains("auth"),
                   MessageResponder.SuccessWithDelay(authDelay, ()=>
                   {
	                   testSocket.IsAuthenticated = true;
	                   return new MicroserviceAuthResponse { result = "ok" };
                   }),
                   MessageFrequency.OnlyOnce(), "re-auth"
                )
                .AddMessageHandler(
                   MessageMatcher
                      .WithPositiveReqId()
                      .WithStatus(200).WithPayload<string>(n => n == fakeEmail),
                   MessageResponder.NoResponse(),
                   MessageFrequency.Exactly(failureCount),
                   "success-after-failure"
                )
                ;


            }), contentResolver);

            await ms.Start<SimpleMicroservice>(new TestArgs());
            Assert.IsTrue(ms.HasInitialized);

            var tasks = new List<Task>();
            for (var i = 0; i < failureCount; i++)
            {
	            var index = i + 1;
	            tasks.Add(Task.Run(() =>
                {
                    testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "GetUserEmail", index, 1, dbid));
                }));
            }
            await Task.Delay(nonceDelay);
            noncePromise.CompleteSuccess();

            var waitingTask = Task.WhenAll(tasks);
            await waitingTask;

            await Task.Delay(1000);
            await ms.OnShutdown(this, null);
            Assert.IsTrue(testSocket.AllMocksCalled());
            Assert.IsTrue(authFailCount <= failureCount); // it cannot be the case that we failed more messages for auth failure than we had original requests.
            Assert.AreEqual(failureCount, successCount); // we need to make sure that the success message came back for each failure, eventually.
        }

    }
}
