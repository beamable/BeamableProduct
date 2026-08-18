using Beamable.Common;
using Beamable.Server;
using microserviceTests.microservice.Util;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
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

	private static ILogger TestLogger => LoggingUtil.testLogger;

	private void AssertLoggedContains(string expected)
	{
		var found = GetLogs().Any(l => l.ToString().Contains(expected));
		Assert.IsTrue(found, $"No log line contained [{expected}]");
	}
}
