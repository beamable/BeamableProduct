using Beamable.Common;
using Beamable.Server;
using microserviceTests.microservice.Util;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace microserviceTests;

/// <summary>
/// Covers the guard described in https://github.com/beamable/BeamableProduct/issues/4566: a message template
/// carrying text that was not written as a template - an exception message with curly braces in it, say - used
/// to throw out of the logger and take the caller's report with it.
/// </summary>
public class SafeLogTemplateTests : CommonTest
{
	private const string BracedMessage = "Simulated init failure with {curly} {braces} in message";
	private const string Stacktrace = "   at Beamable.Tests.Boom()";

	[Test]
	public void Write_TemplateWithMoreHolesThanArgs_DoesNotThrow()
	{
		allowErrorLogs = true;

		Assert.DoesNotThrow(() => SafeLogTemplate.Write(TestLogger, LogLevel.Error,
			$"Custom service initializer failed.\n{BracedMessage}\n{{stacktrace}}",
			new object[] { Stacktrace }));

		// Degraded or not, both halves of what the caller wanted to report have to survive.
		AssertLoggedContains(BracedMessage);
		AssertLoggedContains(Stacktrace);
	}

	[Test]
	public void Write_StrayClosingBrace_DoesNotThrow()
	{
		allowErrorLogs = true;

		// A template with no opening brace is handed to string.Format untouched, so a lone '}' fails to format
		// even though the template declares no holes at all.
		Assert.DoesNotThrow(() => SafeLogTemplate.Write(TestLogger, LogLevel.Error,
			"unbalanced } brace", new object[] { Stacktrace }));

		AssertLoggedContains(Stacktrace);
	}

	[Test]
	public void Write_WellFormedTemplate_RendersArgumentsIntoMessage()
	{
		allowErrorLogs = true;

		SafeLogTemplate.Write(TestLogger, LogLevel.Error,
			"Custom service initializer [{typeName}.{methodName}] failed.\n{message}\n{stacktrace}",
			new object[] { "SomeType", "SomeMethod", BracedMessage, Stacktrace });

		AssertLoggedContains("Custom service initializer [SomeType.SomeMethod] failed.");
		AssertLoggedContains(BracedMessage);
		AssertLoggedContains(Stacktrace);
		// A well formed template is passed through untouched, so nothing is flattened.
		Assert.IsFalse(GetLogs().Any(l => l.ToString().Contains("unformatted log arguments")));
	}

	[Test]
	public void Write_NoArgs_KeepsBracesLiteral()
	{
		allowErrorLogs = true;

		Assert.DoesNotThrow(() => SafeLogTemplate.Write(TestLogger, LogLevel.Error, BracedMessage, null));
		Assert.DoesNotThrow(() => SafeLogTemplate.Write(TestLogger, LogLevel.Error, BracedMessage, new object[0]));

		AssertLoggedContains("{curly}");
	}

	[Test]
	public void BeamableLogger_LogErrorWithBracedMessage_DoesNotThrow()
	{
		allowErrorLogs = true;

		// The shape the issue reported, through the provider the microservice and the CLI both install.
		Assert.DoesNotThrow(() => BeamableLogger.LogError(
			$"Custom service initializer failed.\n{BracedMessage}\n{{stacktrace}}", Stacktrace));

		AssertLoggedContains(BracedMessage);
		AssertLoggedContains(Stacktrace);
	}

	[TestCase("no holes here", "no holes here")]
	[TestCase("a {name} b", "a {0} b")]
	[TestCase("{first} and {second}", "{0} and {1}")]
	[TestCase("aligned {name,5}", "aligned {0,5}")]
	[TestCase("formatted {name:X}", "formatted {0:X}")]
	[TestCase("escaped {{name}}", "escaped {{name}}")]
	[TestCase("unclosed {name", "unclosed {name")]
	[TestCase("", "")]
	[TestCase(null, null)]
	public void ToPositionalFormat_MatchesTheFrameworksRewrite(string template, string expected)
	{
		Assert.AreEqual(expected, SafeLogTemplate.ToPositionalFormat(template));
	}

	[TestCase("plain message", 1, true)]
	[TestCase("one {hole}", 1, true)]
	[TestCase("one {hole}", 2, true, Description = "Surplus arguments are ignored, not an error")]
	[TestCase("two {a} {b}", 1, false)]
	[TestCase("stray } brace", 1, false)]
	[TestCase("escaped {{a}} braces", 1, true)]
	[TestCase("bad alignment {a,zz}", 1, false)]
	[TestCase(null, 1, true)]
	public void CanRender_AgreesWithWhatTheFrameworkCanFormat(string template, int argCount, bool expected)
	{
		var args = Enumerable.Range(0, argCount).Select(i => (object)$"v{i}").ToArray();
		Assert.AreEqual(expected, SafeLogTemplate.CanRender(template, args));
	}

	[Test]
	public void Write_ThrowingArgumentInDegradedPath_DoesNotThrow()
	{
		// The template declares more holes than there are arguments, so Write takes its flat fallback - which
		// used to render the arguments a second time with nothing to catch a ToString() that throws.
		var logger = new SilentLogger();

		Assert.DoesNotThrow(() => SafeLogTemplate.Write(logger, LogLevel.Error, "two {a} {b}",
			new object[] { new ThrowingToString() }));
		Assert.AreEqual(1, logger.writes, "the degraded message still has to be written");
	}

	[Test]
	public void Write_ThrowingArgument_StillReportsTheOtherArguments()
	{
		var logger = new RenderingLogger();

		Assert.DoesNotThrow(() => SafeLogTemplate.Write(logger, LogLevel.Error, "three {a} {b} {c}",
			new object[] { "keep-me", new ThrowingToString() }));

		var message = string.Join("\n", logger.rendered);
		Assert.IsTrue(message.Contains("keep-me"), $"the readable argument was lost: [{message}]");
		Assert.IsTrue(message.Contains(nameof(ThrowingToString)), $"the bad argument went undescribed: [{message}]");
	}

	[Test]
	public void Write_ThrowingArgumentRenderedByTheProvider_DoesNotThrow()
	{
		// A well formed template passes the guard, so the throw comes from the provider instead. Write's own
		// catch then has to fall back without tripping over the same argument again.
		var logger = new RenderingLogger();

		Assert.DoesNotThrow(() => SafeLogTemplate.Write(logger, LogLevel.Error, "one {a}",
			new object[] { new ThrowingToString() }));
	}

	[Test]
	public void CanRender_DoesNotRenderTheArguments()
	{
		var counted = new CountingToString();

		Assert.IsTrue(SafeLogTemplate.CanRender("one {a}", new object[] { counted }));
		Assert.AreEqual(0, counted.calls, "validation must not run application ToString() code");
	}

	[Test]
	public void Write_WellFormedTemplate_LeavesRenderingToTheProvider()
	{
		// The other half of the review: validation used to render every argument on every successful call, on
		// top of whatever the providers then did. Against a provider that never renders, nothing should.
		var logger = new SilentLogger();
		var counted = new CountingToString();

		SafeLogTemplate.Write(logger, LogLevel.Error, "one {a}", new object[] { counted });

		Assert.AreEqual(0, counted.calls, "the guard rendered an argument the provider never asked for");
	}

	private static ILogger TestLogger => LoggingUtil.testLogger;

	private void AssertLoggedContains(string expected)
	{
		var found = GetLogs().Any(l => l.ToString().Contains(expected));
		Assert.IsTrue(found, $"No log line contained [{expected}]");
	}

	/// <summary>
	/// Stores a record but never renders it - the shape the guard has to survive, since a queued provider defers
	/// formatting to its flush and an exporter to its export, both long after Write has returned.
	/// </summary>
	private class SilentLogger : ILogger
	{
		public int writes;

		public bool IsEnabled(LogLevel logLevel) => true;

		public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
			throw new NotSupportedException();

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
			Func<TState, Exception, string> formatter)
		{
			writes++;
		}
	}

	/// <summary>
	/// Renders every record eagerly, the way a console sink does.
	/// </summary>
	private class RenderingLogger : ILogger
	{
		public readonly List<string> rendered = new List<string>();

		public bool IsEnabled(LogLevel logLevel) => true;

		public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
			throw new NotSupportedException();

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
			Func<TState, Exception, string> formatter)
		{
			rendered.Add(formatter(state, exception));
		}
	}

	private class ThrowingToString
	{
		public override string ToString() => throw new InvalidOperationException("ToString is unavailable");
	}

	private class CountingToString
	{
		public int calls;

		public override string ToString()
		{
			calls++;
			return "counted";
		}
	}
}
