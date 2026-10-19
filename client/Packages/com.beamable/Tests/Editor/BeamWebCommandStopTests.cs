using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli;
using Beamable.Editor.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests
{
	public class BeamWebCommandStopTests
	{
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
				using var fixture = new Fixture { WriteOutput = false };
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
			using var fixture = new Fixture { InfoPids = new[] { int.MaxValue, System.Diagnostics.Process.GetCurrentProcess().Id } };
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
				using var fixture = new Fixture { WriteOutput = false };
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
			using var fixture = new Fixture();
			fixture.Command.Run().Error(_ => { });
			fixture.Discover();
			var first = fixture.Command.StopAsync();
			first.Error(_ => { });
			yield return WaitFor(() => first.IsCompleted);
			Assert.That(first.IsFailed, Is.True);
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
			using var fixture = new Fixture { RetryStop = true };
			fixture.Command.Run().Error(_ => { });
			fixture.Discover();
			fixture.AllowHeaders.TrySetResult(true);
			fixture.ReleaseWorker.TrySetResult(true);
			var first = fixture.Command.StopAsync();
			first.Error(_ => { });
			yield return WaitFor(() => first.IsCompleted);
			Assert.That(first.IsFailed, Is.True);
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
				using var fixture = new Fixture(backgroundServer: true) { WriteOutput = false, StopStatus = 500 };
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
			using var fixture = new Fixture(backgroundServer: true) { WriteOutput = false };
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
			using var fixture = new Fixture();
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
			using var fixture = new Fixture();
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
			using var fixture = new Fixture { WriteOutput = false };
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
			using var fixture = new Fixture { StopStatus = 500 };
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
			using var fixture = new Fixture();
			var stop = fixture.Command.StopAsync();
			Assert.That(stop.IsCompleted, Is.True);
			Assert.That(fixture.Command.Run(), Is.SameAs(stop));
			Assert.That(fixture.Factory.discoveryRequest, Is.Zero);
		}

		[UnityTest]
		public IEnumerator FailedAcknowledgementCanRetryWithoutDuplicatingPendingOrSuccessfulStop()
		{
			using var fixture = new Fixture { StopStatus = 500, RetryStop = true };
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

		private static IEnumerator WaitFor(Func<bool> ready, string message = "Timed out waiting for controlled transport gate.")
		{
			var deadline = DateTime.UtcNow.AddSeconds(15);
			while (!ready() && DateTime.UtcNow < deadline) yield return null;
			Assert.That(ready(), Is.True, message);
		}

		private static object Get(object target, string field) => target.GetType()
			.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
		private static void Set(object target, string field, object value) => target.GetType()
			.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
		private static TaskCompletionSource<bool> Signal() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		internal sealed class Fixture : IDisposable
		{
			private readonly HttpListener _listener = new HttpListener();
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
			public bool WriteOutput = true;
			public int StopStatus = 204;
			public bool RetryStop;
			public int[] InfoPids;
			public readonly TaskCompletionSource<bool> RetryReceived = Signal();
			public readonly TaskCompletionSource<bool> AllowRetryAcknowledgement = Signal();

			public Fixture(bool backgroundServer = false)
			{
				var reservation = new TcpListener(IPAddress.Loopback, 0);
				reservation.Start();
				var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
				reservation.Stop();
				_listener.Prefixes.Add($"http://127.0.0.1:{port}/");
				_listener.Start();
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
				ServerTask = backgroundServer ? Task.Run(Serve) : Serve();
				_ = ServerTask.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
			}

			private Queue<BeamableDispatcher.WorkItem> CallbackQueue =>
				((Dictionary<string, Queue<BeamableDispatcher.WorkItem>>)Get(_dispatcher, "_workQueues"))[BeamableDispatcher.DEFAULT_QUEUE_NAME];
			public bool CallbacksQueued => CallbackQueue.Count > 0;
			private Queue<BeamableDispatcher.WorkItem> _pausedQueue;
			public void PauseCallbacks()
			{
				_pausedQueue = CallbackQueue;
				((Dictionary<string, Queue<BeamableDispatcher.WorkItem>>)Get(_dispatcher, "_workQueues"))[BeamableDispatcher.DEFAULT_QUEUE_NAME] = new Queue<BeamableDispatcher.WorkItem>();
			}
			public void ResumeCallbacks()
			{
				var held = CallbackQueue;
				lock (held)
					lock (_pausedQueue)
						while (held.Count > 0) _pausedQueue.Enqueue(held.Dequeue());
				((Dictionary<string, Queue<BeamableDispatcher.WorkItem>>)Get(_dispatcher, "_workQueues"))[BeamableDispatcher.DEFAULT_QUEUE_NAME] = _pausedQueue;
			}


			public void Discover()
			{
				var callbacks = (List<Action>)Get(Factory, "serverReadyCallbacks");
				var ready = callbacks.ToArray();
				callbacks.Clear();
				foreach (var callback in ready) callback();
			}


			private async Task Serve()
			{
				var execute = await _listener.GetContextAsync();
				if (InfoPids != null)
				{
					for (var i = 0; i < InfoPids.Length; i++)
					{
						var info = i == 0 ? execute : await _listener.GetContextAsync();
						var json = JsonUtility.ToJson(new ServerInfoResponse { owner = Factory.Owner, version = Factory.Version, pid = InfoPids[i] });
						var bytes = Encoding.UTF8.GetBytes(json);
						await info.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
						info.Response.Close();
					}
					return;
				}
				using var executeReader = new StreamReader(execute.Request.InputStream);
				var request = JsonUtility.FromJson<BeamWebCommandRequest>(await executeReader.ReadToEndAsync());
				ExecuteReceived.TrySetResult(true);
				await AllowHeaders.Task;
				execute.Response.Headers.Set("X-Beam-Request-Id", request.requestId);
				var ready = Encoding.UTF8.GetBytes(": ready\n");
				await execute.Response.OutputStream.WriteAsync(ready, 0, ready.Length);
				await execute.Response.OutputStream.FlushAsync();
				var stop = await _listener.GetContextAsync();
				using var stopReader = new StreamReader(stop.Request.InputStream);
				StopId = JsonUtility.FromJson<BeamWebCommandRequest>(await stopReader.ReadToEndAsync()).requestId;
				StopReceived.TrySetResult(true);
				await ReleaseWorker.Task;
				if (WriteOutput)
				{
					var output = Encoding.UTF8.GetBytes("data: {\"type\":\"test\",\"data\":{},\"ts\":1}\n");
					await execute.Response.OutputStream.WriteAsync(output, 0, output.Length);
				}
				execute.Response.Close();
				await AllowAcknowledgement.Task;
				stop.Response.StatusCode = StopStatus;
				stop.Response.Close();
				if (RetryStop)
				{
					var retry = await _listener.GetContextAsync();
					using var reader = new StreamReader(retry.Request.InputStream);
					StopId = JsonUtility.FromJson<BeamWebCommandRequest>(await reader.ReadToEndAsync()).requestId;
					RetryReceived.TrySetResult(true);
					await AllowRetryAcknowledgement.Task;
					retry.Response.StatusCode = 204;
					retry.Response.Close();
				}
			}

			public void Dispose()
			{
				AllowHeaders.TrySetResult(true);
				ReleaseWorker.TrySetResult(true);
				AllowAcknowledgement.TrySetResult(true);
				AllowRetryAcknowledgement.TrySetResult(true);
				Command.Cancel();
				_listener.Close();
				_dispatcher.OnDispose();
				UnityEngine.Object.DestroyImmediate(_options);
			}
		}
	}
}
