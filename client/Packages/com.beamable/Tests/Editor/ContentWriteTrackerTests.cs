using System;
using Beamable.Common;
using Beamable.Editor.ContentService;
using NUnit.Framework;

namespace Beamable.Tests.Editor
{
	public class ContentWriteTrackerTests
	{
		[Test]
		public void DeleteWaitsForEveryWriteEvenWhenTheyFinishOutOfOrder()
		{
			var tracker = new ContentWriteTracker();
			var first = new Promise();
			var second = new Promise();
			tracker.Run("realm/global/currency.gems", () => first);
			tracker.Run("realm/global/currency.gems", () => second);
			bool deleted = false;
			var deletion = tracker.Delete("realm/global/currency.gems", () => deleted = true);

			second.CompleteSuccess();
			Assert.That(deleted, Is.False);
			Assert.That(deletion.IsCompleted, Is.False);
			first.CompleteSuccess();
			Assert.That(deleted, Is.True);
			Assert.That(deletion.IsCompleted, Is.True);
			Assert.That(tracker.HasWrites("realm/global/currency.gems"), Is.False);
		}

		[Test]
		public void FailedWriteDoesNotReleaseDeletionWhileAnotherWriteIsPending()
		{
			var tracker = new ContentWriteTracker();
			var first = new Promise();
			var second = new Promise();
			Exception observed = null;
			tracker.Run("key", () => first).Error(e => observed = e);
			tracker.Run("key", () => second);
			bool deleted = false;
			var deletion = tracker.Delete("key", () => deleted = true);
			var failure = new Exception("Write failed");
			first.CompleteError(failure);

			Assert.That(observed, Is.SameAs(failure));
			Assert.That(deleted, Is.False);
			second.CompleteSuccess();
			deletion.GetResult();
			Assert.That(deleted, Is.True);
		}

		[Test]
		public void PendingDeleteBlocksNewWritesAndSharesItsCompletion()
		{
			var tracker = new ContentWriteTracker();
			var write = new Promise();
			tracker.Run("key", () => write);
			int deletes = 0;
			var deletion = tracker.Delete("key", () => deletes++);
			Assert.That(tracker.Delete("key", () => deletes++), Is.SameAs(deletion));
			bool dispatched = false;
			tracker.Run("key", () => { dispatched = true; return Promise.Success; });
			Assert.That(dispatched, Is.False);
			write.CompleteSuccess();
			Assert.That(deletes, Is.EqualTo(1));
			Assert.That(tracker.IsDeleting("key"), Is.False);
		}

		[Test]
		public void DifferentContentOrScopeCanWriteDuringDeletion()
		{
			var tracker = new ContentWriteTracker();
			var write = new Promise();
			tracker.Run("realm/global/gems", () => write);
			tracker.Delete("realm/global/gems", () => { });
			int writes = 0;
			tracker.Run("realm/global/coins", () => { writes++; return Promise.Success; });
			tracker.Run("other/global/gems", () => { writes++; return Promise.Success; });
			Assert.That(writes, Is.EqualTo(2));
			write.CompleteSuccess();
		}

		[Test]
		public void DeleteFailurePropagatesAndAllowsRetry()
		{
			var tracker = new ContentWriteTracker();
			var failure = new Exception("File is locked");
			Exception observed = null;
			tracker.Delete("key", () => throw failure).Error(e => observed = e);
			Assert.That(observed, Is.SameAs(failure));
			Assert.That(tracker.IsDeleting("key"), Is.False);
			bool deleted = false;
			tracker.Delete("key", () => deleted = true).GetResult();
			Assert.That(deleted, Is.True);
		}

		[Test]
		public void SynchronousDispatchFailureDoesNotLeakPendingWrite()
		{
			var tracker = new ContentWriteTracker();
			Exception observed = null;
			tracker.Run("key", () => throw new Exception("Cannot dispatch")).Error(e => observed = e);
			Assert.That(observed, Is.Not.Null);
			Assert.That(tracker.HasWrites("key"), Is.False);
			Assert.That(tracker.Delete("key", () => { }).IsCompleted, Is.True);
		}
	}
}
