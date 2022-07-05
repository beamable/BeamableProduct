using Beamable.Common;
using Beamable.Server;
using Core.Server.Common;
using microserviceTests.microservice.Util;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Raw;
using Serilog.Sinks.TestCorrelator;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace microserviceTests.PromiseTests;

[TestFixture]
public class ErrorTests
{

	[SetUp]
	public void Setup()
	{
		BeamableLogProvider.Provider = new BeamableSerilogProvider();
		Debug.Instance = new MicroserviceDebug();
		// https://github.com/serilog/serilog/wiki/Configuration-Basics
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Warning()
			.WriteTo.TestCorrelator().CreateLogger();
		BeamableSerilogProvider.LogContext.Value = Log.Logger;
	}

	[Test]
	public async Task CaughtPromiseDoesNotLog()
	{

		MicroserviceBootstrapper.ConfigureUnhandledError();
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

		var logs = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
		Assert.IsEmpty(logs.Select(l => l.RenderMessage()));
		Assert.AreEqual(caughtException, exception);
	}


	[Test]
	public async Task UncaughtPromiseDoesLog()
	{

		MicroserviceBootstrapper.ConfigureUnhandledError();
		var exception = new Exception("test failure");
#pragma warning disable CS1998
		async Promise SubMethod()
#pragma warning restore CS1998
		{
			throw exception;
		}

		var _ = SubMethod();

		await Task.Delay(10); // wait for the uncaught promises...
		var logs = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
		Assert.IsNotEmpty(logs.Select(l => l.RenderMessage()));
	}
}
