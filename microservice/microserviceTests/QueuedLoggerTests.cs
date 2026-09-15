using Beamable.Server.Common;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace microserviceTests;

/// <summary>
/// A buffered record is only formatted when the queue is flushed, so a malformed template fails there rather
/// than at the call site. Losing that record must not cost the diagnostic context it was carrying - the whole
/// reason such messages get buffered is that they happen before a real logger exists.
/// </summary>
public class QueuedLoggerTests
{
	private const string Template = "startup failed with {curly} {braces}";

	[Test]
	public void Flush_MalformedRecord_KeepsSeverityEventIdAndException()
	{
		var startupFailure = new InvalidOperationException("the initializer blew up");
		var queue = new QueuedLogger();
		var target = new RecordingLogger();

		Enqueue(queue, LogLevel.Critical, new EventId(42, "startup"), startupFailure, ThrowingFormatter);

		queue.Flush(target);

		Assert.AreEqual(1, target.records.Count);
		var record = target.records[0];
		Assert.AreEqual(LogLevel.Critical, record.logLevel, "the record's own severity has to survive");
		Assert.AreEqual(42, record.eventId.Id, "the event id has to survive");
		Assert.AreSame(startupFailure, record.exception, "the original exception is the point of the record");
		Assert.IsTrue(record.text.Contains(Template), $"the raw template was lost: [{record.text}]");
	}

	[Test]
	public void Flush_MalformedRecordThenValidRecord_WritesBoth()
	{
		var queue = new QueuedLogger();
		var target = new RecordingLogger();

		Enqueue(queue, LogLevel.Error, default, new Exception("first"), ThrowingFormatter);
		Enqueue(queue, LogLevel.Information, default, null, (state, _) => "second record is fine");

		queue.Flush(target);

		Assert.AreEqual(2, target.records.Count, "a bad record must not swallow the rest of the queue");
		Assert.IsTrue(target.records[1].text.Contains("second record is fine"));
	}

	[Test]
	public void Flush_ThrowingStructuredArgument_PreservesSafeArgumentExceptionAndFollowingRecord()
	{
		var startupFailure = new InvalidOperationException("the initializer blew up");
		var queue = new QueuedLogger();
		var target = new RecordingLogger();
		var throwing = new ThrowingToString();
		var state = new StructuredState(
			new KeyValuePair<string, object>("bad", throwing),
			new KeyValuePair<string, object>("safe", "keep-me"),
			new KeyValuePair<string, object>("{OriginalFormat}", Template));

		queue.Log(LogLevel.Critical, new EventId(42, "startup"), state, startupFailure,
			(structuredState, _) => structuredState.ToString());
		Enqueue(queue, LogLevel.Information, default, null, (state, _) => "following record");

		Assert.DoesNotThrow(() => queue.Flush(target));

		Assert.AreEqual(2, target.records.Count, "the failed record must not swallow the next one");
		var recovered = target.records[0];
		Assert.AreEqual(LogLevel.Critical, recovered.logLevel);
		Assert.AreEqual(42, recovered.eventId.Id);
		Assert.AreSame(startupFailure, recovered.exception);
		Assert.IsTrue(recovered.text.Contains(Template), $"the raw template was lost: [{recovered.text}]");
		Assert.IsTrue(recovered.text.Contains("keep-me"), $"the safe argument was lost: [{recovered.text}]");
		Assert.IsTrue(recovered.text.Contains(nameof(ThrowingToString)),
			$"the throwing argument was not safely described: [{recovered.text}]");
		Assert.AreEqual(1, state.toStringCalls,
			"recovery must inspect structured entries rather than invoke the failed state formatter again");
		Assert.AreEqual("following record", target.records[1].text);
	}

	[Test]
	public void Flush_TargetRefusingEveryWrite_DoesNotStrandTheQueue()
	{
		// The recovery write can fail too. If that escapes, it escapes from inside the lock, the queue is never
		// cleared, and flushSignal has already been raised - so the logger is left permanently unusable.
		var queue = new QueuedLogger();

		Enqueue(queue, LogLevel.Error, default, null, ThrowingFormatter);
		Enqueue(queue, LogLevel.Error, default, null, (state, _) => "fine");

		Assert.DoesNotThrow(() => queue.Flush(new AlwaysThrowingLogger()));
		Assert.AreEqual(0, queue.messages.Count, "the queue has to end up drained");
	}

	private static string ThrowingFormatter(object state, Exception exception) =>
		throw new FormatException("the template could not be rendered");

	/// <summary>
	/// Enqueues a record shaped the way Microsoft.Extensions.Logging shapes one, so that the raw template is
	/// recoverable from the state under the "{OriginalFormat}" key.
	/// </summary>
	private static void Enqueue(QueuedLogger queue, LogLevel level, EventId eventId, Exception exception,
		Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string> formatter,
		params KeyValuePair<string, object>[] arguments)
	{
		var state = arguments.ToList();
		state.Add(new KeyValuePair<string, object>("{OriginalFormat}", Template));
		queue.Log(level, eventId, (IReadOnlyList<KeyValuePair<string, object>>)state, exception, formatter);
	}

	private class ThrowingToString
	{
		public override string ToString() => throw new InvalidOperationException("ToString is unavailable");
	}

	/// <summary>
	/// Reproduces the relevant FormattedLogValues behaviour: it is structured state, but its ToString renders
	/// the values and can fail. Recovery must inspect the list entries directly instead of calling this twice.
	/// </summary>
	private class StructuredState : IReadOnlyList<KeyValuePair<string, object>>
	{
		private readonly List<KeyValuePair<string, object>> _values;
		public int toStringCalls;

		public StructuredState(params KeyValuePair<string, object>[] values)
		{
			_values = values.ToList();
		}

		public int Count => _values.Count;
		public KeyValuePair<string, object> this[int index] => _values[index];

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _values.GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		public override string ToString()
		{
			toStringCalls++;
			return _values[0].Value + " " + _values[1].Value;
		}
	}

	private struct Record
	{
		public LogLevel logLevel;
		public EventId eventId;
		public Exception exception;
		public string text;
	}

	private class RecordingLogger : ILogger
	{
		public readonly List<Record> records = new List<Record>();

		public bool IsEnabled(LogLevel logLevel) => true;

		public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
			throw new NotSupportedException();

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
			Func<TState, Exception, string> formatter)
		{
			records.Add(new Record
			{
				logLevel = logLevel,
				eventId = eventId,
				exception = exception,
				text = formatter(state, exception)
			});
		}
	}

	private class AlwaysThrowingLogger : ILogger
	{
		public bool IsEnabled(LogLevel logLevel) => true;

		public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
			throw new NotSupportedException();

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
			Func<TState, Exception, string> formatter)
		{
			throw new InvalidOperationException("this target is not accepting writes");
		}
	}
}
