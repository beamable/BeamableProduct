
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using NUnit.Framework;
namespace microserviceTests.PromiseTests
{
   [TestFixture]
   public class ExecuteSeriallyTests
   {
      [Test]
      public async Task AllPromisesSucceed()
      {
         const int promiseCount = 100000;

         var promiseGenerators = new List<Func<Promise<int>>>();
         var promises = new List<Promise<int>>();
         var value = 0;
         for (var i = 0; i < promiseCount; i++)
         {
            var index = i;
            var promise = new Promise<int>();
            promises.Add(promise);
            promiseGenerators.Add(() =>
            {
               // assert that a generator is never run at the wrong time.
               Assert.IsTrue(index == value);
               value++;

               return promise;
            });
         }

         var startThread = Thread.CurrentThread;
         var serialPromise = Promise.ExecuteSerially(promiseGenerators);

         var tasks = new List<Task>();
         for (var i = 0; i < promiseCount; i++)
         {
            var index = i;
            var task = Task.Run( async () =>
            {
               await Task.Delay(1);;
               var promise = promises[index];
               promise.CompleteSuccess(index);

            });
            tasks.Add(task);
         }

         //await Task.WhenAll(tasks);
         Task.WaitAll(tasks.ToArray());
         var result = await serialPromise;
         var finishThread = Thread.CurrentThread;
         Assert.AreEqual(startThread.ManagedThreadId, finishThread.ManagedThreadId);
         Assert.IsTrue(serialPromise.IsCompleted);

         Assert.AreEqual(PromiseBase.Unit, result);

         // check that ordering is maintained.
         for (var i = 0; i < promiseCount; i++)
         {
            var promise = promises[i];
            Assert.AreEqual(true, promise.IsCompleted);
            Assert.AreEqual(i, promise.GetResult());
         }
      }
   }
}
