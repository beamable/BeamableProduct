using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using cli;
using cli.Services;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Spectre.Console;
using Spectre.Console.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using ZLogger;
using MessageTemplate = ZLogger.MessageTemplate;

#pragma warning disable CS8618

namespace tests.Examples;

[NonParallelizable]
public class CLITest
{
	protected static string OriginalWorkingDir;

	static CLITest()
	{
		OriginalWorkingDir = Directory.GetCurrentDirectory();
	}
	
	protected string WorkingDir => Path.Combine(OriginalWorkingDir, "testRuns", TestId);
	protected string TestId { get; private set; }

	protected Mock<IRequester> _mockRequester;
	protected LoggingLevelSwitch _serilogLevel;
	private Action<IDependencyBuilder> _configurator;

	private List<Mock> _mockObjects = new();

	protected DockerClient _dockerClient = null!;


	protected TestConsole Ansi
	{
		get;
		private set;
	}

	[SetUp]
	public void Setup()
	{
		ProjectContextUtil.EnableManifestCache = false;
		_dockerClient = new DockerClientConfiguration(new AnonymousCredentials()).CreateClient();

		TestId = Guid.NewGuid().ToString();
		
		Directory.SetCurrentDirectory(OriginalWorkingDir);
		Directory.CreateDirectory(WorkingDir);
		Directory.SetCurrentDirectory(WorkingDir);

		AnsiConsole.Console = Ansi = new TestConsole()
			.Colors(ColorSystem.Standard)
			.Interactive()
			.EmitAnsiSequences();

		_serilogLevel = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Verbose };
		_mockRequester = new Mock<IRequester>();
	}


	protected void DisposeDockerClient()
	{
		// Dispose the Docker client
		_dockerClient.Dispose();
	}

	[TearDown]
	public void Teardown()
	{
		DisposeDockerClient();
		ResetConfigurator();
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

			var mock = new Mock<T>();
			configurator(mock);
			_mockObjects.Add(mock);
			builder.ReplaceSingleton<T, T>(() => mock.Object);
		};
	}

	protected void ResetConfigurator()
	{
		_configurator = new Action<IDependencyBuilder>(_ => { });
	}

	protected int Run(params string[] args) => RunFull(args, assertExitCode: true);
	protected int RunFull(string[] args, bool assertExitCode=false, Action<IDependencyBuilder>? configurator=null)
	{
		var exitCode = Cli.RunWithParams(builder =>
		{
			builder.Remove<IBeamableRequester>();
			builder.Remove<IRequester>();
			builder.Remove<CliRequester>();
			builder.AddSingleton<IRequester>(_mockRequester.Object);
			builder.AddSingleton<IBeamableRequester>(_mockRequester.Object);

			_configurator?.Invoke(builder);
			configurator?.Invoke(builder);
		},
		logger =>
		{
			logger.ClearProviders().AddZLoggerConsole(options =>
			{
				options.UsePlainTextFormatter(formatter =>
				{
					formatter.SetPrefixFormatter($"{0:utc-longdate} [{1:short}]", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
				});
			});
		},
		// logger.
		// 		.WriteTo.Console(new MessageTemplateTextFormatter(
		// 			"{Timestamp:HH:mm:ss.fff} [{Level:u4}] {Message:lj}{NewLine}{Exception}"))
		// 		.MinimumLevel.ControlledBy(_serilogLevel)
		// 		.CreateLogger();
		// },
		args);

		if (assertExitCode)
		{
			Assert.AreEqual(0, exitCode, $"Command had a non zero exit code. Check logs. code=[{exitCode}] command=[{string.Join(" ", args)}]");
		}
		return exitCode;
	}
}
