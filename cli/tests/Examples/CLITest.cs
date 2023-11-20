using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using cli;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Spectre.Console;
using Spectre.Console.Testing;
using System;
using System.Collections.Generic;
using System.IO;
#pragma warning disable CS8618

namespace tests.Examples;

[NonParallelizable]
public class CLITest
{
	protected string WorkingDir => Path.Combine(OriginalWorkingDir, "testRuns", TestId);
	protected string TestId { get; private set; }
	protected string OriginalWorkingDir;

	protected Mock<IRequester> _mockRequester;
	protected LoggingLevelSwitch _serilogLevel;
	private Action<IDependencyBuilder> _configurator;

	private List<Mock> _mockObjects = new();

	protected TestConsole Ansi
	{
		get;
		private set;
	}

	[SetUp]
	public void Setup()
	{
		TestId = Guid.NewGuid().ToString();

		OriginalWorkingDir = Directory.GetCurrentDirectory();

		Directory.CreateDirectory(WorkingDir);
		Directory.SetCurrentDirectory(WorkingDir);

		AnsiConsole.Console = Ansi = new TestConsole()
			.Colors(ColorSystem.Standard)
			.Interactive()
			.EmitAnsiSequences();

		_serilogLevel = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Information };
		_mockRequester = new Mock<IRequester>();
	}

	[TearDown]
	public void Teardown()
	{
		Directory.SetCurrentDirectory(OriginalWorkingDir);
		Directory.Delete(WorkingDir, true);

		foreach (var mock in _mockObjects)
		{
			mock.VerifyAll();
		}
	}

	protected void Configure(Action<IDependencyBuilder> configurator)
	{
		_configurator = configurator;
	}

	protected void Mock<T>(Action<Mock<T>> configurator)
		where T : class
	{
		var curriedConfig = _configurator;
		_configurator = builder =>
		{
			curriedConfig?.Invoke(builder);
			builder.ReplaceSingleton<T, T>(() =>
			{
				var mock = new Mock<T>();
				configurator(mock);
				_mockObjects.Add(mock);
				return mock.Object;
			});
		};
	}

	protected int Run(params string[] args)
	{
		var exitCode = Cli.RunWithParams(builder =>
		{
			builder.Remove<IBeamableRequester>();
			builder.Remove<IRequester>();
			builder.Remove<CliRequester>();
			builder.AddSingleton<IRequester>(_mockRequester.Object);
			builder.AddSingleton<IBeamableRequester>(_mockRequester.Object);

			_configurator?.Invoke(builder);
		},
		logger => logger
		.WriteTo.Console(new MessageTemplateTextFormatter(
			"{Timestamp:HH:mm:ss.fff} [{Level:u4}] {Message:lj}{NewLine}{Exception}"))
		.MinimumLevel.ControlledBy(_serilogLevel)
		.CreateLogger(),

		args);

		Assert.AreEqual(0, exitCode, $"Command had a non zero exit code. Check logs. code=[{exitCode}] command=[{string.Join(" ", args)}]");
		return exitCode;
	}
}
