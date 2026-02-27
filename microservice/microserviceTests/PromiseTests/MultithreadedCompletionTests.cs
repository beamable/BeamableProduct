using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using microserviceTests.OpenAPITests;
using NUnit.Framework;

namespace microserviceTests.PromiseTests
{
   [TestFixture]
   public class MultithreadedCompletionTests
   {
      [Test]
      [Timeout(1000)]
      public async Task Then_CanDeadlock_WhenCallbackBlocksOnAnotherPromise()
      {
         var p1 = new Promise<int>();

         p1.CompleteSuccess(12);
         var invoked = false;
         var waitForFinish = new TaskCompletionSource();
         var t1 = new Thread(() =>
         {
            p1.Then(_ =>
            {
               TaskCompletionSource s = new TaskCompletionSource();
               var t2 = new Thread(() =>
               {
                  Thread.Sleep(10);
                  p1.Then(_ =>
                  {
                     // complete the task, but this .Then will never enter, because it is held open
                     s.SetResult();
                  });
               });
               t2.Start();
               
               s.Task.Wait(); // wait for the source to complete.

               waitForFinish.SetResult();
            });
         });
         t1.Start();

         await waitForFinish.Task;
      }
      [Test]
      [Timeout(1000)]
      public async Task Error_CanDeadlock_WhenCallbackBlocksOnAnotherPromise()
      {
         var p1 = new Promise<int>();

         p1.CompleteError(new Exception("fake"));
         var invoked = false;
         var waitForFinish = new TaskCompletionSource();
         var t1 = new Thread(() =>
         {
            p1.Error(_ =>
            {
               TaskCompletionSource s = new TaskCompletionSource();
               var t2 = new Thread(() =>
               {
                  Thread.Sleep(10);
                  p1.Error(_ =>
                  {
                     // complete the task, but this .Then will never enter, because it is held open
                     s.SetResult();
                  });
               });
               t2.Start();
               
               s.Task.Wait(); // wait for the source to complete.

               waitForFinish.SetResult();
            });
         });
         t1.Start();

         await waitForFinish.Task;
      }
      
      [Test]
      [Timeout(1000)]
      public void CompleteError_DeadlockExample()
      {
         var p1 = new Promise<int>();
         var p2 = new Promise<int>();

         // Register cross callbacks
         p1.Error(ex =>
         {
            // tries to fail the other promise while holding p1._lock
            p2.CompleteError(new Exception("p1 -> p2"));
         });

         p2.Error(ex =>
         {
            // tries to fail the other promise while holding p2._lock
            p1.CompleteError(new Exception("p2 -> p1"));
         });

         // Complete on separate threads to trigger lock inversion
         var t1 = Task.Run(() => p1.CompleteError(new Exception("start p1")));
         var t2 = Task.Run(() => p2.CompleteError(new Exception("start p2")));

         // Deadlock happens here — these tasks block forever
         Task.WaitAll(t1, t2);
      }
      
      [Test]
      [Timeout(1000)]
      public void CompleteSuccess_PromiseDeadlockExample()
      {
         var p1 = new Promise<int>();
         var p2 = new Promise<int>();

         // Thread 1: completes p1 after a small delay
         var t1 = Task.Run(() =>
         {
            Thread.Sleep(10);
            p1.CompleteSuccess(42);
         });

         // Thread 2: completes p2 after a small delay
         var t2 = Task.Run(() =>
         {
            Thread.Sleep(10);
            p2.CompleteSuccess(99);
         });

         // Cross-register callbacks that will try to complete the other promise
         p1.Then(_ =>
         {
            // This will attempt to lock p2._lock while holding p1._lock
            p2.CompleteSuccess(123);
         });

         p2.Then(_ =>
         {
            // This will attempt to lock p1._lock while holding p2._lock
            p1.CompleteSuccess(456);
         });

         // Wait for both tasks (deadlock will occur here)
         Task.WaitAll(t1, t2); 
         // <-- this will never complete because of the deadlock
      }
      
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
