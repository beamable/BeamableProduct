using System.Linq;
using cli.Services.LocalStack;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Covers the in-memory line buffer that feeds readiness gates in attached mode (`beam local up` default).
/// Producer (the child's stdout/stderr pump) calls Append with complete lines; the readiness gate drains via
/// ReadAvailableLines. Must be FIFO, drain-once, and bounded so a never-drained chatty service can't grow it.
/// </summary>
public class StreamLineBufferTests
{
	[Test]
	public void Drains_appended_lines_in_order_once()
	{
		var buf = new StreamLineBuffer();
		buf.Append("Serving traffic at :9002");
		buf.Append("second line");

		Assert.That(buf.ReadAvailableLines().ToList(),
			Is.EqualTo(new[] { "Serving traffic at :9002", "second line" }));
		// Nothing new since last drain.
		Assert.That(buf.ReadAvailableLines().ToList(), Is.Empty);
	}

	[Test]
	public void Read_returns_only_lines_since_last_read()
	{
		var buf = new StreamLineBuffer();
		buf.Append("a");
		Assert.That(buf.ReadAvailableLines().ToList(), Is.EqualTo(new[] { "a" }));

		buf.Append("b");
		buf.Append("c");
		Assert.That(buf.ReadAvailableLines().ToList(), Is.EqualTo(new[] { "b", "c" }));
	}

	[Test]
	public void Is_bounded_dropping_oldest_past_the_cap()
	{
		var buf = new StreamLineBuffer(cap: 3);
		foreach (var i in Enumerable.Range(1, 10))
			buf.Append($"line{i}");

		var drained = buf.ReadAvailableLines().ToList();
		Assert.That(drained.Count, Is.LessThanOrEqualTo(3), "buffer must not grow past its cap");
		// The most recent lines survive; the oldest are dropped.
		Assert.That(drained, Does.Contain("line10"));
		Assert.That(drained, Does.Not.Contain("line1"));
	}

	[Test]
	public void Null_line_is_ignored()
	{
		var buf = new StreamLineBuffer();
		buf.Append(null);
		buf.Append("real");
		Assert.That(buf.ReadAvailableLines().ToList(), Is.EqualTo(new[] { "real" }));
	}
}
