using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Beamable.Common;
using NUnit.Framework;

namespace microserviceTests.PromiseTests
{
   [TestFixture]
   public class ExecuteRollingTests
   {
      [Test]
      [TimeoutWithTeardown(8 * 1000)]
      public async Task AllSucceed()
      {
         const int promiseCount = 100000;

         var promiseGenerators = new List<Func<Promise<int>>>();
         var promises = new List<Promise<int>>();

         for (var i = 0; i < promiseCount; i++)
         {
            var promise = new Promise<int>();
            promises.Add(promise);
            promiseGenerators.Add(() => promise);
         }

         // var sw = new Stopwatch();
         // sw.Start();
         var serialPromise = Promise.ExecuteInBatchSequence(100, promiseGenerators);

         // Console.WriteLine(sw.ElapsedMilliseconds);
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
