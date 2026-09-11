using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
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
using tests.MoqExtensions;
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
	protected Mock<IAuthApi> _mockAuth;
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
		_mockAuth = new Mock<IAuthApi>();
		
		_mockAuth.Setup(x => x.LoginRefreshToken(It.IsAny<string>()))
				.ReturnsPromise(new TokenResponse
				{
					refresh_token = "refresh",
					access_token = "access",
					token_type = "token",
					expires_in = (long)TimeSpan.FromMinutes(30).TotalMilliseconds
				});

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
		DeleteDirectoryRobust(WorkingDir);

		foreach (var mock in _mockObjects)
		{
			mock.VerifyAll();
		}
	}


	/// <summary>
	/// Recursively deletes a test's working directory, tolerating the two things a real `npm install` leaves
	/// behind that plain <see cref="Directory.Delete(string, bool)"/> cannot handle on Windows.
	///
	/// A `file:` dependency is materialised as a JUNCTION under `node_modules` (see
	/// PortalExtensionAddLibraryCommand), and a recursive delete that walks into a reparse point instead of
	/// unlinking it fails with "The parameter is incorrect" — an IOException raised in TearDown, which NUnit
	/// reports as a failure even though the test itself passed. Read-only files (npm and git both write some)
	/// are the other reliable way to break the built-in delete.
	///
	/// A short retry covers the transient Windows case where a just-exited child process or a virus scanner
	/// still holds a handle.
	/// </summary>
	private static void DeleteDirectoryRobust(string path)
	{
		for (var attempt = 0; ; attempt++)
		{
			try
			{
				DeleteDirectoryCore(path);
				return;
			}
			catch (Exception) when (attempt < 4)
			{
				// Give whoever still holds a handle a moment to let go.
				System.Threading.Thread.Sleep(100 * (attempt + 1));
			}
		}
	}

	private static void DeleteDirectoryCore(string path)
	{
		if (!Directory.Exists(path))
		{
			return;
		}

		var dir = new DirectoryInfo(path);

		foreach (var sub in dir.EnumerateDirectories())
		{
			// A junction/symlink: delete the LINK, never what it points at. Recursing through one would also
			// delete the real library the test scaffolded elsewhere in the tree.
			if (sub.Attributes.HasFlag(FileAttributes.ReparsePoint))
			{
				sub.Delete();
			}
			else
			{
				DeleteDirectoryCore(sub.FullName);
			}
		}

		foreach (var file in dir.EnumerateFiles())
		{
			if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
			{
				file.Attributes = FileAttributes.Normal;
			}

			file.Delete();
		}

		dir.Delete(false);
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
			builder.Remove<IAuthApi>();
			
			builder.AddSingleton<IAuthApi>(_mockAuth.Object);
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
