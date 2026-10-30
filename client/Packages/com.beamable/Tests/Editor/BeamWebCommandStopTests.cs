using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli;
using Beamable.Editor.Utility;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
	public class BeamWebCommandStopTests
	{
		private readonly List<Fixture> _fixtures = new List<Fixture>();

		[UnityTearDown]
		public IEnumerator TearDown() => CleanupFixtures(_fixtures);

		internal static IEnumerator CleanupFixtures(List<Fixture> ownedFixtures)
		{
			// A failed nested UnityTest iterator need not dispose its parent's using scopes.
			// Own fixtures here so cleanup still runs after an assertion or transport timeout.
			var fixtures = ownedFixtures.ToArray();
			var drained = false;
			try
			{
				foreach (var fixture in fixtures) fixture.BeginCleanup();
				var deadline = DateTime.UtcNow.AddSeconds(15);
				while (!(drained = Array.TrueForAll(fixtures, fixture => fixture.IsDrained)) && DateTime.UtcNow < deadline)
				{
					// Cancellation alone does not resolve a pending fake discovery callback.
					foreach (var fixture in fixtures) fixture.Discover();
					EditorApplication.QueuePlayerLoopUpdate();
					yield return null;
				}
			}
			finally
			{
				foreach (var fixture in fixtures) fixture.Dispose();
				ownedFixtures.Clear();
			}
			// Keep the assertion outside the yielding loop so it cannot bypass final disposal.
			Assert.That(drained, Is.True, "Fixture cleanup did not drain all server, run, and stop operations.");
		}

		[UnityTest]
		public IEnumerator FixtureCleanupDoesNotLeaveStoppedEditorCoroutineCallbacks()
		{
			var errors = new List<string>();
			void Capture(string message, string stack, LogType type)
			{
				if (type == LogType.Exception && stack.Contains("EditorCoroutine")) errors.Add(message);
			}
			Application.logMessageReceived += Capture;
			try
			{
				new Fixture(_fixtures);
				yield return TearDown();
				// Let callbacks captured by the current update and deferred cleanup both finish.
				yield return null;
				yield return null;
				Assert.That(errors, Is.Empty);
			}
			finally { Application.logMessageReceived -= Capture; }
		}

		[UnityTest]
		public IEnumerator TearDownDrainsHeldAcknowledgementAndPausedCallbacks()
		{
			var fixture = new Fixture(_fixtures);
			var callbackRan = false;
			fixture.Command.On(_ => callbackRan = true);
			var run = fixture.Command.Run();
			run.Error(_ => { });
			fixture.Discover();
			fixture.AllowHeaders.TrySetResult(true);
			var stop = fixture.Command.StopAsync();
			stop.Error(_ => { });
			yield return WaitFor(() => fixture.StopReceived.Task.IsCompleted);
			fixture.PauseCallbacks();
			fixture.ReleaseWorker.TrySetResult(true);
			yield return WaitFor(() => fixture.CallbacksQueued);
			Assert.That(run.IsCompleted, Is.False);
			Assert.That(stop.IsCompleted, Is.False);

			yield return TearDown();

			Assert.That(fixture.IsDrained, Is.True);
			Assert.That(callbackRan, Is.True, "Cleanup must drain callbacks before disposing their dispatcher.");
		}

		[UnityTest]
		public IEnumerator TearDownDrainsPendingDiscovery()
		{
			var fixture = new Fixture(_fixtures);
			var run = fixture.Command.Run();
			run.Error(_ => { });
			Assert.That(run.IsCompleted, Is.False);

			yield return TearDown();

			Assert.That(fixture.IsDrained, Is.True);
			Assert.That(run.IsCompleted, Is.True);
		}

		[UnityTest]
		public IEnumerator ServerExitDuringRediscoveryStillBoundsShutdown()
		{
#if UNITY_EDITOR_WIN
			var disconnectedServer = new TcpListener(IPAddress.Loopback, 0);
			disconnectedServer.Start();
			using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = "powershell.exe", Arguments = "-NoProfile -NonInteractive -Command Start-Sleep -Seconds 60",
				UseShellExecute = false, CreateNoWindow = true
			});
			try
			{
				var fixture = new Fixture(_fixtures) { WriteOutput = false };
				fixture.Factory.port = ((IPEndPoint)disconnectedServer.LocalEndpoint).Port;
				var connection = disconnectedServer.AcceptSocketAsync();
				Set(fixture.Factory, "<ServerProcess>k__BackingField", process);
				var run = fixture.Command.Run();
				run.Error(_ => { });
				fixture.Discover();
				yield return WaitFor(() => connection.IsCompleted);
				using var socket = connection.Result;
				process.Kill();
				Assert.That(process.WaitForExit(1000), Is.True);
				// Reset TCP before any HTTP headers; HttpListener shutdown may send a normal response.
				socket.LingerState = new LingerOption(true, 0);
				socket.Close();
				disconnectedServer.Stop();
				yield return WaitFor(() => fixture.Factory.discoveryRequest >= 2, "Failed request must enter rediscovery.");
				var stop = fixture.Command.StopAsync();
				stop.Error(_ => { });
				yield return WaitFor(() => stop.IsCompleted, "Shutdown must remain bounded during rediscovery.");
				Assert.That(stop.IsFailed, Is.True, "Pending discovery must time out, not bypass the registration deadline.");
				fixture.Command.Cancel();
				for (var i = 0; i < 4 && !run.IsCompleted; i++) { fixture.Discover(); yield return null; }
				yield return WaitFor(() => run.IsCompleted);
				var retry = fixture.Command.StopAsync();
				retry.Error(_ => { });
				yield return WaitFor(() => retry.IsCompleted);
				Assert.That(retry.IsFailed, Is.False);
			}
			finally { disconnectedServer.Stop(); if (!process.HasExited) process.Kill(); }
#else
			Assert.Ignore("Windows process-ownership regression.");
			yield break;
#endif
		}

		[UnityTest]
		public IEnumerator DiscoveryCanRetryAfterServerProcessDisappears()
		{
			var fixture = new Fixture(_fixtures) { InfoPids = new[] { int.MaxValue, System.Diagnostics.Process.GetCurrentProcess().Id } };
			var first = fixture.Factory.PingServer();
			yield return WaitFor(() => first.IsCompleted);
			Assert.That(first.GetResult(), Is.EqualTo(BeamWebCommandFactory.PingResult.NoServer));
			var retry = fixture.Factory.PingServer();
			yield return WaitFor(() => retry.IsCompleted);
			Assert.That(retry.GetResult(), Is.EqualTo(BeamWebCommandFactory.PingResult.Match));
		}

		[UnityTest]
		public IEnumerator ConfirmedServerExitAllowsShutdownWithoutAnHttpAcknowledgement()
		{
#if UNITY_EDITOR_WIN
			using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = "powershell.exe", Arguments = "-NoProfile -NonInteractive -Command Start-Sleep -Seconds 60",
				UseShellExecute = false, CreateNoWindow = true
			});
			try
			{
				var fixture = new Fixture(_fixtures) { WriteOutput = false };
				Set(fixture.Factory, "<ServerProcess>k__BackingField", process);
				var run = fixture.Command.Run();
				run.Error(_ => { });
				fixture.Discover();
				fixture.AllowHeaders.TrySetResult(true);
				var registration = (TaskCompletionSource<string>)Get(fixture.Command, "_stopUrl");
				yield return WaitFor(() => registration.Task.IsCompleted);
				process.Kill();
				Assert.That(process.WaitForExit(1000), Is.True);
				var stop = fixture.Command.StopAsync();
				stop.Error(_ => { });
				yield return WaitFor(() => stop.IsCompleted);
				Assert.That(stop.IsFailed, Is.False);
				Assert.That(run.IsCompleted, Is.True, "The old run and dispatcher must still drain.");
				Assert.That(fixture.StopReceived.Task.IsCompleted, Is.False, "An exited process cannot acknowledge HTTP.");
				Assert.That(fixture.Command.StopAsync(), Is.SameAs(stop));
			}
			finally { if (!process.HasExited) process.Kill(); }
#else
			Assert.Ignore("Windows process-ownership regression.");
			yield break;
#endif
		}

		[UnityTest]
		public IEnumerator StalledRegistrationTimesOutAndCanRetry()
		{
			var fixture = new Fixture(_fixtures);
			fixture.Command.Run().Error(_ => { });
			fixture.Discover();
			yield return WaitFor(() => fixture.ExecuteReceived.Task.IsCompleted, diagnostics: fixture.Describe);
			var first = fixture.Command.StopAsync();
			Exception failure = null;
			first.Error(error => failure = error);
			yield return WaitFor(() => first.IsCompleted, diagnostics: fixture.Describe);
			Assert.That(failure, Is.TypeOf<TimeoutException>(), $"Only the held registration should time out. Received: {failure}");
			Assert.That(fixture.StopReceived.Task.IsCompleted, Is.False);
			fixture.AllowHeaders.TrySetResult(true);
			fixture.ReleaseWorker.TrySetResult(true);
			fixture.AllowAcknowledgement.TrySetResult(true);
			var retry = fixture.Command.StopAsync();
			retry.Error(_ => { });
			yield return WaitFor(() => retry.IsCompleted);
			Assert.That(retry.IsFailed, Is.False);
		}

		[UnityTest]
		public IEnumerator StalledAcknowledgementTimesOutAndCanRetry()
		{
			var fixture = new Fixture(_fixtures) { RetryStop = true };
			fixture.Command.Run().Error(_ => { });
			fixture.Discover();
			fixture.AllowHeaders.TrySetResult(true);
			fixture.ReleaseWorker.TrySetResult(true);
			// This case must reach registration before timing the acknowledgement; the test above
			// owns registration timeouts. An unrelated transport error must not count as a timeout.
			var registration = (TaskCompletionSource<string>)Get(fixture.Command, "_stopUrl");
			yield return WaitFor(() => registration.Task.IsCompleted);
			var first = fixture.Command.StopAsync();
			Exception failure = null;
			first.Error(error => failure = error);
			yield return WaitFor(() => first.IsCompleted, diagnostics: fixture.Describe);
			Assert.That(failure, Is.InstanceOf<OperationCanceledException>(), $"Only the held acknowledgement should time out. Received: {failure}");
			Assert.That(fixture.StopReceived.Task.IsCompleted, Is.True, "The server must have received the timed-out stop.");
			fixture.AllowAcknowledgement.TrySetResult(true);
			var retry = fixture.Command.StopAsync();
			retry.Error(_ => { });
			yield return WaitFor(() => fixture.RetryReceived.Task.IsCompleted);
			Assert.That(retry.IsCompleted, Is.False);
			fixture.AllowRetryAcknowledgement.TrySetResult(true);
			yield return WaitFor(() => retry.IsCompleted);
			Assert.That(retry.IsFailed, Is.False);
		}

		[UnityTest]
		public IEnumerator AssemblyReloadFailureStopsOwnedProcessAndAllowsRepeatedCleanup()
		{
#if UNITY_EDITOR_WIN
			using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
			{
				FileName = "powershell.exe", Arguments = "-NoProfile -NonInteractive -Command Start-Sleep -Seconds 60",
				UseShellExecute = false, CreateNoWindow = true
			});
			try
			{
				var fixture = new Fixture(_fixtures) { WriteOutput = false, StopStatus = 500 };
				Set(fixture.Factory, "<ServerProcess>k__BackingField", process);
				var run = fixture.Command.Run();
				run.Error(_ => { });
				fixture.Discover();
				fixture.AllowHeaders.TrySetResult(true);
				fixture.ReleaseWorker.TrySetResult(true);
				fixture.AllowAcknowledgement.TrySetResult(true);
				var stop = typeof(BeamWebCommand).GetMethod("StopForAssemblyReload", BindingFlags.Instance | BindingFlags.NonPublic);
				stop.Invoke(fixture.Command, null);
				Assert.That(process.HasExited, Is.True, "A failed acknowledgement must stop the owned process before reload.");
				// A second cleanup sees the exited process; it must remain safe and bounded.
				stop.Invoke(fixture.Command, null);
				yield return WaitFor(() => run.IsCompleted);
			}
			finally { if (!process.HasExited) process.Kill(); }
#else
			Assert.Ignore("Windows process-ownership regression.");
			yield break;
#endif
		}

		[UnityTest]
		public IEnumerator AssemblyReloadStopAcknowledgesWithoutUnityUpdates()
		{
			var fixture = new Fixture(_fixtures) { WriteOutput = false };
			var run = fixture.Command.Run();
			run.Error(_ => { });
			fixture.Discover();
			fixture.ReleaseWorker.TrySetResult(true);
			fixture.AllowAcknowledgement.TrySetResult(true);
			_ = Task.Run(async () => { await Task.Delay(100); fixture.AllowHeaders.TrySetResult(true); });
			var stop = typeof(BeamWebCommand).GetMethod("StopForAssemblyReload",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(stop, Is.Not.Null);
			// Deliberately do not yield: this is the production reload callback's scheduling constraint.
			stop.Invoke(fixture.Command, null);
			Assert.That(fixture.StopReceived.Task.IsCompleted, Is.True);
			Assert.That(fixture.StopId, Is.EqualTo(fixture.Command.id));
			Assert.That(fixture.ServerTask.Wait(1000), Is.True);
			yield return WaitFor(() => run.IsCompleted);
		}

		[UnityTest]
		public IEnumerator StopBeforeRegistrationWaitsForServerAcknowledgementAndCallbacks()
		{
			var fixture = new Fixture(_fixtures);
			var callbackRan = false;
			fixture.Command.On(_ => callbackRan = true);
			var run = fixture.Command.Run();
			run.Error(_ => { });
			var stop = fixture.Command.StopAsync();
			stop.Error(_ => { });
			Assert.That(fixture.Command.StopAsync(), Is.SameAs(stop));
			Assert.That(stop.IsCompleted, Is.False);
			fixture.Discover();
			yield return WaitFor(() => fixture.ExecuteReceived.Task.IsCompleted);
			Assert.That(fixture.StopReceived.Task.IsCompleted, Is.False, "Stop must wait for registration.");
			fixture.AllowHeaders.TrySetResult(true);
			yield return WaitFor(() => fixture.StopReceived.Task.IsCompleted);
			Assert.That(fixture.StopId, Is.EqualTo(fixture.Command.id));
			Assert.That(stop.IsCompleted, Is.False, "The worker is still held.");
			fixture.ReleaseWorker.TrySetResult(true);
			yield return WaitFor(() => run.IsCompleted);
			Assert.That(callbackRan, Is.True, "The real dispatcher must deliver the final output.");
			Assert.That(stop.IsCompleted, Is.False, "EOF alone is not a stop acknowledgement.");
			fixture.AllowAcknowledgement.TrySetResult(true);
			yield return WaitFor(() => stop.IsCompleted);
			Assert.That(stop.IsFailed, Is.False);
			yield return WaitFor(() => fixture.ServerTask.IsCompleted);
			Assert.That(fixture.ServerTask.IsFaulted, Is.False, fixture.ServerTask.Exception?.ToString());
		}

		[UnityTest]
		public IEnumerator StopAcknowledgementWaitsForQueuedCallbacks()
		{
			var fixture = new Fixture(_fixtures);
			var callbackRan = false;
			fixture.Command.On(_ => callbackRan = true);
			fixture.Command.Run().Error(_ => { });
			fixture.Discover();
			fixture.AllowHeaders.TrySetResult(true);
			var stop = fixture.Command.StopAsync();
			stop.Error(_ => { });
			yield return WaitFor(() => fixture.StopReceived.Task.IsCompleted);
			fixture.PauseCallbacks();
			fixture.ReleaseWorker.TrySetResult(true);
			fixture.AllowAcknowledgement.TrySetResult(true);
			yield return WaitFor(() => fixture.ServerTask.IsCompleted && fixture.CallbacksQueued);
			Assert.That(fixture.ServerTask.IsFaulted, Is.False, fixture.ServerTask.Exception?.ToString());
			Assert.That(callbackRan, Is.False);
			Assert.That(stop.IsCompleted, Is.False, "Server acknowledgement must also drain queued Unity callbacks.");
			fixture.ResumeCallbacks();
			yield return WaitFor(() => stop.IsCompleted);
			Assert.That(stop.IsFailed, Is.False);
			Assert.That(callbackRan, Is.True);
		}

		[UnityTest]
		public IEnumerator StopAfterLegacyCancelStillWaitsForServer()
		{
			var fixture = new Fixture(_fixtures) { WriteOutput = false };
			var run = fixture.Command.Run();
			run.Error(_ => { });
			fixture.Discover();
			fixture.AllowHeaders.TrySetResult(true);
			var registration = (TaskCompletionSource<string>)Get(fixture.Command, "_stopUrl");
			yield return WaitFor(() => registration.Task.IsCompleted);
			fixture.Command.Cancel();
			var stop = fixture.Command.StopAsync();
			stop.Error(_ => { });
			yield return WaitFor(() => fixture.StopReceived.Task.IsCompleted);
			Assert.That(stop.IsCompleted, Is.False);
			fixture.ReleaseWorker.TrySetResult(true);
			fixture.AllowAcknowledgement.TrySetResult(true);
			yield return WaitFor(() => stop.IsCompleted);
			Assert.That(stop.IsFailed, Is.False);
		}

		[UnityTest]
		public IEnumerator FailedStopAcknowledgementDoesNotReportSuccess()
		{
			var fixture = new Fixture(_fixtures) { StopStatus = 500 };
			fixture.Command.Run().Error(_ => { });
			fixture.Discover();
			fixture.AllowHeaders.TrySetResult(true);
			var stop = fixture.Command.StopAsync();
			Exception failure = null;
			stop.Error(error => failure = error);
			yield return WaitFor(() => fixture.StopReceived.Task.IsCompleted);
			fixture.ReleaseWorker.TrySetResult(true);
			fixture.AllowAcknowledgement.TrySetResult(true);
			yield return WaitFor(() => stop.IsCompleted);
			Assert.That(stop.IsFailed, Is.True);
			Assert.That(failure, Is.Not.Null);
		}

		[Test]
		public void StopBeforeRunPreventsLaterExecution()
		{
			var fixture = new Fixture(_fixtures);
			var stop = fixture.Command.StopAsync();
			Assert.That(stop.IsCompleted, Is.True);
			Assert.That(fixture.Command.Run(), Is.SameAs(stop));
			Assert.That(fixture.Factory.discoveryRequest, Is.Zero);
		}

		[UnityTest]
		public IEnumerator FailedAcknowledgementCanRetryWithoutDuplicatingPendingOrSuccessfulStop()
		{
			var fixture = new Fixture(_fixtures) { StopStatus = 500, RetryStop = true };
			fixture.Command.Run().Error(_ => { });
			fixture.Discover();
			fixture.AllowHeaders.TrySetResult(true);
			fixture.ReleaseWorker.TrySetResult(true);
			fixture.AllowAcknowledgement.TrySetResult(true);
			var first = fixture.Command.StopAsync();
			first.Error(_ => { });
			yield return WaitFor(() => first.IsCompleted);
			Assert.That(first.IsFailed, Is.True);
			var retry = fixture.Command.StopAsync();
			retry.Error(_ => { });
			Assert.That(retry, Is.Not.SameAs(first));
			Assert.That(fixture.Command.StopAsync(), Is.SameAs(retry));
			yield return WaitFor(() => fixture.RetryReceived.Task.IsCompleted);
			Assert.That(fixture.StopId, Is.EqualTo(fixture.Command.id));
			Assert.That(retry.IsCompleted, Is.False);
			fixture.AllowRetryAcknowledgement.TrySetResult(true);
			yield return WaitFor(() => retry.IsCompleted);
			Assert.That(retry.IsFailed, Is.False);
			Assert.That(fixture.Command.StopAsync(), Is.SameAs(retry));
		}

		private static IEnumerator WaitFor(Func<bool> ready, string message = "Timed out waiting for controlled transport gate.",
			Func<string> diagnostics = null, [CallerLineNumber] int line = 0)
		{
			var deadline = DateTime.UtcNow.AddSeconds(15);
			while (!ready() && DateTime.UtcNow < deadline)
			{
				// EditMode polls Editor updates; async continuations need the separate player loop.
				EditorApplication.QueuePlayerLoopUpdate();
				yield return null;
			}
			if (!ready()) Assert.Fail($"{message} Test line: {line}. {diagnostics?.Invoke()}");
		}

		private static object Get(object target, string field) => target.GetType()
			.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
		private static void Set(object target, string field, object value) => target.GetType()
			.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
		private static TaskCompletionSource<bool> Signal() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		internal sealed class Fixture : IDisposable
		{
			private readonly HttpListener _listener;
			private readonly bool _external;
			private readonly string _controlDirectory;
			private readonly string _testName;
			private readonly System.Collections.Concurrent.ConcurrentQueue<KeyValuePair<string, string>> _logLines = new System.Collections.Concurrent.ConcurrentQueue<KeyValuePair<string, string>>();
			private readonly Timer _logTimer;
			private int _writingLogs;
			private System.Diagnostics.Process _serverProcess;
			private readonly Task _timerProbe = Task.Delay(1000);
			private readonly BeamWebCommandFactoryOptions _options;
			private readonly BeamableDispatcher _dispatcher = new BeamableDispatcher();
			private readonly IDependencyProviderScope _provider;
			public readonly BeamWebCommandFactory Factory;
			public readonly BeamWebCommand Command;
			public readonly TaskCompletionSource<bool> ExecuteReceived = Signal();
			public readonly TaskCompletionSource<bool> AllowHeaders = Signal();
			public readonly TaskCompletionSource<bool> StopReceived = Signal();
			public readonly TaskCompletionSource<bool> ReleaseWorker = Signal();
			public readonly TaskCompletionSource<bool> AllowAcknowledgement = Signal();
			public readonly Task ServerTask;
			public string StopId;
			private bool _writeOutput = true;
			private int _stopStatus = 204;
			private bool _retryStop;
			private int[] _infoPids;
			public bool WriteOutput { get => _writeOutput; set { _writeOutput = value; WriteExternalConfig(); } }
			public int StopStatus { get => _stopStatus; set { _stopStatus = value; WriteExternalConfig(); } }
			public bool RetryStop { get => _retryStop; set { _retryStop = value; WriteExternalConfig(); } }
			public int[] InfoPids { get => _infoPids; set { _infoPids = value; WriteExternalConfig(); } }
			public readonly TaskCompletionSource<bool> RetryReceived = Signal();
			public readonly TaskCompletionSource<bool> AllowRetryAcknowledgement = Signal();

			public Fixture(List<Fixture> fixtures)
			{
				var projectRoot = Path.GetDirectoryName(Application.dataPath);
				var modeFile = Path.Combine(projectRoot, "beam-stop-probe-mode.txt");
				var mode = File.Exists(modeFile) ? File.ReadAllText(modeFile).Trim() : System.Environment.GetEnvironmentVariable("BEAM_STOP_SERVER_MODE");
				if (!string.IsNullOrEmpty(mode) && mode != "inprocess" && mode != "external")
					throw new InvalidOperationException("Unknown diagnostic server mode: " + mode);
				_external = mode == "external";
				_testName = TestContext.CurrentContext.Test.FullName;
				_controlDirectory = Path.Combine(projectRoot, "diagnostic-output", _external ? "external" : "inprocess", "fixture-" + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(_controlDirectory);
				_logTimer = new Timer(_ => FlushLogs(), null, 250, 250);
				Trace("fixture-created");
				var reservation = new TcpListener(IPAddress.Loopback, 0);
				reservation.Start();
				var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
				reservation.Stop();
				if (!_external)
				{
					_listener = new HttpListener();
					_listener.Prefixes.Add($"http://127.0.0.1:{port}/");
					_listener.Start();
					Trace("listener-started", port.ToString());
				}
				_options = ScriptableObject.CreateInstance<BeamWebCommandFactoryOptions>();
				_options.port = port;
				// Keep history logging in memory without starting telemetry, discovery, or CLI processes.
				var otel = (UnityOtelManager)FormatterServices.GetUninitializedObject(typeof(UnityOtelManager));
				foreach (var name in new[] { "_cachedLogs", "_cachedTimestamps" })
				{
					var field = typeof(UnityOtelManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
					field.SetValue(otel, Activator.CreateInstance(field.FieldType));
				}
				Set(otel, "_lastTimeFlush", double.MaxValue);
				_provider = new DependencyBuilder().AddSingleton(otel).Build();
				var history = new BeamWebCliCommandHistory(_options, _provider);
				history.OnAfterLoadState();
				Factory = (BeamWebCommandFactory)FormatterServices.GetUninitializedObject(typeof(BeamWebCommandFactory));
				Set(Factory, "_options", _options);
				Set(Factory, "_history", history);
				Set(Factory, "serverReadyCallbacks", new List<Action>());
				Factory.dispatcher = _dispatcher;
				Command = new BeamWebCommand(Factory, history);
				Command.SetCommand("beam content ps --watch");
				// The real CLI runs outside Unity; the fake server must not depend on Editor updates either.
				if (_external) StartExternalServer(projectRoot, port);
				ServerTask = Task.Run(_external ? (Func<Task>)ServeExternal : Serve);
				_ = ServerTask.ContinueWith(task => Trace("server-task-complete", task.Status + ": " + task.Exception));
				fixtures.Add(this);
			}

			[Serializable]
			private sealed class ProbeConfig
			{
				public bool awaitSignalObservation = true;
				public bool retryStop;
				public bool writeOutput;
				public int stopStatus;
				public string owner;
				public string version;
			}

			[Serializable]
			private sealed class ProbeSignal { public ProbeSignalData data; }
			[Serializable]
			private sealed class ProbeSignalData { public string requestId; }
			[Serializable]
			private sealed class ProbeEvent
			{
				public string utc, test, mode, eventName, context, detail;
				public int thread;
			}

			private void Trace(string name, string detail = null)
			{
				// Diagnostics must not turn a file-system logging error into a transport failure.
				try
				{
					var line = JsonUtility.ToJson(new ProbeEvent
					{
						utc = DateTime.UtcNow.ToString("O"), test = _testName,
						mode = _external ? "external" : "inprocess", eventName = name,
						thread = Thread.CurrentThread.ManagedThreadId,
						context = SynchronizationContext.Current?.GetType().FullName, detail = detail
					});
					_logLines.Enqueue(new KeyValuePair<string, string>("fixture.jsonl", line));
				}
				catch (Exception) { }
			}

			private void FlushLogs()
			{
				if (Interlocked.Exchange(ref _writingLogs, 1) != 0) return;
				try
				{
					while (_logLines.TryDequeue(out var entry))
						File.AppendAllText(Path.Combine(_controlDirectory, entry.Key), entry.Value + System.Environment.NewLine);
					if (_disposed && (ServerTask?.IsCompleted ?? true)) _logTimer.Dispose();
				}
				catch (Exception) { }
				finally { Volatile.Write(ref _writingLogs, 0); }
			}

			private void WriteExternalConfig()
			{
				if (!_external || Factory == null) return;
				// The server reads this only after accepting its first request. Object-initializer
				// properties are therefore applied before Run/Ping without adding test-side steps.
				var path = Path.Combine(_controlDirectory, "config.json");
				var temporary = path + ".tmp";
				var json = JsonUtility.ToJson(new ProbeConfig
				{
					retryStop = RetryStop, writeOutput = WriteOutput, stopStatus = StopStatus,
					owner = Factory.Owner, version = Factory.Version
				});
				// Preserve null versus empty-array semantics independently of Unity's serializer.
				json = json.Substring(0, json.Length - 1) + ",\"infoPids\":"
					+ (InfoPids == null ? "null" : "[" + string.Join(",", InfoPids) + "]") + "}";
				File.WriteAllText(temporary, json);
				if (File.Exists(path)) File.Replace(temporary, path, null);
				else File.Move(temporary, path);
				Trace("config-written");
			}

			private void StartExternalServer(string projectRoot, int port)
			{
				WriteExternalConfig();
				var binaryFile = Path.Combine(projectRoot, "beam-stop-probe-server.txt");
				var binary = File.Exists(binaryFile) ? File.ReadAllText(binaryFile).Trim()
					: Path.Combine(projectRoot, ".probe-server", Application.platform == RuntimePlatform.WindowsEditor ? "BeamStopProbeServer.exe" : "BeamStopProbeServer");
				if (!File.Exists(binary)) throw new FileNotFoundException("External diagnostic server executable is missing.", binary);
				_serverProcess = new System.Diagnostics.Process
				{
					StartInfo = new System.Diagnostics.ProcessStartInfo
					{
						FileName = binary, Arguments = port + " \"" + _controlDirectory + "\"",
						UseShellExecute = false, CreateNoWindow = true,
						RedirectStandardOutput = true, RedirectStandardError = true
					}
				};
				_serverProcess.OutputDataReceived += (_, e) => RecordServerOutput("stdout", e.Data);
				_serverProcess.ErrorDataReceived += (_, e) => RecordServerOutput("stderr", e.Data);
				_serverProcess.Start();
				_serverProcess.BeginOutputReadLine();
				_serverProcess.BeginErrorReadLine();
				Trace("external-started", "pid=" + _serverProcess.Id + ";port=" + port);
				// Readiness belongs to fixture setup; original operation deadlines remain unchanged.
				var deadline = DateTime.UtcNow.AddSeconds(10);
				while (!File.Exists(Path.Combine(_controlDirectory, "ready.json")))
				{
					if (_serverProcess.HasExited || DateTime.UtcNow >= deadline)
					{
						if (!_serverProcess.HasExited) _serverProcess.Kill();
						throw new InvalidOperationException("External diagnostic server did not become ready: " + _controlDirectory);
					}
					Thread.Sleep(10);
				}
				Trace("external-ready");
			}

			private void RecordServerOutput(string stream, string line)
			{
				if (line == null) return;
				try { _logLines.Enqueue(new KeyValuePair<string, string>("server-" + stream + ".jsonl", line)); }
				catch (Exception) { }
			}

			private void PublishGate(TaskCompletionSource<bool> gate, string name)
			{
				var path = Path.Combine(_controlDirectory, name);
				if (!gate.Task.IsCompleted || File.Exists(path)) return;
				File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
				Trace("gate-published", name);
			}

			private void PublishGates()
			{
				PublishGate(AllowHeaders, "allow-headers");
				PublishGate(ReleaseWorker, "release-worker");
				PublishGate(AllowAcknowledgement, "allow-ack");
				PublishGate(AllowRetryAcknowledgement, "allow-retry-ack");
			}

			private void ReadSignal(TaskCompletionSource<bool> signal, string name, bool stop)
			{
				var path = Path.Combine(_controlDirectory, name + ".json");
				if (signal.Task.IsCompleted || !File.Exists(path)) return;
				var json = File.ReadAllText(path);
				if (stop) StopId = JsonUtility.FromJson<ProbeSignal>(json).data.requestId;
				Trace("signal-received", name + ": " + json);
				signal.TrySetResult(true);
				// Preserve the original fixture's signal-before-response ordering even when
				// an assertion runs immediately after synchronous shutdown, without yielding.
				File.WriteAllText(Path.Combine(_controlDirectory, "observed-" + name.Replace("-received", "")), DateTime.UtcNow.ToString("O"));
			}

			private async Task ServeExternal()
			{
				while (true)
				{
					PublishGates();
					ReadSignal(ExecuteReceived, "execute-received", false);
					ReadSignal(StopReceived, "stop-received", true);
					ReadSignal(RetryReceived, "retry-received", true);
					if (_serverProcess.HasExited)
					{
						// Observe final signal files after process termination as well.
						ReadSignal(ExecuteReceived, "execute-received", false);
						ReadSignal(StopReceived, "stop-received", true);
						ReadSignal(RetryReceived, "retry-received", true);
						Trace("external-exited", "exit=" + _serverProcess.ExitCode);
						if (_serverProcess.ExitCode != 0)
							throw new InvalidOperationException("External diagnostic server exited with " + _serverProcess.ExitCode + "; see " + _controlDirectory);
						return;
					}
					await Task.Delay(10);
				}
			}

			private Queue<BeamableDispatcher.WorkItem> CallbackQueue =>
				((Dictionary<string, Queue<BeamableDispatcher.WorkItem>>)Get(_dispatcher, "_workQueues"))[BeamableDispatcher.DEFAULT_QUEUE_NAME];
			public bool CallbacksQueued => CallbackQueue.Count > 0;
			private Queue<BeamableDispatcher.WorkItem> _pausedQueue;
			public void PauseCallbacks()
			{
				Trace("callbacks-paused");
				_pausedQueue = CallbackQueue;
				((Dictionary<string, Queue<BeamableDispatcher.WorkItem>>)Get(_dispatcher, "_workQueues"))[BeamableDispatcher.DEFAULT_QUEUE_NAME] = new Queue<BeamableDispatcher.WorkItem>();
			}
			public void ResumeCallbacks()
			{
				if (_pausedQueue == null) return;
				Trace("callbacks-resumed");
				var held = CallbackQueue;
				lock (held)
					lock (_pausedQueue)
						while (held.Count > 0) _pausedQueue.Enqueue(held.Dequeue());
				((Dictionary<string, Queue<BeamableDispatcher.WorkItem>>)Get(_dispatcher, "_workQueues"))[BeamableDispatcher.DEFAULT_QUEUE_NAME] = _pausedQueue;
				_pausedQueue = null;
			}


			public string Describe()
			{
				ThreadPool.GetAvailableThreads(out var workers, out var io);
				var response = (Task)Get(Command, "_responseTask");
				var run = (Promise)Get(Command, "_runPromise");
				var history = (BeamWebCliCommandHistory)Get(Factory, "_history");
				var logs = history.commands[0].logs;
				return $"Mode={(_external ? "external" : "inprocess")}; Diagnostics={_controlDirectory}; " +
				       $"Execute={ExecuteReceived.Task.Status}; Stop={StopReceived.Task.Status}; " +
				       $"Server={ServerTask.Status}; ServerError={ServerTask.Exception}; " +
				       $"Response={response?.Status}; RunComplete={run?.IsCompleted}; " +
				       $"Timer={_timerProbe.Status}; AvailableWorkers={workers}; AvailableIO={io}; " +
				       $"LastCommandLog={(logs.Count == 0 ? "none" : logs[logs.Count - 1].message)}";
			}

			public void Discover()
			{
				var callbacks = (List<Action>)Get(Factory, "serverReadyCallbacks");
				var ready = callbacks.ToArray();
				callbacks.Clear();
				if (ready.Length > 0) Trace("discovery-callbacks", ready.Length.ToString());
				foreach (var callback in ready) callback();
			}


			private async Task Serve()
			{
				Trace("accept-execute-wait");
				var execute = await _listener.GetContextAsync();
				Trace("accept-execute-complete", execute.Request.RawUrl);
				if (InfoPids != null)
				{
					for (var i = 0; i < InfoPids.Length; i++)
					{
						var info = i == 0 ? execute : await _listener.GetContextAsync();
						var json = JsonUtility.ToJson(new ServerInfoResponse { owner = Factory.Owner, version = Factory.Version, pid = InfoPids[i] });
						var bytes = Encoding.UTF8.GetBytes(json);
						await info.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
						info.Response.Close();
						Trace("info-sent", InfoPids[i].ToString());
					}
					return;
				}
				using var executeReader = new StreamReader(execute.Request.InputStream);
				var request = JsonUtility.FromJson<BeamWebCommandRequest>(await executeReader.ReadToEndAsync());
				Trace("execute-received", request.requestId);
				ExecuteReceived.TrySetResult(true);
				Trace("gate-wait", "allow-headers");
				await AllowHeaders.Task;
				Trace("gate-open", "allow-headers");
				execute.Response.Headers.Set("X-Beam-Request-Id", request.requestId);
				var ready = Encoding.UTF8.GetBytes(": ready\n");
				await execute.Response.OutputStream.WriteAsync(ready, 0, ready.Length);
				await execute.Response.OutputStream.FlushAsync();
				Trace("headers-sent", request.requestId);
				var stop = await _listener.GetContextAsync();
				using var stopReader = new StreamReader(stop.Request.InputStream);
				StopId = JsonUtility.FromJson<BeamWebCommandRequest>(await stopReader.ReadToEndAsync()).requestId;
				Trace("stop-received", StopId);
				StopReceived.TrySetResult(true);
				Trace("gate-wait", "release-worker");
				await ReleaseWorker.Task;
				Trace("gate-open", "release-worker");
				if (WriteOutput)
				{
					var output = Encoding.UTF8.GetBytes("data: {\"type\":\"test\",\"data\":{},\"ts\":1}\n");
					await execute.Response.OutputStream.WriteAsync(output, 0, output.Length);
				}
				execute.Response.Close();
				Trace("execute-closed", request.requestId);
				Trace("gate-wait", "allow-ack");
				await AllowAcknowledgement.Task;
				Trace("gate-open", "allow-ack");
				stop.Response.StatusCode = StopStatus;
				stop.Response.Close();
				Trace("ack-sent", StopId + ": " + StopStatus);
				if (RetryStop)
				{
					var retry = await _listener.GetContextAsync();
					using var reader = new StreamReader(retry.Request.InputStream);
					StopId = JsonUtility.FromJson<BeamWebCommandRequest>(await reader.ReadToEndAsync()).requestId;
					Trace("retry-received", StopId);
					RetryReceived.TrySetResult(true);
					Trace("gate-wait", "allow-retry-ack");
					await AllowRetryAcknowledgement.Task;
					Trace("gate-open", "allow-retry-ack");
					retry.Response.StatusCode = 204;
					retry.Response.Close();
					Trace("retry-ack-sent", StopId);
				}
			}

			private bool _cleanupStarted;
			private bool _disposed;
			public bool IsDrained => ServerTask.IsCompleted
			                         && (((Promise)Get(Command, "_runPromise"))?.IsCompleted ?? true)
			                         && (((Promise)Get(Command, "_stopPromise"))?.IsCompleted ?? true);

			public void BeginCleanup()
			{
				if (_cleanupStarted) return;
				_cleanupStarted = true;
				Trace("cleanup-started", "server=" + ServerTask.Status);
				ResumeCallbacks();
				AllowHeaders.TrySetResult(true);
				ReleaseWorker.TrySetResult(true);
				AllowAcknowledgement.TrySetResult(true);
				AllowRetryAcknowledgement.TrySetResult(true);
				Command.Cancel();
				if (_external) File.WriteAllText(Path.Combine(_controlDirectory, "shutdown"), DateTime.UtcNow.ToString("O"));
				else _listener.Close();
				Trace("listener-shutdown-requested");
			}

			public void Dispose()
			{
				if (_disposed) return;
				_disposed = true;
				BeginCleanup();
				Trace("fixture-disposed", "server=" + ServerTask.Status);
				if (_serverProcess != null)
				{
					if (!_serverProcess.HasExited)
					{
						Trace("external-forced-kill");
						_serverProcess.Kill();
					}
					if (ServerTask.IsCompleted) _serverProcess.Dispose();
					else _ = ServerTask.ContinueWith(_ => _serverProcess.Dispose());
				}
				// Keep the dispatcher alive until teardown has drained pending callbacks.
				// Editor Coroutines 1.0 can still invoke callbacks captured by the current update.
				EditorApplication.delayCall += () => _dispatcher.OnDispose();
				UnityEngine.Object.DestroyImmediate(_options);
			}
		}
	}
}
