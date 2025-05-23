using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using cli;
using cli.Services;
using Docker.DotNet;
using Moq;
using NUnit.Framework;
using Spectre.Console;
using Spectre.Console.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using ZLogger;

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
	protected BeamLogSwitch _logSwitch;
	private Action<IDependencyBuilder> _configurator;

	protected List<Mock> _mockObjects = new();

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

		_logSwitch = new BeamLogSwitch() { Level = LogLevel.Trace };
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
			logger.SetMinimumLevel(_logSwitch.Level);
			logger.AddZLoggerConsole();
		},

		args);

		if (assertExitCode)
		{
			Assert.AreEqual(0, exitCode, $"Command had a non zero exit code. Check logs. code=[{exitCode}] command=[{string.Join(" ", args)}]");
		}
		return exitCode;
	}
}
