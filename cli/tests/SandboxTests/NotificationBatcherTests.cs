using System;
using System.Linq;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class NotificationBatcherTests
{
	private DateTimeOffset _now;

	[SetUp]
	public void SetUp() => _now = DateTimeOffset.UnixEpoch;

	private DateTimeOffset Advance(TimeSpan delta) => _now += delta;

	[Test]
	public void EmptyBatcher_NoFlush()
	{
		var b = new NotificationBatcher();
		Assert.That(b.TryFlushIfDue(_now), Is.Null);
		Assert.That(b.FlushNow(_now), Is.Null);
	}

	[Test]
	public void SingleEvent_NotFlushedBeforeWindow()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.InvocationOutput("inv-1", "stream", "hello"), _now);

		Advance(TimeSpan.FromMilliseconds(500));
		Assert.That(b.TryFlushIfDue(_now), Is.Null);
	}

	[Test]
	public void SingleEvent_FlushedAtWindow()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.InvocationOutput("inv-1", "stream", "hello"), _now);

		Advance(TimeSpan.FromSeconds(1));
		var batch = b.TryFlushIfDue(_now);
		Assert.That(batch, Is.Not.Null);
		Assert.That(batch!.Events.Count, Is.EqualTo(1));
		Assert.That(batch.Sequence, Is.EqualTo(1));
	}

	[Test]
	public void ManyEventsInWindow_CollapseToSingleBatch()
	{
		var b = new NotificationBatcher();
		for (var i = 0; i < 100; i++)
		{
			b.Enqueue(new SandboxEvent.InvocationOutput("inv-1", "stream", $"line {i}"), _now);
			Advance(TimeSpan.FromMilliseconds(2)); // 100 events over 200 ms
		}

		Advance(TimeSpan.FromSeconds(1));
		var batch = b.TryFlushIfDue(_now);
		Assert.That(batch, Is.Not.Null);
		Assert.That(batch!.Events.Count, Is.EqualTo(100));
	}

	[Test]
	public void ThreeSecondsContinuousActivity_ProducesAtMostThreeBatches()
	{
		var b = new NotificationBatcher();
		var batches = 0;
		var elapsed = TimeSpan.Zero;
		while (elapsed < TimeSpan.FromSeconds(3))
		{
			b.Enqueue(new SandboxEvent.InvocationOutput("inv-1", "stream", "tick"), _now);
			Advance(TimeSpan.FromMilliseconds(50));
			elapsed += TimeSpan.FromMilliseconds(50);
			if (b.TryFlushIfDue(_now) is not null) batches++;
		}
		Assert.That(batches, Is.LessThanOrEqualTo(3));
	}

	[Test]
	public void QuietWindow_ProducesNoBatches()
	{
		var b = new NotificationBatcher();
		Advance(TimeSpan.FromSeconds(5));
		Assert.That(b.TryFlushIfDue(_now), Is.Null);
	}

	[Test]
	public void FileChanged_SameWatchIdAndPath_CollapseKinds()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.FileChanged("w1", "foo.cs", new[] { "created" }), _now);
		Advance(TimeSpan.FromMilliseconds(50));
		b.Enqueue(new SandboxEvent.FileChanged("w1", "foo.cs", new[] { "changed" }), _now);
		Advance(TimeSpan.FromMilliseconds(50));
		b.Enqueue(new SandboxEvent.FileChanged("w1", "foo.cs", new[] { "changed" }), _now);

		Advance(TimeSpan.FromSeconds(1));
		var batch = b.TryFlushIfDue(_now);
		Assert.That(batch, Is.Not.Null);
		Assert.That(batch!.Events.Count, Is.EqualTo(1));
		var fc = (SandboxEvent.FileChanged)batch.Events[0];
		Assert.That(fc.Kinds, Is.EquivalentTo(new[] { "created", "changed" }));
	}

	[Test]
	public void FileChanged_DifferentPath_KeptSeparate()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.FileChanged("w1", "foo.cs", new[] { "changed" }), _now);
		b.Enqueue(new SandboxEvent.FileChanged("w1", "bar.cs", new[] { "changed" }), _now);

		Advance(TimeSpan.FromSeconds(1));
		var batch = b.TryFlushIfDue(_now);
		Assert.That(batch!.Events.Count, Is.EqualTo(2));
	}

	[Test]
	public void OrderingPreserved_AcrossTypes()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.InvocationOutput("inv-1", "stream", "first"), _now);
		b.Enqueue(new SandboxEvent.FileChanged("w1", "a.cs", new[] { "changed" }), _now);
		b.Enqueue(new SandboxEvent.InvocationStatus("inv-1", "completed", 0), _now);

		Advance(TimeSpan.FromSeconds(1));
		var batch = b.TryFlushIfDue(_now);
		Assert.That(batch!.Events.Select(e => e.GetType()), Is.EqualTo(new[]
		{
			typeof(SandboxEvent.InvocationOutput),
			typeof(SandboxEvent.FileChanged),
			typeof(SandboxEvent.InvocationStatus),
		}));
	}

	[Test]
	public void FlushNow_ReturnsPendingImmediately()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.ShutdownImminent("deposed"), _now);
		var batch = b.FlushNow(_now);
		Assert.That(batch, Is.Not.Null);
		Assert.That(batch!.Events.Single(), Is.InstanceOf<SandboxEvent.ShutdownImminent>());
	}

	[Test]
	public void SequenceMonotonic()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.Connection("s1", "paired"), _now);
		Advance(TimeSpan.FromSeconds(1));
		var b1 = b.TryFlushIfDue(_now);

		b.Enqueue(new SandboxEvent.Connection("s1", "unpaired"), _now);
		Advance(TimeSpan.FromSeconds(1));
		var b2 = b.TryFlushIfDue(_now);

		Assert.That(b1!.Sequence, Is.EqualTo(1));
		Assert.That(b2!.Sequence, Is.EqualTo(2));
	}

	[Test]
	public void AfterFlush_NextEventStartsNewWindow()
	{
		var b = new NotificationBatcher();
		b.Enqueue(new SandboxEvent.InvocationOutput("inv-1", "stream", "a"), _now);
		Advance(TimeSpan.FromSeconds(1));
		Assert.That(b.TryFlushIfDue(_now), Is.Not.Null);

		// Right after flush — pending should be empty, and a fresh event should not be
		// considered "due" until another full window elapses.
		Advance(TimeSpan.FromMilliseconds(100));
		b.Enqueue(new SandboxEvent.InvocationOutput("inv-1", "stream", "b"), _now);
		Advance(TimeSpan.FromMilliseconds(500));
		Assert.That(b.TryFlushIfDue(_now), Is.Null);

		Advance(TimeSpan.FromMilliseconds(600));
		Assert.That(b.TryFlushIfDue(_now), Is.Not.Null);
	}
}
