using Beamable.Common;
using Beamable.Server;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace microserviceTests.PromiseTests;

[TestFixture]
public class ErrorTests : CommonTest
{

	[Test]
	[NonParallelizable]
	public async Task CaughtPromiseDoesNotLog()
	{

		MicroserviceStartupUtil.ConfigureUnhandledError();
		var exception = new Exception("test failure");
#pragma warning disable CS1998
		async Promise SubMethod()
#pragma warning restore CS1998
		{
			throw exception;
		}

		Promise Method()
		{
			return SubMethod();
		}

		var caughtException = default(Exception);
		try
		{
			await Method();
		}
		catch (Exception ex)
		{
			caughtException = ex;
		}

		Assert.AreEqual(caughtException, exception);
	}


	[Test]
	[NonParallelizable]
	public async Task UncaughtPromiseDoesLog()
	{
		allowErrorLogs = true;
		MicroserviceStartupUtil.ConfigureUnhandledError();
		var exception = new Exception("test failure");
#pragma warning disable CS1998
		async Promise SubMethod()
#pragma warning restore CS1998
		{
			throw exception;
		}

		var _ = SubMethod();

		while (!GetBadLogs().Any())
		{
			await Task.Delay(10); // wait for the uncaught promises...
		}
		var logs = GetBadLogs().ToList();
		Assert.IsNotEmpty(logs.Select(l => l.ToString()));
	}
}
