using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using NUnit.Framework;

namespace microserviceTests.PromiseTests
{
   [TestFixture]
   public class ExecuteRollingTests
   {
      [Test]
      public async Task AllSucceed()
      {
         const int promiseCount = 10000;

         var promiseGenerators = new List<Func<Promise<int>>>();
         var promises = new List<Promise<int>>();

         for (var i = 0; i < promiseCount; i++)
         {
            var promise = new Promise<int>();
            promises.Add(promise);
            promiseGenerators.Add(() => promise);
         }

         var serialPromise = Promise.ExecuteRolling(100, promiseGenerators);

         var tasks = new List<Task>();
         for (var i = 0; i < promiseCount; i++)
         {
            var index = i;
            var task = Task.Run( async () =>
            {
               await Task.Yield();

               var promise = promises[index];
               promise.CompleteSuccess(index);
            });
            tasks.Add(task);
         }

         await Task.WhenAll(tasks);

         try
         {
            var result = await serialPromise;
            Assert.AreEqual(promiseCount, result.Count);
            Assert.AreEqual(true, serialPromise.HasProcessedAllEntries);

            // check that ordering is maintained.
            for (var i = 0; i < promiseCount; i++)
            {
               var promise = promises[i];
               Assert.AreEqual(true, promise.IsCompleted);
               Assert.AreEqual(i, promise.GetResult());
            }
         }
         catch (Exception ex)
         {
            Assert.Fail(ex.Message);
         }
      }
   }
}