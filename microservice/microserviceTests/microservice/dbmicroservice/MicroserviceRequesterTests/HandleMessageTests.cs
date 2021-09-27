using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice.dbmicroservice.MicroserviceRequesterTests
{
   [TestFixture]
   public class HandleMessageTests
   {
      [SetUp]
      [TearDown]
      public void ResetContentInstance()
      {
         ContentApi.Instance = new Promise<IContentApi>();
      }

      [Test]
      [Timeout(2 * 60 * 1000)]
      public async Task EventSubscriptionMultiThreadedAccess()
      {
         var context = new SocketRequesterContext(() =>
            throw new NotImplementedException("This test should never access the socket"));


         const int threadCount = 500;
         const int cycleCount = 1000;

         const string eventName = "test";
         const string eventPath = "event/test";


         Exception failure = null;

         Task Launch(int threadNumber)
         {

            var task = Task.Run(() =>
            {
               try
               {
                  var subscription = context.Subscribe<int>(eventName, _ =>
                  {
                     // do nothing...
                  });

                  for (var i = 0; i < cycleCount; i++)
                  {
                     var id = (threadNumber * cycleCount) + i;
                     var rc = new RequestContext("cid", "pid", id, 200, 1, eventPath, "get", "1", new HashSet<string>());
                     context.HandleMessage(rc, "");
                  }
               }
               catch (Exception ex)
               {
                  failure = ex;

               }
            });
            return task;
         }

         var threads = new List<Task>();
         for (var i = 0; i < threadCount; i++)
         {
            threads.Add(Launch(i));
         }

         await Task.WhenAll(threads);

         if (failure != null)
         {
            Assert.Fail("Failed thread. " + failure.Message + " " + failure.StackTrace);
         }

      }
   }
}