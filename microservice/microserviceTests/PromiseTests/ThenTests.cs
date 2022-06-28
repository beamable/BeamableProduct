using Beamable.Common;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace microserviceTests.PromiseTests
{
	[TestFixture]
	public class ThenTests
	{
		[Test]
		public async Task FlatmapForksIntoAsync()
		{
			for (var i = 0; i < 5000; i++)
			{
				var promise = new Promise<int>();
				Task<int> task = null;
				var nextPromise = promise.FlatMap((x) =>
				{
					task = Task.Run<int>(async () =>
				{
					   await Task.Yield();
					   return x + 1;
				   });
					return task.ToPromise();
				});

				promise.CompleteSuccess(3);

				await nextPromise;
				Assert.IsTrue(nextPromise.IsCompleted);
				Assert.AreEqual(4, nextPromise.GetResult());
			}
		}

		[Test]
		public async Task MultithreadedMapChain()
		{
			var promise = new Promise<int>();

			var taskCount = 5000;
			var curr = promise;
			var tasks = new List<Task>();
			for (var i = 0; i < taskCount; i++)
			{
				tasks.Add(Task.Run(async () =>
				{
					await Task.Yield();
					lock (promise)
					{
						curr = curr.Map(x => x + 1);
					}
				}));
			}

			promise.CompleteSuccess(1);
			await Task.WhenAll(tasks);

			Assert.IsTrue(curr.IsCompleted);
			Assert.AreEqual(1 + taskCount, curr.GetResult());
		}


		[Test]
		public async Task ManyThreadsAttachingCallbacks()
		{
			var promise = new Promise<int>();
			var history = new ConcurrentDictionary<int, int>();

			var halfTaskCount = 10000;
			var tasks = new List<Task>();

			void AttachALotOfThens(int offset)
			{
				for (var i = 0; i < halfTaskCount; i++)
				{
					var index = i + offset; // avoid closure change
					tasks.Add(Task.Run(async () =>
					{
						await Task.Yield();
						promise.Then(value =>
				   {
						   if (!history.TryAdd(index, value))
						   {
							   Assert.Fail("history slot already taken for " + index);
						   }
					   });
					}));
				}
			}

			AttachALotOfThens(0);
			var completionTask = Task.Run(async () =>
			{
				await Task.Yield();
				promise.CompleteSuccess(3);
			});
			AttachALotOfThens(halfTaskCount);

			await completionTask;
			await Task.WhenAll(tasks);

			Assert.AreEqual(halfTaskCount * 2, history.Count);
		}

		[Test]
		public async Task ManyThreadsAttachingErrors()
		{
			var promise = new Promise<int>();
			var history = new ConcurrentDictionary<int, Exception>();

			var halfTaskCount = 10000;
			var tasks = new List<Task>();

			void AttachALotOfErrors(int offset)
			{
				for (var i = 0; i < halfTaskCount; i++)
				{
					var index = i + offset; // avoid closure change
					tasks.Add(Task.Run(async () =>
					{
						await Task.Yield();
						promise.Error(ex =>
				   {
						   if (!history.TryAdd(index, ex))
						   {
							   Assert.Fail("history slot already taken for " + index);
						   }
					   });
					}));
				}
			}

			AttachALotOfErrors(0);
			var completionTask = Task.Run(async () =>
			{
				await Task.Yield();
				promise.CompleteError(new Exception());
			});
			AttachALotOfErrors(halfTaskCount);

			await completionTask;
			await Task.WhenAll(tasks);

			Assert.AreEqual(halfTaskCount * 2, history.Count);
		}

		[Test]
		public async Task ManyThreadsAttachingFlatmaps()
		{
			var promise = new Promise<int>();
			var history = new ConcurrentDictionary<int, int>();

			var halfTaskCount = 10000;
			var tasks = new List<Task>();

			void AttachALotOfFlatMaps(int offset)
			{
				for (var i = 0; i < halfTaskCount; i++)
				{
					var index = i + offset; // avoid closure change
					tasks.Add(Task.Run(async () =>
					{
						await Task.Yield();
						await promise.FlatMap(value =>
				   {
						   return Task.Run(async () =>
					  {
							  await Task.Yield();
							  return value + 1;
						  }).ToPromise();
					   }).Then(value =>
					   {
						   if (!history.TryAdd(index, value))
						   {
							   Assert.Fail("history slot already taken for " + index);
						   }
					   });
					}));
				}
			}

			AttachALotOfFlatMaps(0);
			var completionTask = Task.Run(async () =>
			{
				await Task.Yield();
				promise.CompleteSuccess(3);
			});
			AttachALotOfFlatMaps(halfTaskCount);

			await completionTask;
			await Task.WhenAll(tasks);

			Assert.AreEqual(halfTaskCount * 2, history.Count);
		}

		[Test]
		public async Task ManyThreadsAttachingMaps()
		{
			var promise = new Promise<int>();
			var history = new ConcurrentDictionary<int, int>();

			var halfTaskCount = 10000;
			var tasks = new List<Task>();

			void AttachALotOfMaps(int offset)
			{
				for (var i = 0; i < halfTaskCount; i++)
				{
					var index = i + offset; // avoid closure change
					tasks.Add(Task.Run(async () =>
					{
						await Task.Yield();
						promise.Map(value => value + 1).Then(value =>
				   {
						   if (!history.TryAdd(index, value))
						   {
							   Assert.Fail("history slot already taken for " + index);
						   }
					   });
					}));
				}
			}

			AttachALotOfMaps(0);
			var completionTask = Task.Run(async () =>
			{
				await Task.Yield();
				promise.CompleteSuccess(3);
			});
			AttachALotOfMaps(halfTaskCount);

			await completionTask;
			await Task.WhenAll(tasks);

			Assert.AreEqual(halfTaskCount * 2, history.Count);
		}
	}
}
