using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using microservice.Common;
using NUnit.Framework;

namespace microserviceTests.microservice.Common;

[TestFixture]
public class CacheTests
{
   [Test]
   public async Task FaultedTaskIsRemovedAndNextGetRetries()
   {
      var calls = 0;
      var cache = new Cache<string, int>(_ =>
      {
         calls++;
         return calls == 1
            ? Task.FromException<int>(new InvalidOperationException("temporary failure"))
            : Task.FromResult(42);
      });

      Assert.ThrowsAsync<InvalidOperationException>(async () => await cache.Get("key"));

      Assert.That(await cache.Get("key"), Is.EqualTo(42));
      Assert.That(calls, Is.EqualTo(2));
   }

   [Test]
   public async Task CanceledTaskIsRemovedAndNextGetRetries()
   {
      var calls = 0;
      var canceledToken = new CancellationToken(true);
      var cache = new Cache<string, int>(_ =>
      {
         calls++;
         return calls == 1
            ? Task.FromCanceled<int>(canceledToken)
            : Task.FromResult(42);
      });

      Assert.ThrowsAsync<TaskCanceledException>(async () => await cache.Get("key"));

      Assert.That(await cache.Get("key"), Is.EqualTo(42));
      Assert.That(calls, Is.EqualTo(2));
   }

   [Test]
   public async Task SuccessfulTaskRemainsCached()
   {
      var calls = 0;
      var cache = new Cache<string, int>(_ =>
      {
         calls++;
         return Task.FromResult(42);
      });

      Assert.That(await cache.Get("key"), Is.EqualTo(42));
      Assert.That(await cache.Get("key"), Is.EqualTo(42));
      Assert.That(calls, Is.EqualTo(1));
   }

   [Test]
   public async Task ConcurrentCallersShareOneResolverTask()
   {
      var calls = 0;
      var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
      var cache = new Cache<string, int>(_ =>
      {
         Interlocked.Increment(ref calls);
         return completion.Task;
      });

      var requests = new List<Task<int>>();
      for (var i = 0; i < 20; i++)
      {
         requests.Add(cache.Get("key"));
      }

      completion.SetResult(42);

      Assert.That(await Task.WhenAll(requests), Is.All.EqualTo(42));
      Assert.That(calls, Is.EqualTo(1));
   }

   [Test]
   public async Task OldFailureDoesNotRemoveNewerEntry()
   {
      var calls = 0;
      var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
      var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
      var cache = new Cache<string, int>(_ =>
      {
         calls++;
         return calls == 1 ? first.Task : second.Task;
      });

      var oldTask = cache.Get("key");
      cache.Purge("key");
      var newTask = cache.Get("key");

      first.SetException(new InvalidOperationException("old failure"));
      Assert.ThrowsAsync<InvalidOperationException>(async () => await oldTask);

      second.SetResult(42);
      Assert.That(await newTask, Is.EqualTo(42));
      Assert.That(await cache.Get("key"), Is.EqualTo(42));
      Assert.That(calls, Is.EqualTo(2));
   }
}
