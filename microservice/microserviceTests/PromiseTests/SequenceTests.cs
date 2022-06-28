using Beamable.Common;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace microserviceTests.PromiseTests
{
	[TestFixture]
	public class SequenceTests
	{
		[Test]
		public async Task AllPromisesSucceed()
		{
			const int promiseCount = 10000;

			var promises = new List<Promise<int>>();
			for (var i = 0; i < promiseCount; i++)
			{
				promises.Add(new Promise<int>());
			}
			var sequencePromise = Promise.Sequence(promises);

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

			Assert.AreEqual(true, sequencePromise.IsCompleted);

			var results = await sequencePromise;
			Assert.AreEqual(promiseCount, results.Count);

			// check that ordering is maintained.
			for (var i = 0; i < promiseCount; i++)
			{
				var value = results[i];
				Assert.AreEqual(i, value);
			}
		}
	}
}
