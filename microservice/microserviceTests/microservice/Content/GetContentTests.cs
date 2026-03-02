using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Common.Reflection;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Content;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading;
using Beamable.Common.Leaderboards;
using microserviceTests.microservice.Util;

namespace microserviceTests.microservice.Content
{
   [TestFixture]
   public class GetContentTests
   {
      private ReflectionCache _cache;

      [SetUp]
      public void Setup()
      {
         _cache = new ReflectionCache();
         var contentTypeCache = new ContentTypeReflectionCache();
         _cache.RegisterTypeProvider(contentTypeCache);
         _cache.RegisterReflectionSystem(contentTypeCache);

         var asms = AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name).ToList();
         _cache.GenerateReflectionCache(asms);
         
         LoggingUtil.InitTestCorrelator();
      }

      
      
      [Test]
      public async Task FirstManifestBreaksWithAuth()
      {
         var args = new TestArgs();
         var reqCtx = new RequestContext(args.CustomerID, args.ProjectName, 1, 200, 1, "path", "GET", "");
         var contentResolver = new TestContentResolver(async (uri) =>
         {
            var content = new ItemContent();
            content.SetContentName("foo");
            
            var serailizer = new MicroserviceContentSerializer();
            var json = serailizer.Serialize(content);
            return json;
         });

         TestSocket testSocket = null;
         var socketProvider = new TestSocketProvider(socket =>
         {
            testSocket = socket;
            
            // make the first request fail with an auth issue.
            socket.AddMessageHandler(
	            MessageMatcher
		            .WithRouteContains("basic/content/manifest")
		            .WithReqId(-1)
		            .WithGet(),
	            MessageResponder.AuthFailure(100),
	            MessageFrequency.OnlyOnce()
            );
            
            // but the second attempt succeeds!
            //  -4 is the request id after the first failed content call, and the nonce and auth calls. 
            socket.AddInitialContentMessageHandler(-4, new ContentReference
            {
               id = "items.foo",
               version = "123",
               uri = "items.foo",
               visibility = "public"
            });
            socket.SetAuthentication(true);

            // set up nonce and auth calls to be -2 and -3
            socket.AddAuthMessageHandlers(1);

         });

         var socket = socketProvider.Create("test", args);
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
        
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx, false, new NoopActivityProvider());
         (_, socketCtx.Daemon) =
	         MicroserviceAuthenticationDaemon.Start(args, requester, new CancellationTokenSource());

         var contentService = new ContentService(requester, socketCtx, contentResolver, _cache);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc);
         });


         await contentService.Init();

         var fetchPromise = contentService.GetContent("items.foo");
         var fetchTask = Task.Run(async () => await fetchPromise);
         fetchTask.Wait(1000);
         Assert.IsTrue(fetchPromise.IsCompleted);
         
         Assert.AreEqual("items.foo", fetchPromise.GetResult().Id);

         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      
      [Test]
      public void Simple()
      {
         var args = new TestArgs();
         var reqCtx = new RequestContext(args.CustomerID, args.ProjectName, 1, 200, 1, "path", "GET", "");
         var contentResolver = new TestContentResolver(async (uri) =>
         {
            var content = new ItemContent();
            content.SetContentName("foo");


            var serailizer = new MicroserviceContentSerializer();
            var json = serailizer.Serialize(content);
            return json;
         });

         TestSocket testSocket = null;
         var socketProvider = new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddInitialContentMessageHandler(-1, new ContentReference
            {
               id = "items.foo",
               version = "123",
               uri = "items.foo",
               visibility = "public"
            });
            socket.SetAuthentication(true);

            // don't mock anything...
         });

         var socket = socketProvider.Create("test", args);
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
        
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx, false, new NoopActivityProvider());
         (_, socketCtx.Daemon) =
	         MicroserviceAuthenticationDaemon.Start(args, requester, new CancellationTokenSource());

         var contentService = new ContentService(requester, socketCtx, contentResolver, _cache);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc);
         });


         contentService.Init();

         var fetchPromise = contentService.GetContent("items.foo");
         var fetchTask = Task.Run(async () => await fetchPromise);
         fetchTask.Wait(10);
         Assert.IsTrue(fetchPromise.IsCompleted);

         Assert.AreEqual("items.foo", fetchPromise.GetResult().Id);

         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      
      
      [Test]
      public void Simple_Filter()
      {
         var args = new TestArgs();
         var reqCtx = new RequestContext(args.CustomerID, args.ProjectName, 1, 200, 1, "path", "GET", "");
         var contentResolver = new TestContentResolver(async (uri) =>
         {
	         var serailizer = new MicroserviceContentSerializer();

	         if (uri.Contains("boo"))
	         {
		         var leaderboardContent = new LeaderboardContent();
		         leaderboardContent.SetContentName("boo");
		         return serailizer.Serialize(leaderboardContent);
	         }
	         else
	         {
		         var itemContent = new ItemContent();
		         itemContent.SetContentName("foo");
		         return serailizer.Serialize(itemContent);
	         }

         });

         TestSocket testSocket = null;
         var socketProvider = new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddInitialContentMessageHandler(-1, new ContentReference
            {
               id = "items.foo",
               version = "123",
               uri = "items.foo",
               visibility = "public"
            }, new ContentReference
            {
	            id = "leaderboards.boo",
	            version = "123",
	            uri = "leaderboards.boo",
	            visibility = "public"
            });
            socket.SetAuthentication(true);

            // don't mock anything...
         });

         var socket = socketProvider.Create("test", args);
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx, false, new NoopActivityProvider());
         (_, socketCtx.Daemon) =
	         MicroserviceAuthenticationDaemon.Start(args, requester, new CancellationTokenSource());

         var contentService = new ContentService(requester, socketCtx, contentResolver, _cache);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc);
         });


         contentService.Init();

         var fetchPromise = contentService.GetManifest("t:items");
         var fetchTask = Task.Run(async () => await fetchPromise);
         fetchTask.Wait(10);
         Assert.IsTrue(fetchPromise.IsCompleted);

         Assert.AreEqual(1, fetchPromise.GetResult().entries.Count);
         Assert.AreEqual("items.foo", fetchPromise.GetResult().entries[0].contentId);

         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      
      [Test]
      public void CachePurgedOnNewManifest()
      {
         var timesToGetContent = 100;
         var fetchCounter = 0;
         var args = new TestArgs();
         var reqCtx = new RequestContext(args.CustomerID, args.ProjectName, 1, 200, 1, "path", "GET", "");
         var contentResolver = new TestContentResolver(async (uri) =>
         {
            fetchCounter++;
            var content = new ItemContent();
            content.SetContentName("foo");


            var serailizer = new MicroserviceContentSerializer();
            var json = serailizer.Serialize(content);
            return json;
         });

         TestSocket testSocket = null;
         var socketProvider = new TestSocketProvider(socket =>
         {
            testSocket = socket;

            socket.AddInitialContentMessageHandler(-1, new ContentReference
               {
                  id = "items.foo",
                  version = "123",
                  uri = "items.foo",
                  visibility = "public"
               })
               .AddInitialContentMessageHandler(-2, new ContentReference
               {
                  id = "items.foo",
                  version = "123",
                  uri = "items.foo.newversion",
                  visibility = "public"
               })
               .SetAuthentication(true)
               ;

            // don't mock anything...
         });

         var socket = socketProvider.Create("test", args);
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx, false, new NoopActivityProvider());
         (_, socketCtx.Daemon) =
	         MicroserviceAuthenticationDaemon.Start(args, requester, new CancellationTokenSource());

         var contentService = new ContentService(requester, socketCtx, contentResolver, _cache);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc);
         });


         contentService.Init();

         for (var i = 0; i < timesToGetContent; i++)
         {
            var fetchPromise = contentService.GetContent("items.foo");
            var fetchTask = Task.Run(async () => await fetchPromise);
            fetchTask.Wait(10);
            Assert.IsTrue(fetchPromise.IsCompleted);
            Assert.AreEqual("items.foo", fetchPromise.GetResult().Id);
         }

         Assert.AreEqual(1, fetchCounter);
         // purge the cache...
         testSocket.SendToClient(ClientRequest.Event("content.manifest", 3, new { }));

         for (var i = 0; i < timesToGetContent; i++)
         {
            var fetchPromise = contentService.GetContent("items.foo");
            var fetchTask = Task.Run(async () => await fetchPromise);
            fetchTask.Wait(10);
            Assert.IsTrue(fetchPromise.IsCompleted);
            Assert.AreEqual("items.foo", fetchPromise.GetResult().Id);
         }

         Assert.AreEqual(2, fetchCounter);

         socketCtx.Daemon.KillAuthThread();
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      public async Task ContentOnlyFetchedOnce()
      {
         var timesToGetContent = 100;
         var fetchCounter = 0;
         var args = new TestArgs();
         var reqCtx = new RequestContext(args.CustomerID, args.ProjectName, 1, 200, 1, "path", "GET", "");
         var contentResolver = new TestContentResolver(async (uri) =>
         {
            fetchCounter++;
            var content = new ItemContent();
            content.SetContentName("foo");


            var serailizer = new MicroserviceContentSerializer();
            var json = serailizer.Serialize(content);
            return json;
         });

         TestSocket testSocket = null;
         var socketProvider = new TestSocketProvider(socket =>
         {
            testSocket = socket;

            socket.AddInitialContentMessageHandler(-1, new ContentReference
            {
               id = "items.foo",
               version = "123",
               uri = "items.foo",
               visibility = "public"
            });
            socket.SetAuthentication(true);

            // don't mock anything...
         });

         var socket = socketProvider.Create("test", args);
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx, false, new NoopActivityProvider());
         (_, socketCtx.Daemon) =
	         MicroserviceAuthenticationDaemon.Start(args, requester, new CancellationTokenSource());

         var contentService = new ContentService(requester, socketCtx, contentResolver, _cache);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc);
         });


         contentService.Init();

         var tasks = new List<Task>();
         for (var i = 0; i < timesToGetContent; i++)
         {
	         if (i == 1)
	         {
		         await Task.Delay(200); // simulate some time for all the requests to get started. 
	         }
	         var task = Task.Run(async () =>
	         {
		         var fetchPromise = contentService.GetContent("items.foo");
		         var fetchTask = Task.Run(async () => await fetchPromise);
		         fetchTask.Wait(10);
		         Assert.IsTrue(fetchPromise.IsCompleted);
		         Assert.AreEqual("items.foo", fetchPromise.GetResult().Id);
	         });
            tasks.Add(task);
            
         }

         await Task.WhenAll(tasks);
         Assert.AreEqual(1, fetchCounter);

         Assert.IsTrue(testSocket.AllMocksCalled());
      }
      
      [Test]
      public async Task ContentFetchTimeTest()
      {
	      Stopwatch watch = Stopwatch.StartNew();
	      
	      var args = new TestArgs();
	      var contentResolver = new TestContentResolver(async (uri) =>
	      {
		      var content = new ItemContent();
		      content.SetContentName(uri);

		      var serializer = new MicroserviceContentSerializer();
		      var json = serializer.Serialize(content);
		      return json;
	      });
	      
	      TestSocket testSocket = null;
	      var socketProvider = new TestSocketProvider(socket =>
	      {
		      testSocket = socket;

		      int contentCount = 5000;
		      ContentReference[] references = new ContentReference[contentCount];
		      for (int i = 0; i < references.Length; i++)
		      {
			      string id = $"items.test{i}";
			      references[i] = new ContentReference {id = id, version = "123", uri = id, visibility = "public"};
		      }

		      socket.AddStandardMessageHandlers().AddInitialContentMessageHandler(-6, references).AddMessageHandler(
			      MessageMatcher
				      .WithReqId(1)
				      .WithStatus(200),
			      MessageResponder.NoResponse(),
			      MessageFrequency.OnlyOnce()
		      ).AddMessageHandler(
			      MessageMatcher
				      .WithReqId(2)
				      .WithStatus(200),
			      MessageResponder.NoResponse(),
			      MessageFrequency.OnlyOnce()
		      );
		      socket.SetAuthentication(true);
	      });
	      
	      var ms = new TestSetup(socketProvider, contentResolver);

	      await ms.Start<SimpleMicroservice>(args);
	      Assert.IsTrue(ms.HasInitialized);
	      
	      watch.Restart();
	      testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.GetContents), 1, 1));
	      watch.Stop();
	      Console.WriteLine($"First call executed in {watch.ElapsedMilliseconds}ms");
	      
	      watch.Restart();
	      testSocket.SendToClient(ClientRequest.ClientCallable("micro_simple", nameof(SimpleMicroservice.GetContents), 2, 1));
	      watch.Stop();
	      Console.WriteLine($"Second call executed in {watch.ElapsedMilliseconds}ms");

	      await ms.OnShutdown(this, null);
	      
	      Assert.IsTrue(testSocket.AllMocksCalled());
      }
   }
}
