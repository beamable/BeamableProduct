using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using NUnit.Framework;

namespace microserviceTests.PromiseTests
{
   [TestFixture]
   public class MultithreadedCompletionTests
   {
      [Test]
      public async Task CompletionFromAnotherThread()
      {
         for (var x = 0; x < 1000; x++)
         {
            var promise = new Promise<int>();

            var earlyCbCalled = false;
            var lateCbCalled = false;

            promise.Then(_ =>
            {
               Assert.IsFalse(earlyCbCalled);
               earlyCbCalled = true;
            });

            var awaitingTask = Task.Run(async () =>
            {
               await promise;
            });

            var taskCount = 1000;
            var tasks = new List<Task>();
            for (var i = 0; i < taskCount; i++)
            {
               var index = i;
               var task = Task.Run(async () =>
               {
                  await Task.Delay(1);;
                  promise.CompleteSuccess(
                     index); // it isn't garunteed which task will get here first, but only one should.
               });
               tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            await awaitingTask;

            Assert.IsTrue(earlyCbCalled);
            promise.Then(_ =>
            {
               Assert.IsFalse(lateCbCalled);
               lateCbCalled = true;
            });

            Assert.IsTrue(lateCbCalled);
         }
      }

      [Test]
      public async Task FailureFromAnotherThread()
      {
         for (var x = 0; x < 1000; x++)
         {
            var promise = new Promise<int>();

            var earlyCbCalled = false;
            var lateCbCalled = false;

            promise.Error(_ =>
            {
               Assert.IsFalse(earlyCbCalled);
               earlyCbCalled = true;
            });

            var awaitingTask = Task.Run(async () =>
            {
               await promise;
            });

            var taskCount = 1000;
            var tasks = new List<Task>();
            for (var i = 0; i < taskCount; i++)
            {
               var index = i;
               var task = Task.Run(async () =>
               {
                  await Task.Delay(1);;
                  promise.CompleteError(
                     new Exception()); // it isn't garunteed which task will get here first, but only one should.
               });
               tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            Assert.ThrowsAsync<Exception>(async () =>
            {
               await awaitingTask;
            });

            Assert.IsTrue(earlyCbCalled);
            promise.Error(_ =>
            {
               Assert.IsFalse(lateCbCalled);
               lateCbCalled = true;
            });

            Assert.IsTrue(lateCbCalled);
         }
      }
   }
}
