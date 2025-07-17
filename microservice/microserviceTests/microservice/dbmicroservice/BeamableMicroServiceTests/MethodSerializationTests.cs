using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Inventory;
using System.Threading.Tasks;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using Beamable.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
   [TestFixture]
   public class MethodSerializationTests : CommonTest
   {
	   [Test]
	   [NonParallelizable]
	   public async Task Call_BadInput()
	   {
		   allowErrorLogs = true;
		   TestSocket testSocket = null;
		   var ms = new TestSetup(new TestSocketProvider(socket =>
		   {
			   testSocket = socket;
			   socket.AddStandardMessageHandlers()
				   .AddMessageHandler(
					   MessageMatcher
						   .WithReqId(1)
						   .WithStatus(400),
					   MessageResponder.NoResponse(),
					   MessageFrequency.OnlyOnce()
				   );
		   }));

		   await ms.Start<SimpleMicroservice>(new TestArgs());
		   Assert.IsTrue(ms.HasInitialized);

		   testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", nameof(SimpleMicroservice.Add), 1, 1, null, 3));

		   // simulate shutdown event...
		   await ms.OnShutdown(this, null);
		   AssetBadInputError();
		   Assert.IsTrue(testSocket.AllMocksCalled());
	   }

	   [Test]
      [NonParallelizable]
      public async Task Call_Array_Null()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Sum", 1, 1, null));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }


      [Test]
      [NonParallelizable]
      public async Task Call_Array_Empty()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Sum", 1, 1, new int[]{}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_Array_WithParams()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "Sum", 1, 1, new int[]{1,2,3}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays_BothNull()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 1, null, null));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays_FirstNull()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 1, null, new int[]{1,2}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays_SecondNull()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 1, new int[]{1,2}, null));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MultipleArrays()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "TwoArrays", 1, 1, new int[]{1,2}, new int[]{3,4}));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      
      [TestCase(nameof(SimpleMicroservice.Random1))]
      [TestCase(nameof(SimpleMicroservice.Random2))]
      [TestCase(nameof(SimpleMicroservice.Random3))]
      [TestCase(nameof(SimpleMicroservice.Random4))]
      [NonParallelizable]
      public async Task GH_Issue_4156(string methodName)
      {

          TestSocket testSocket = null;
          var ms = new TestSetup(new TestSocketProvider(socket =>
          {
              testSocket = socket;
              socket.AddStandardMessageHandlers()
                  .AddMessageHandler(
                      MessageMatcher
                          .WithReqId(1)
                          .WithStatus(200)
                          .WithPayload<RandomResponse>(rr => rr.number == 123),
                      MessageResponder.NoResponse(),
                      MessageFrequency.OnlyOnce()
                  );
          }));

          await ms.Start<SimpleMicroservice>(new TestArgs());
          Assert.IsTrue(ms.HasInitialized);

          testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", methodName, 1, 1));

          // simulate shutdown event...
          await ms.OnShutdown(this, null);
          Assert.IsTrue(testSocket.AllMocksCalled());
      }
      
      [Test]
      [NonParallelizable]
      public async Task Call_PromiseMethod()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "PromiseTestMethod", 1, 1));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_TypelessPromiseMethod()
      {

         TestSocket testSocket = null;
         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "PromiseTypelessTestMethod", 1, 1));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithJSON_AsParameter()
      {

         TestSocket testSocket = null;

         var req = new
         {
            testIntVal1 = 12345,
            testIntVal2 = 12345
         };

         string serialized = JsonConvert.SerializeObject(req);
         JToken  json = JToken.Parse(serialized);

         var ms = new TestSetup(new TestSocketProvider(socket =>
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


         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithJSON_AsParameter", 1, 1, serialized));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithRegularString_AsParameter()
      {

         TestSocket testSocket = null;

         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithRegularString_AsParameter", 1, 1, "test_String"));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task XYZ()
      {

         TestSocket testSocket = null;

         var ms = new TestSetup(new TestSocketProvider(socket =>
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

         await ms.Start<SimpleMicroserviceNonLegacy>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         // testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithRegularString_AsParameter", 1, 0, "test_String"));

         testSocket.SendToClient(ClientRequest.ClientCallablePayloadArgs("micro_sample", "MethodWithRegularString_AsParameter", 1, 1, "[\"test_String\"]"));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithVector2Int_AsParameter()
      {

         TestSocket testSocket = null;

         var ms = new TestSetup(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithReqId(1)
                     .WithStatus(200)
                     .WithPayload<Vector2Int>(n => n.x == 10 & n.y == 20),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithVector2Int_AsParameter", 1, 1, new Vector2Int(10, 20)));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }
      
      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithInventoryView_AsParameter()
      {
	      InventoryView view = new InventoryView();
	      view.currencies.Add("xx", 1);


	      List<ItemView> itemViews = new List<ItemView>();

	      Dictionary<string, string> prop1 = new Dictionary<string, string>
	      {
		      {"A", "B"},
		      {"C", "D"}
	      };
	      
	      Dictionary<string, string> prop2 = new Dictionary<string, string>
	      {
		      {"E", "F"},
		      {"G", "H"}
	      };

	      itemViews.Add(new ItemView()
	      {
		      createdAt = 100,
		      updatedAt = 1,
		      properties = prop1
	      });
	      
	      itemViews.Add(new ItemView()
	      {
		      createdAt = 200,
		      updatedAt = 2,
		      properties = prop2
	      });

	      view.currencyProperties = new Dictionary<string, List<CurrencyProperty>>();


	      CurrencyProperty cr = new CurrencyProperty();
	      cr.name = "nn";
	      cr.value = "val1";
		     
	      view.currencyProperties.Add("prop1",new List<CurrencyProperty>(){cr});
	      view.items.Add("tt", itemViews);

	      TestSocket testSocket = null;
	      var ms = new TestSetup(new TestSocketProvider(socket =>
	      {
		      testSocket = socket;
		      socket.AddStandardMessageHandlers()
			      .AddMessageHandler(
				      MessageMatcher
					      .WithReqId(1)
					      .WithStatus(200)
					      .WithPayload<InventoryView>(v => string.Equals(JsonConvert.SerializeObject(v), JsonConvert.SerializeObject(view))),
				      MessageResponder.NoResponse(),
				      MessageFrequency.OnlyOnce()
			      );
	      }));

	      await ms.Start<SimpleMicroservice>(new TestArgs());
	      Assert.IsTrue(ms.HasInitialized);

	      testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithInventoryView_AsParameter", 1, 1, view));

	      // simulate shutdown event...
	      await ms.OnShutdown(this, null);
	      Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithException()
      {

         TestSocket testSocket = null;

         var ms = new TestSetup(new TestSocketProvider(socket =>
         {
            testSocket = socket;
            socket.AddStandardMessageHandlers()
               .AddMessageHandler(
                  MessageMatcher
                     .WithReqId(1)
                     .WithStatus(401).WithPayload<MicroserviceException>(ex => string.Equals(ex.Message,"test")),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<SimpleMicroservice>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", "MethodWithExceptionThrow", 1, 1, string.Empty));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         allowErrorLogs = true;
         Assert.AreEqual(1, GetBadLogs().Count());
         Assert.IsTrue(testSocket.AllMocksCalled());
      }

      [Test]
      [NonParallelizable]
      public async Task Call_MethodWithSendMail()
      {
	      TestSocket testSocket = null;

	      var ms = new TestSetup(new TestSocketProvider(socket =>
	      {
		      testSocket = socket;
		      socket.AddStandardMessageHandlers()
			      .AddMessageHandler(
				      MessageMatcher
					      .WithReqId(TestSocket.DEFAULT_FIRST_BEAMABLE_REQUEST), // outbound mail response...
				      MessageResponder.Success(new EmptyResponse()),
				      MessageFrequency.OnlyOnce()
			      )
			      .AddMessageHandler(
				      MessageMatcher
					      .WithReqId(1)
					      .WithStatus(200)
					      .WithPayload<EmptyResponse>(x => x!=null),
				      MessageResponder.NoResponse(),
				      MessageFrequency.OnlyOnce()
			      );
	      }));

	      await ms.Start<SimpleMicroservice>(new TestArgs());
	      Assert.IsTrue(ms.HasInitialized);

	      testSocket.SendToClient(ClientRequest.ClientCallable("micro_sample", nameof(SimpleMicroservice.MethodWithSendMail), 1, 1));

	      // simulate shutdown event...
	      await ms.OnShutdown(this, null);
	      Assert.IsTrue(testSocket.AllMocksCalled());
      }
   }
}
