using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tests;

[NonParallelizable]
public class CollectorDiscoveryTests
{
	private const string ScenarioKey = "BEAM_COLLECTOR_TEST_SCENARIO";
	private const string MarkerKey = "BEAM_COLLECTOR_TEST_MARKER";
	private const string PortKey = "BEAM_COLLECTOR_DISCOVERY_PORT";

	[TestCase(0, "READY", true)]
	[TestCase(30, "READY", true)]
	[TestCase(30, "NOT_READY", false)]
	public async Task Finds_matching_version_behind_other_collectors(int precedingMessages, string state, bool ready)
	{
		using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		receiver.ReceiveBufferSize = 65536;
		receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		using var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		for (int i = 0; i < precedingMessages; i++)
			await sender.SendToAsync(Announcement("other-version", "READY"), SocketFlags.None, receiver.LocalEndPoint!);
		await sender.SendToAsync(Announcement(CollectorManager.Version, state), SocketFlags.None, receiver.LocalEndPoint!);

		var status = await CollectorManager.IsCollectorRunning(receiver, CancellationToken.None, NullLogger.Instance);
		Assert.Multiple(() =>
		{
			Assert.That(status.isRunning, Is.True);
			Assert.That(status.isReady, Is.EqualTo(ready));
			Assert.That(status.pid, Is.EqualTo(Environment.ProcessId));
			Assert.That(status.version, Is.EqualTo(CollectorManager.Version));
		});
	}

	[Test]
	public async Task Missing_version_has_a_bounded_wait()
	{
		using var receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		await using var announcements = new Announcements(((IPEndPoint)receiver.LocalEndPoint!).Port, "other-version");
		var status = await CollectorManager.IsCollectorRunning(receiver, CancellationToken.None, NullLogger.Instance)
			.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.That(status.isRunning, Is.False);
	}

	// A regression here can call Environment.Exit. Keep it out of the parent test host.
	[TestCase("failure")]
	[TestCase("ready")]
	[TestCase("cancelled")]
	[TestCase("not-ready")]
	[TestCase("not-ready-timeout")]
	public async Task Cli_readiness_is_scoped_to_the_call(string scenario)
	{
		var directory = Path.Combine(Path.GetTempPath(), "beam-collector-test-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var marker = Path.Combine(directory, "completed");
		var start = new ProcessStartInfo("dotnet")
		{
			UseShellExecute = false, CreateNoWindow = true,
			RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory
		};
		start.ArgumentList.Add("vstest");
		start.ArgumentList.Add(typeof(CollectorDiscoveryTests).Assembly.Location);
		start.ArgumentList.Add("--TestCaseFilter:FullyQualifiedName=tests.CollectorDiscoveryTests.Child_process");
		start.Environment[ScenarioKey] = scenario;
		start.Environment[MarkerKey] = marker;
		using var process = Process.Start(start)!;
		var stdout = process.StandardOutput.ReadToEndAsync();
		var stderr = process.StandardError.ReadToEndAsync();
		try
		{
			await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(40));
			Assert.That(process.ExitCode, Is.Zero, await stdout + await stderr);
			Assert.That(File.Exists(marker), Is.True, "The child must complete its assertions, not merely discover zero tests.");
		}
		finally
		{
			if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); }
			Directory.Delete(directory, true);
		}
	}

	[Test]
	public async Task Child_process()
	{
		var scenario = Environment.GetEnvironmentVariable(ScenarioKey);
		if (string.IsNullOrEmpty(scenario)) Assert.Ignore("Executed by the isolated-process tests.");
		using var reservation = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		reservation.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		var port = ((IPEndPoint)reservation.LocalEndPoint!).Port;
		reservation.Dispose();
		Environment.SetEnvironmentVariable(PortKey, port.ToString());
		var directory = Directory.GetCurrentDirectory(); // The parent's unique, empty test directory.
		using var cancellation = new CancellationTokenSource();
		try
		{
			switch (scenario)
			{
				case "failure":
					await ServerServiceTests.VerifyCollectorFailureDoesNotStopServer(directory, cancellation);
					break;
				case "ready":
					await using (var announcements = new Announcements(port, CollectorManager.Version))
					{
						for (int i = 0; i < 3; i++)
						{
							var status = await CollectorManager.StartCollectorAndWait(directory, false, true, cancellation, NullLogger.Instance);
							Assert.That(status.isReady, Is.True);
							AssertPortReleased(port);
						}
					}
					break;
				case "cancelled":
					cancellation.Cancel();
					Assert.ThrowsAsync<OperationCanceledException>(async () =>
						await CollectorManager.StartCollectorAndWait(directory, false, true, cancellation, NullLogger.Instance));
					break;
				case "not-ready":
					await using (var announcements = new Announcements(port, CollectorManager.Version, "NOT_READY"))
					{
						cancellation.CancelAfter(300);
						Assert.CatchAsync<OperationCanceledException>(async () =>
							await CollectorManager.StartCollectorAndWait(directory, false, true, cancellation, NullLogger.Instance)
								.WaitAsync(TimeSpan.FromSeconds(3)));
					}
					break;
				case "not-ready-timeout":
					await using (var announcements = new Announcements(port, CollectorManager.Version, "NOT_READY"))
					{
						var error = Assert.ThrowsAsync<TimeoutException>(async () =>
							await CollectorManager.StartCollectorAndWait(directory, false, true, cancellation, NullLogger.Instance)
								.WaitAsync(TimeSpan.FromSeconds(20)));
						Assert.That(error!.Message, Does.Contain("Timed out waiting for the collector to become ready"));
					}
					break;
				default: Assert.Fail("Unknown child scenario"); break;
			}
			AssertPortReleased(port);
			File.WriteAllText(Environment.GetEnvironmentVariable(MarkerKey)!, "passed");
		}
		finally { cancellation.Cancel(); }
	}

	private static void AssertPortReleased(int port)
	{
		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		socket.ExclusiveAddressUse = true;
		socket.Bind(new IPEndPoint(IPAddress.Any, port));
	}

	// Wire fields and READY value come from the Go ServiceDiscovery producer, not C# serialization.
	private static byte[] Announcement(string version, string state) => Encoding.UTF8.GetBytes(
		$"{{\"version\":\"{version}\",\"status\":\"{state}\",\"pid\":{Environment.ProcessId},\"otlpEndpoint\":\"127.0.0.1:4318\"}}");

	private sealed class Announcements : IAsyncDisposable
	{
		private readonly CancellationTokenSource _stop = new();
		private readonly Task _send;
		public Announcements(int port, string version, string state = "READY") => _send = Send(port, version, state);
		private async Task Send(int port, string version, string state)
		{
			using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			var endpoint = new IPEndPoint(IPAddress.Loopback, port);
			try
			{
				while (!_stop.IsCancellationRequested)
				{
					await socket.SendToAsync(Announcement(version, state), SocketFlags.None, endpoint);
					await Task.Delay(10, _stop.Token);
				}
			}
			catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
		}
		public async ValueTask DisposeAsync() { _stop.Cancel(); await _send; _stop.Dispose(); }
	}
}
