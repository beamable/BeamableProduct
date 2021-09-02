using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Content;
using NUnit.Framework;

namespace microserviceTests.microservice.Content
{
   [TestFixture]
   public class GetContentTests
   {
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

            // don't mock anything...
         });

         var socket = socketProvider.Create("test");
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx);

         var contentService = new ContentService(requester, socketCtx, contentResolver);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc, data);
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
               ;

            // don't mock anything...
         });

         var socket = socketProvider.Create("test");
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx);

         var contentService = new ContentService(requester, socketCtx, contentResolver);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc, data);
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

            // don't mock anything...
         });

         var socket = socketProvider.Create("test");
         var socketCtx = new SocketRequesterContext(() => Promise<IConnection>.Successful(socket));
         var requester = new MicroserviceRequester(args, reqCtx, socketCtx);

         var contentService = new ContentService(requester, socketCtx, contentResolver);

         testSocket.Connect();
         testSocket.OnMessage((_, data, id) =>
         {
            data.TryBuildRequestContext(args, out var rc);
            socketCtx.HandleMessage(rc, data);
         });


         contentService.Init();

         var tasks = new List<Task>();
         for (var i = 0; i < timesToGetContent; i++)
         {
            tasks.Add(Task.Run(async () =>
            {

               var fetchPromise = contentService.GetContent("items.foo");
               var fetchTask = Task.Run(async () => await fetchPromise);
               fetchTask.Wait(10);
               Assert.IsTrue(fetchPromise.IsCompleted);
               Assert.AreEqual("items.foo", fetchPromise.GetResult().Id);
            }));
         }

         await Task.WhenAll(tasks);
         Assert.AreEqual(1, fetchCounter);

         Assert.IsTrue(testSocket.AllMocksCalled());
      }
   }
}