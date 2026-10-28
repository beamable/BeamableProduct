using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.Content;
using Beamable.Editor.BeamCli;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests.CLI
{
	/// <summary>
	/// The editor's client for the local CLI server (<see cref="BeamWebCommand"/>), run against a fake server so each
	/// rule the client learned the hard way stays covered: how it reads the stream, retries, cancels and discovers.
	/// </summary>
	public class BeamWebCommandTests
	{
		private const string Owner = "beam-web-command-tests";
		private const string Version = "0.0.0-test";
		private const float TimeoutSeconds = 20;

		private BeamableDispatcher _dispatcher;
		private BeamWebCommandFactoryOptions _options;
		private BeamWebCommandFactory _factory;
		private readonly List<FakeCliServer> _servers = new List<FakeCliServer>();

		/// <summary>A CLI server stand-in: answers /info and lets each test script /execute.</summary>
		private class FakeCliServer : IDisposable
		{
			public readonly int Port;
			public string Version = BeamWebCommandTests.Version;
			/// <summary>The "features" array /info reports, as JSON, or null to leave it out (an older CLI).</summary>
			public string FeaturesJson;
			public int Pid;
			public readonly ConcurrentQueue<HttpListenerRequest> ExecuteRequests = new ConcurrentQueue<HttpListenerRequest>();
			public Func<HttpListenerContext, int, Task> OnExecute = (ctx, _) => Respond(ctx, 200, "");

			private readonly HttpListener _listener = new HttpListener();
			private int _executeCount;

			public FakeCliServer(int port)
			{
				Port = port;
				_listener.Prefixes.Add($"http://127.0.0.1:{port}/");
				_listener.Start();
				Task.Run(Accept);
			}

			private async Task Accept()
			{
				while (_listener.IsListening)
				{
					HttpListenerContext ctx;
					try
					{
						ctx = await _listener.GetContextAsync();
					}
					catch (Exception)
					{
						return;
					}

					_ = Task.Run(async () =>
					{
						try
						{
							if (ctx.Request.Url.AbsolutePath == "/info")
							{
								var features = FeaturesJson == null ? "" : $",\"features\":{FeaturesJson}";
								var info = $"{{\"version\":\"{Version}\",\"owner\":\"{Owner}\",\"pid\":{Pid}{features}}}";
								await Respond(ctx, 200, info);
							}
							else
							{
								ExecuteRequests.Enqueue(ctx.Request);
								await OnExecute(ctx, Interlocked.Increment(ref _executeCount));
							}
						}
						catch (Exception)
						{
							// A scripted disconnect.
						}
					});
				}
			}

			public static async Task Respond(HttpListenerContext ctx, int status, string body)
			{
				ctx.Response.StatusCode = status;
				var bytes = Encoding.UTF8.GetBytes(body);
				await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
				ctx.Response.Close();
			}

			public static async Task Write(HttpListenerContext ctx, string text)
			{
				var bytes = Encoding.UTF8.GetBytes(text);
				await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
				await ctx.Response.OutputStream.FlushAsync();
			}

			public void Dispose()
			{
				_listener.Close();
			}
		}

		private static string DataLine(string name) =>
			"data: {\"ts\":1,\"type\":\"stream\",\"data\":{\"name\":\"" + name + "\"}}\n";

		private static int FreePortPair()
		{
			// Two adjacent free ports, for the discovery tests that step from one server to the next.
			for (var attempt = 0; attempt < 50; attempt++)
			{
				var probe = new TcpListener(IPAddress.Loopback, 0);
				probe.Start();
				var port = ((IPEndPoint)probe.LocalEndpoint).Port;
				probe.Stop();
				try
				{
					var next = new TcpListener(IPAddress.Loopback, port + 1);
					next.Start();
					next.Stop();
					return port;
				}
				catch (SocketException)
				{
					// Taken; try another pair.
				}
			}
			throw new InvalidOperationException("No two adjacent free ports.");
		}

		private FakeCliServer StartServer(int port)
		{
			var server = new FakeCliServer(port);
			_servers.Add(server);
			return server;
		}

		private IEnumerator CreateFactory(int startPort)
		{
			var ctx = BeamEditorContext.Default;
			yield return ctx.InitializePromise.ToYielder();

			_dispatcher = new BeamableDispatcher();
			_options = ScriptableObject.CreateInstance<BeamWebCommandFactoryOptions>();
			_options.startPortOverride = new OptionalInt(startPort);
			_options.ownerOverride = new OptionalString(Owner);
			_options.versionOverride = new OptionalString(Version);
			var history = new BeamWebCliCommandHistory(_options, ctx.ServiceScope);
			_factory = new BeamWebCommandFactory(_dispatcher, history, _options, null);
		}

		private static IEnumerator WaitFor(Func<bool> condition, string what)
		{
			var stopwatch = Stopwatch.StartNew();
			while (!condition())
			{
				if (stopwatch.Elapsed.TotalSeconds > TimeoutSeconds)
				{
					Assert.Fail($"Timed out waiting for {what}.");
				}
				yield return null;
			}
		}

		private BeamWebCommand Command(List<string> received, Action onTerminate = null)
		{
			var command = (BeamWebCommand)_factory.Create();
			command.SetCommand("beam config");
			command.On(desc =>
			{
				if (desc.type == "stream")
				{
					received.Add(desc.json);
				}
			});
			command.OnTerminate(_ => onTerminate?.Invoke());
			return command;
		}

		[TearDown]
		public void TearDown()
		{
			_dispatcher?.OnDispose();
			foreach (var server in _servers)
			{
				server.Dispose();
			}
			_servers.Clear();
		}

		// R5 (#4801): the end of the stream is a null line from the async read, never a blocking EndOfStream check;
		// empty lines and the server's zero-width heartbeat are skipped.
		[UnityTest]
		public IEnumerator NullLineIsEof()
		{
			var port = FreePortPair();
			var server = StartServer(port);
			server.OnExecute = async (ctx, _) =>
			{
				await FakeCliServer.Write(ctx, DataLine("first") + "\n​");
				await FakeCliServer.Write(ctx, DataLine("second"));
				ctx.Response.Close();
			};
			yield return CreateFactory(port);

			var received = new List<string>();
			var terminated = 0;
			var run = Command(received, () => terminated++).Run();
			yield return WaitFor(() => run.IsCompleted, "the command to finish");

			Assert.That(received.Count, Is.EqualTo(2));
			Assert.That(received[0], Does.Contain("first"));
			Assert.That(received[1], Does.Contain("second"), "The heartbeat prefix must be stripped, not break the line.");
			Assert.That(terminated, Is.EqualTo(1));
		}

		// R1 (#4601) and R3 (#4635): a read waiting on a stalled stream wakes up on cancel, and cancelling does not
		// surface as an uncaught promise error.
		[UnityTest]
		public IEnumerator CancelUnblocksStalledReadWithoutUncaughtError()
		{
			var port = FreePortPair();
			var server = StartServer(port);
			var release = new TaskCompletionSource<bool>();
			server.OnExecute = async (ctx, _) =>
			{
				await FakeCliServer.Write(ctx, DataLine("before-stall"));
				await release.Task; // the server goes quiet without closing
				ctx.Response.Close();
			};
			yield return CreateFactory(port);

			var received = new List<string>();
			var command = Command(received);
			var run = command.Run();
			yield return WaitFor(() => received.Count == 1, "the first event");

			var cancelledAt = Stopwatch.StartNew();
			command.Cancel();
			yield return WaitFor(() => run.IsCompleted, "the cancelled command to finish");
			release.TrySetResult(true);

			Assert.That(cancelledAt.Elapsed.TotalSeconds, Is.LessThan(2), "Cancel must wake the read, not wait for data.");
			LogAssert.NoUnexpectedReceived();
		}

		// R4 (#4662): a failure before the response starts is retried. A 401 means nothing ran.
		[UnityTest]
		public IEnumerator RetryOnlyPreResponse_RefusedTokenIsRetried()
		{
			var port = FreePortPair();
			var server = StartServer(port);
			server.OnExecute = async (ctx, attempt) =>
			{
				if (attempt == 1)
				{
					await FakeCliServer.Respond(ctx, 401, "{\"type\":\"Unauthorized\"}");
					return;
				}
				await FakeCliServer.Write(ctx, DataLine("after-retry"));
				ctx.Response.Close();
			};
			yield return CreateFactory(port);

			var received = new List<string>();
			var run = Command(received).Run();
			yield return WaitFor(() => run.IsCompleted, "the command to finish");

			Assert.That(server.ExecuteRequests.Count, Is.EqualTo(2));
			Assert.That(received.Single(), Does.Contain("after-retry"));
		}

		// R4 (#4662): once the response has started, a broken stream is not retried; the command may have run.
		[UnityTest]
		public IEnumerator RetryOnlyPreResponse_StartedResponseIsNotRetried()
		{
			var port = FreePortPair();
			var server = StartServer(port);
			server.OnExecute = async (ctx, _) =>
			{
				await FakeCliServer.Write(ctx, DataLine("partial"));
				ctx.Response.Abort();
			};
			yield return CreateFactory(port);

			// The broken stream is reported as an error; that is the expected outcome here.
			LogAssert.ignoreFailingMessages = true;
			var received = new List<string>();
			var run = Command(received).Run();
			run.Error(_ => { });
			yield return WaitFor(() => run.IsCompleted, "the command to finish");

			Assert.That(server.ExecuteRequests.Count, Is.EqualTo(1));
		}

		// R2 (#4592): repeated transport failures the client did not cause make it drop the server and rediscover,
		// on the dispatcher, before giving up.
		[UnityTest]
		public IEnumerator InvalidateOnTransportFailure()
		{
			var port = FreePortPair();
			var server = StartServer(port);
			server.OnExecute = (ctx, _) =>
			{
				ctx.Response.Abort();
				return Task.CompletedTask;
			};
			yield return CreateFactory(port);
			_options.token = "stale";

			// Giving up is reported as an error; that is the expected outcome here.
			LogAssert.ignoreFailingMessages = true;
			var run = Command(new List<string>()).Run();
			run.Error(_ => { });
			yield return WaitFor(() => run.IsCompleted, "the command to give up");

			Assert.That(server.ExecuteRequests.Count, Is.EqualTo(3), "One try and two retries.");
			Assert.That(_options.token, Is.Null, "Invalidating the server forgets its token.");
			Assert.That(_factory.port, Is.EqualTo(port), "Discovery restarts at the start port and finds the server again.");
		}

		// Every /execute carries the server's session token.
		[UnityTest]
		public IEnumerator ExecuteSendsTheSessionToken()
		{
			var port = FreePortPair();
			var server = StartServer(port);
			yield return CreateFactory(port);
			_options.token = "session-token";

			var run = Command(new List<string>()).Run();
			yield return WaitFor(() => run.IsCompleted, "the command to finish");

			Assert.That(server.ExecuteRequests.Single().Headers["Authorization"], Is.EqualTo("Bearer session-token"));
		}

		// R9 (35b6fdcd1): a server of another CLI version is skipped and discovery moves to the next port.
		[UnityTest]
		public IEnumerator VersionMismatchSkipsPort()
		{
			var port = FreePortPair();
			var wrongVersion = StartServer(port);
			wrongVersion.Version = "9.9.9";
			var right = StartServer(port + 1);
			yield return CreateFactory(port);

			var run = Command(new List<string>()).Run();
			yield return WaitFor(() => run.IsCompleted, "the command to finish");

			Assert.That(wrongVersion.ExecuteRequests, Is.Empty);
			Assert.That(right.ExecuteRequests.Count, Is.EqualTo(1));
			Assert.That(_factory.port, Is.EqualTo(port + 1));
		}

		// A server started before "Enable MCP for AI Agents" changed is skipped, so the toggle gets a server with the
		// right endpoints instead of reusing the old one.
		[UnityTest]
		public IEnumerator McpSettingMismatchSkipsServer()
		{
			var previous = McpServerSettings.Enabled;
			McpServerSettings.Enabled = true;
			try
			{
				var port = FreePortPair();
				var withoutMcp = StartServer(port);
				withoutMcp.FeaturesJson = "[\"auth\"]";
				var withMcp = StartServer(port + 1);
				withMcp.FeaturesJson = "[\"auth\",\"mcp\"]";
				yield return CreateFactory(port);

				var run = Command(new List<string>()).Run();
				yield return WaitFor(() => run.IsCompleted, "the command to finish");

				Assert.That(withoutMcp.ExecuteRequests, Is.Empty);
				Assert.That(withMcp.ExecuteRequests.Count, Is.EqualTo(1));
			}
			finally
			{
				McpServerSettings.Enabled = previous;
			}
		}

		// Cancelling the command that started the server only stops the `dotnet beam` launcher; the server process
		// it found by owner must be stopped too, or the next discovery reuses it.
		[UnityTest]
		[UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor)]
		public IEnumerator KillServerKillsTheServerProcess()
		{
			// A copy of `sleep` whose process name looks like the CLI's stands in for the server process.
			var exe = Path.Combine(Path.GetTempPath(), "dotnet-fake-cli-" + Guid.NewGuid().ToString("N").Substring(0, 8));
			Process.Start(new ProcessStartInfo("/bin/cp", $"/bin/sleep \"{exe}\"") { UseShellExecute = false })?.WaitForExit();
			var sleeper = Process.Start(new ProcessStartInfo(exe, "30") { UseShellExecute = false });
			try
			{
				var port = FreePortPair();
				var server = StartServer(port);
				server.Pid = sleeper.Id;
				yield return CreateFactory(port);
				var run = Command(new List<string>()).Run();
				yield return WaitFor(() => run.IsCompleted, "the command to finish");

				_factory.KillServer();
				yield return WaitFor(() => sleeper.HasExited, "the server process to stop");
				Assert.That(_factory.port, Is.EqualTo(port), "Discovery starts over at the start port.");
			}
			finally
			{
				if (!sleeper.HasExited)
				{
					sleeper.Kill();
				}
				File.Delete(exe);
			}
		}

		// R8 (1fcad5291, f247bf55d): the server dies with this editor and splits command lines with JSON intact.
		[UnityTest]
		public IEnumerator StartServerArgs()
		{
			var port = FreePortPair();
			StartServer(port);
			yield return CreateFactory(port);

			var args = _factory.CreateServerServeArgs();
			Assert.That(args.requireProcessId, Is.EqualTo(Process.GetCurrentProcess().Id));
			Assert.That(args.customSplitter, Is.True);
			Assert.That(args.autoIncPort, Is.True);
			Assert.That(args.enableMcp, Is.EqualTo(McpServerSettings.Enabled));
			Assert.That(args.owner, Is.EqualTo("\"" + Owner + "\""));
		}

		[Test]
		public void PreferredPortMatchesTheCli()
		{
			// Same algorithm as `beam server serve` without --port, so both sides land on the same port.
			var port = CliServerPorts.PreferredPort("/Users/dev/Projects/MyGame");
			Assert.That(CliServerPorts.PreferredPort("/users/dev/projects/mygame/"), Is.EqualTo(port));
			Assert.That(port, Is.InRange(8500, CliServerPorts.RANGE_END));
		}

		// R4 (#4662): overlapping BeamCli.Refresh() calls share one in-flight refresh.
		[Test]
		[Ignore("BeamCli.Refresh runs real CLI commands through the editor's own server; covering it needs a seam to inject the command factory.")]
		public void RefreshCoalesces()
		{
		}
	}
}
