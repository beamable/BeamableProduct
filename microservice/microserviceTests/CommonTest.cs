using Beamable.Common;
using Beamable.Common.Api.Content;
using Beamable.Server;
using microserviceTests.microservice.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace microserviceTests;

/// <summary>
/// Applies a timeout in milliseconds to a test.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public class TimeoutWithTeardown: PropertyAttribute, IWrapTestMethod
{
	private readonly int _timeout;

	public TimeoutWithTeardown(int timeout)
		: base(timeout)
	{
		_timeout = timeout;
	}

	public TestCommand Wrap(TestCommand command)
	{
		var timeoutCommand = new LoggingTimeoutCommand(command, _timeout);
		return timeoutCommand;
	}
}

public class LoggingTimeoutCommand : TimeoutCommand
{
	public LoggingTimeoutCommand(TestCommand innerCommand, int timeout) : base(innerCommand, timeout)
	{
	}
}

/// <summary>
/// Workaround for https://github.com/nunit/nunit/issues/3283.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Assembly)]
public sealed class PreventExecutionContextLeaksAttribute : Attribute, IWrapSetUpTearDown
{
	public TestCommand Wrap(TestCommand command) => new ExecuteInIsolatedExecutionContextCommand(command);

	private sealed class ExecuteInIsolatedExecutionContextCommand : DelegatingTestCommand
	{
		public ExecuteInIsolatedExecutionContextCommand(TestCommand innerCommand) : base(innerCommand)
		{
		}

		[DebuggerNonUserCode]
		public override TestResult Execute(TestExecutionContext context)
		{
			using (var copy = ExecutionContext.Capture().CreateCopy())
			{
				var returnValue = new StrongBox<TestResult>();
				ExecutionContext.Run(copy, Execute, state: (context, returnValue));
				return returnValue.Value;
			}
		}

		[DebuggerNonUserCode]
		private void Execute(object state)
		{
			var (context, returnValue) = ((TestExecutionContext, StrongBox<TestResult>))state;

			returnValue.Value = innerCommand.Execute(context);
		}
	}
}

public class CommonTest
{
	protected bool allowErrorLogs;

	private Task timeoutTask;
	public static Stopwatch globalTime = new Stopwatch();
	private Stopwatch localTime = new Stopwatch();

	static CommonTest()
	{
		globalTime.Start();
	}

	private string GetTime(Stopwatch sw)
	{
		return TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds).ToString();
	}

	[SetUp]
	public void SetupTest()
	{
		localTime.Restart();
		// set content static variables...
		ContentApi.Instance = new Promise<IContentApi>();
		
		// set up logging
		LoggingUtil.InitTestCorrelator();

		// reset exit code to 0
		Environment.ExitCode = 0;

		Console.WriteLine($"[{GetTime(globalTime)}] - Starting Test - [{TestContext.CurrentContext.Test.MethodName}]");
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

		Console.WriteLine($"[{GetTime(globalTime)}] - Finishing Test - {TestContext.CurrentContext.Result.Outcome.Status} [{TestContext.CurrentContext.Test.MethodName}] ({GetTime(localTime)})");
		localTime.Stop();
		if (testFailure)
		{
			Console.WriteLine($"Dumping logs for [{TestContext.CurrentContext.Test.MethodName}]");
			foreach (var log in GetLogs().ToList())
			{
				
				Console.WriteLine($"l=[{log.LogInfo.LogLevel}]" + log.ToString());
			}
			Console.WriteLine("End of log stream");
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

	protected IEnumerable<IZLoggerEntry> GetLogs()
	{
		return LoggingUtil.testLogs.allLogs;
	}

	protected IEnumerable<IZLoggerEntry> GetBadLogs()
	{
		return GetLogs().Where(l => 
			l.LogInfo.LogLevel >= LogLevel.Error);
	}

	protected void AssertBadLogCountContains(string contains, int expectedCount = 1)
	{
		var logCount = GetLogs().Count(l =>
			l.ToString().Contains(contains));
		Assert.AreEqual(expectedCount, logCount);
	}

	protected void AssertBadLogCountContains<T>(int expectedCount = 1) where T : Exception
	{
		AssertBadLogCountContains(
			$"Exception {typeof(T).Name}",
			expectedCount);
	}

	protected void AssetBadInputError() => AssertBadLogCountContains<BadInputException>();
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
