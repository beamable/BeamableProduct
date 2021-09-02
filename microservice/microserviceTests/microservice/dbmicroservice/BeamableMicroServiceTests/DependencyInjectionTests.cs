using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Microservice.Tests.Socket;
using Beamable.Server;
using microserviceTests.microservice.Util;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.BeamableMicroServiceTests
{
   [TestFixture]
   public class DependencyInjectionTests
   {
      [SetUp]
      [TearDown]
      public void ResetContentInstance()
      {
         ContentApi.Instance = new Promise<IContentApi>();
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
                     .WithPayload<bool>(n => n),
                  MessageResponder.NoResponse(),
                  MessageFrequency.OnlyOnce()
               );
         }));

         await ms.Start<ServiceWithDependency>(new TestArgs());
         Assert.IsTrue(ms.HasInitialized);

         testSocket.SendToClient(ClientRequest.ClientCallable("micro_serviceWithDeps", "NoNulls", 1, 0));

         // simulate shutdown event...
         await ms.OnShutdown(this, null);
         Assert.IsTrue(testSocket.AllMocksCalled());
      }


   }
}