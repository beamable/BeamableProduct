using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Server;
using microserviceTests.microservice.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using Swan;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace microserviceTests;

public class CommonTest
{
	protected ITestCorrelatorContext logContext;
	protected bool allowErrorLogs;

	[SetUp]
	public void SetupTest()
	{
		// set content static variables...
		ContentApi.Instance = new Promise<IContentApi>();
		BeamableMicroService._contentService = null;

		// hacky way to clear the test log context
		var field = typeof(TestCorrelator).GetField("ContextGuidDecoratedLogEvents", BindingFlags.Static | BindingFlags.NonPublic);
		var fieldVal = field.GetValue(null);
		var clearMethod = fieldVal.GetType().GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
		clearMethod.Invoke(fieldVal, new object[] { });

		// set up logging
		LoggingUtil.InitTestCorrelator();

		// reset exit code to 0
		Environment.ExitCode = 0;
	}

	[TearDown]
	public void TeardownTest()
	{
		// there should be no error logs, unless the test has been configured to allow them.
		var logFailure = !allowErrorLogs && GetBadLogs().Any();
		var exitCodeFailure = Environment.ExitCode != 0;

		// did the test fail due to the test itself?
		var testFailure = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed ||
		                  TestContext.CurrentContext.Result.Outcome == ResultState.Error;

		testFailure |= logFailure; // or perhaps because there are un-accounted for logs
		testFailure |= exitCodeFailure; // or the exit code wasn't right.

		if (testFailure)
		{
			Console.WriteLine("Dumping logs...");
			foreach (var log in GetLogs().ToList())
			{
				Console.WriteLine(log.RenderMessage());
			}
		}

		allowErrorLogs = false;

		if (logFailure)
		{
			Assert.Fail("There were more than 0 error/fatal logs");
		}

		if (exitCodeFailure)
		{
			Assert.Fail("The exit code was not 0");
		}
	}

	protected IEnumerable<LogEvent> GetLogs()
	{
		return TestCorrelator.GetLogEventsFromCurrentContext();
		// return TestCorrelator.GetLogEventsFromContextGuid(logContext.Guid);
	}

	protected IEnumerable<LogEvent> GetBadLogs()
	{
		return GetLogs().Where(l => l.Level == LogEventLevel.Error || l.Level == LogEventLevel.Fatal);
	}

	protected void AssertBadLogCountContains(string contains, int expectedCount = 1)
	{
		var logCount = GetLogs().Count(l =>
			l.RenderMessage().Contains(contains));
		Assert.AreEqual(expectedCount, logCount);
	}

	protected void AssertBadLogCountContains<T>(int expectedCount = 1) where T : Exception
	{
		AssertBadLogCountContains(
			$"Exception \"{typeof(T).Name}\"",
			expectedCount);
	}

	protected void AssertMissingScopeError(int expectedCount = 1) =>
		AssertBadLogCountContains<MissingScopesException>();

	protected void AssertMissingParameterError(int expectedCount = 1) =>
		AssertBadLogCountContains<ParameterMissingRequiredException>();

	protected void AssertUnauthorizedException(int expectedCount = 1) =>
		AssertBadLogCountContains<UnauthorizedUserException>();
}

public class TestWithNoLogs : TestAttribute
{

}
