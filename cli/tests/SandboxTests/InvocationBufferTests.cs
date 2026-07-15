using System.Linq;
using cli.Services.Sandbox;
using NUnit.Framework;

namespace tests.SandboxTests;

[TestFixture]
public class InvocationBufferTests
{
	[Test]
	public void AppendThenSnapshot_ReturnsAllLinesInOrder()
	{
		var b = new InvocationBuffer();
		b.Append("stream", "a");
		b.Append("stream", "b");
		b.Append("logs", "c");

		var page = b.Snapshot();
		Assert.That(page.Lines.Select(l => l.Line), Is.EqualTo(new[] { "a", "b", "c" }));
		Assert.That(page.Lines.Select(l => l.Sequence), Is.EqualTo(new long[] { 0, 1, 2 }));
		Assert.That(page.DroppedCount, Is.EqualTo(0));
		Assert.That(page.NextSequence, Is.EqualTo(3));
	}

	[Test]
	public void Snapshot_SinceSequence_ReturnsOnlyNewerLines()
	{
		var b = new InvocationBuffer();
		b.Append("stream", "a");
		b.Append("stream", "b");
		b.Append("stream", "c");

		var page = b.Snapshot(sinceSequence: 0); // sequence 0 = "a"; want > 0
		Assert.That(page.Lines.Select(l => l.Line), Is.EqualTo(new[] { "b", "c" }));
	}

	[Test]
	public void Snapshot_NoNewLinesSinceCursor_ReturnsEmpty()
	{
		var b = new InvocationBuffer();
		b.Append("stream", "a");
		var first = b.Snapshot();
		var second = b.Snapshot(sinceSequence: first.NextSequence - 1);
		Assert.That(second.Lines, Is.Empty);
		Assert.That(second.NextSequence, Is.EqualTo(first.NextSequence));
	}

	[Test]
	public void LineCap_DropsOldestAndRecordsCount()
	{
		var b = new InvocationBuffer(maxLines: 3, maxBytes: int.MaxValue);
		for (var i = 0; i < 5; i++) b.Append("stream", $"line-{i}");

		var page = b.Snapshot();
		Assert.That(page.Lines.Length, Is.EqualTo(3));
		Assert.That(page.Lines.Select(l => l.Line), Is.EqualTo(new[] { "line-2", "line-3", "line-4" }));
		Assert.That(page.DroppedCount, Is.EqualTo(2));
		// Sequences are stable: dropped lines keep their original sequence values.
		Assert.That(page.Lines.Select(l => l.Sequence), Is.EqualTo(new long[] { 2, 3, 4 }));
	}

	[Test]
	public void ByteCap_DropsOldestUntilUnderCap()
	{
		// Each entry is ~16 + channel*2 + line*2 bytes. Cap at 200 bytes to force truncation.
		var b = new InvocationBuffer(maxLines: 1000, maxBytes: 200);
		for (var i = 0; i < 10; i++) b.Append("s", new string('x', 30));
		// We expect the buffer to settle below 200 bytes.
		var page = b.Snapshot();
		Assert.That(page.DroppedCount, Is.GreaterThan(0));
		Assert.That(page.Lines.Length, Is.LessThan(10));
	}

	[Test]
	public void DroppedCount_StrictlyIncreases_AcrossSnapshots()
	{
		var b = new InvocationBuffer(maxLines: 2, maxBytes: int.MaxValue);
		b.Append("s", "a");
		b.Append("s", "b");
		var first = b.Snapshot();
		Assert.That(first.DroppedCount, Is.EqualTo(0));

		b.Append("s", "c");
		b.Append("s", "d");
		var second = b.Snapshot();
		Assert.That(second.DroppedCount, Is.GreaterThan(first.DroppedCount));
	}

	[Test]
	public void ConcurrentAppend_AndSnapshot_DoNotThrow()
	{
		var b = new InvocationBuffer();
		var writer = System.Threading.Tasks.Task.Run(() =>
		{
			for (var i = 0; i < 500; i++) b.Append("s", $"line-{i}");
		});
		var reader = System.Threading.Tasks.Task.Run(() =>
		{
			for (var i = 0; i < 100; i++) { _ = b.Snapshot(); }
		});
		System.Threading.Tasks.Task.WaitAll(writer, reader);
		Assert.That(b.Snapshot().Lines.Length, Is.EqualTo(500).Or.LessThan(500));
	}
}
