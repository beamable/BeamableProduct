using System;
using System.Collections.Generic;
using System.Threading;
using Beamable.Server;
using NUnit.Framework;


namespace microserviceTests.microservice.dbmicroservice.MicroserviceRequesterTests
{
   [TestFixture]
   public class AddListenerTests
   {
      [Test]
      public void MultiThreadedAccess()
      {
         var context = new SocketRequesterContext(() =>
            throw new NotImplementedException("This test should never access the socket"));

         const int threadCount = 500;
         const int cycleCount = 50000;

         const string uri = "uri";
         Func<string, object> dumbParser = (raw) => 1;

         Exception failure = null;

         Thread Launch(int threadNumber)
         {
            var thread = new Thread(() =>
            {
               try
               {

                  for (var i = 0; i < cycleCount; i++)
                  {
                     var id = (threadNumber * cycleCount) + i;
                     var req = new WebsocketRequest {id = id};
                     context.AddListener(req, uri, dumbParser);
                     Thread.Sleep(1);
                  }
               }
               catch (Exception ex)
               {
                  failure = ex;

               }
            });
            thread.Start();
            return thread;
         }

         var threads = new List<Thread>();
         for (var i = 0; i < threadCount; i++)
         {
            threads.Add(Launch(i));
         }

         // wait for all threads to terminate...
         foreach (var thread in threads)
         {
            thread.Join(10);
         }

         if (failure != null)
         {
            Assert.Fail("Failed thread. " + failure.Message + " " + failure.StackTrace);
         }

      }
   }
}