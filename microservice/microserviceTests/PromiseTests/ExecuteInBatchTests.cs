using Beamable.Common;
using Beamable.Common.Api.Content;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace microserviceTests.PromiseTests
{
	[TestFixture]
	public class ExecuteInBatchTests
	{
		[SetUp]
		[TearDown]
		public void ResetContentInstance()
		{
			ContentApi.Instance = new Promise<IContentApi>();
		}

		[Test]
		[NonParallelizable]
		public async Task AllTestSucceed()
		{
			const int promiseCount = 100000;
			const int batchSize = 100;

			var promiseGenerators = new List<Func<Promise<int>>>();
			var promises = new List<Promise<int>>();
			var value = 0;
			var batchNumber = 0;
			for (var i = 0; i < promiseCount; i++)
			{
				var index = i;
				var promise = new Promise<int>();
				promises.Add(promise);
				promiseGenerators.Add(() =>
				{
					var computedBatch = index / batchSize;
					Assert.IsTrue(computedBatch == batchNumber);

				// assert that a generator is never run at the wrong time.
				Assert.IsTrue(index == value);
					value++;

					if (value % batchSize == 0 && value != 0)
					{
						batchNumber++;
					}
					return promise;
				});
			}

			var serialPromise = Promise.ExecuteInBatch(batchSize, promiseGenerators);

			var tasks = new List<Task>();

			for (var i = 0; i < promiseCount; i++)
			{
				var index = i;
				var task = Task.Run(async () =>
			   {
				   await Task.Yield();

				   var promise = promises[index];

				   promise.CompleteSuccess(index);
			   });
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			var result = await serialPromise;
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
