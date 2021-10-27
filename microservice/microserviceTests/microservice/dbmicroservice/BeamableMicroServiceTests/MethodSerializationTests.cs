using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using microserviceTests.microservice.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
   [TestFixture]
   public class MethodSerializationTests
   {
      [SetUp]
      [TearDown]
      public void ResetContentInstance()
      {
         ContentApi.Instance = new Promise<IContentApi>();
      }

      [Test]
      [NonParallelizable]
      public async Task Call_Array_Null()
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
                     .WithPayload<int>(n => n == 0),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Sum", 1, 0, null));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }


      [Test]
      [NonParallelizable]
      public async Task Call_Array_Empty()
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
                     .WithPayload<int>(n => n == 0),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Sum", 1, 0, new int[]{}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_Array_WithParams()
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
                     .WithPayload<int>(n => n == 6),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Sum", 1, 0, new int[]{1,2,3}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays_BothNull()
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
                     .WithPayload<int>(n => n == 0),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 0, null, null));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays_FirstNull()
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 0, null, new int[]{1,2}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays_SecondNull()
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 0, new int[]{1,2}, null));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays()
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
                     .WithPayload<int>(n => n == 10),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 0, new int[]{1,2}, new int[]{3,4}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }
      
      [Test]
      [NonParallelizable]
      public async Task Call_PromiseMethod()
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
                     .WithPayload<int>(n => n == 1),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "PromiseTestMethod", 1, 0));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }
      
      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithJSON_AsParameter()
      {
         LoggingUtil.Init();
         TestSocket testSocket = null;

         var req = new
         {
            testIntVal1 = 12345,
            testIntVal2 = 12345
         };
         
         string serialized = JsonConvert.SerializeObject(req);
         JToken  json = JToken.Parse(serialized);
         
         var ms = new BeamableMicroService(new TestSocketProvider(socket =>
            {
               testSocket = socket;
               socket.AddStandardMessageHandlers()
                  .AddMessageHandler(
                     MessageMatcher
                        .WithReqId(1)
                        .WithStatus(200)
                        .WithPayload<string>(n =>
                           {
                              return JToken.DeepEquals((JToken)n, json);
                           }
                        ),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);


         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithJSON_AsParameter", 1, 0, serialized));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }
      
      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithRegularString_AsParameter()
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
                     .WithPayload<string>(n => string.Equals(n, "test_String")),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);
         
         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithRegularString_AsParameter", 1, 0, "test_String"));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }
   }
}