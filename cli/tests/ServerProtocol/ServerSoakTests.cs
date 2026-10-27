using cli.Services;
using cli.Services.CliInvocation;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace tests.ServerProtocol;

/// <summary>
/// `beam server serve` runs every request as a fresh app inside one long-lived process (the Unity editor keeps it for
/// the whole session), so nothing a request creates may outlive it.
/// </summary>
[NonParallelizable]
public class ServerSoakTests
{
	private string _originalDirectory = null!;
	private string _workspace = null!;

	private sealed class CountingReporter : IDataReporterService
	{
		public int Count;
		public void Report(string rawMessage) => Count++;
		public void Report<T>(string type, T data) => Count++;
	}

	[SetUp]
	public void SetUp()
	{
		// A workspace whose backend is unreachable, so commands never wait on the network.
		_originalDirectory = Directory.GetCurrentDirectory();
		_workspace = Path.Combine(Path.GetTempPath(), "beam-soak", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(_workspace, ".beamable"));
		File.WriteAllText(Path.Combine(_workspace, ".beamable", "config.beam.json"), JsonConvert.SerializeObject(new
		{
			cid = "1234567890",
			pid = "DE_1234567890",
			host = "http://127.0.0.1:9",
			otelConfig = new { BeamCliAllowTelemetry = false }
		}));
		Directory.SetCurrentDirectory(_workspace);
	}

	[TearDown]
	public void TearDown()
	{
		Directory.SetCurrentDirectory(_originalDirectory);
		try
		{
			Directory.Delete(_workspace, true);
		}
		catch (IOException)
		{
			// A log file may still be open; the temp folder is cleaned eventually.
		}
	}

	private static async Task<int> Run(IDataReporterService reporter, string command = "config --emit-log-streams")
	{
		return await CliInvocationCore.Execute(new CliInvocationRequest
		{
			ReadCommand = () => Task.FromResult((command, command)),
			UseCustomSplitter = true,
			CreateReporter = _ => reporter,
			ReportSetupFailure = _ => Task.CompletedTask
		});
	}

	// Kept out of line so no local in the test method keeps the reporter reachable.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> RunAndForget()
	{
		var reporter = new CountingReporter();
		await Run(reporter);
		Assert.That(reporter.Count, Is.GreaterThan(0), "The command must have reported through this request's reporter.");
		return new WeakReference(reporter);
	}

	[Test]
	public async Task A_finished_request_leaves_nothing_reachable()
	{
		// The process-wide meter provider is created by the first app that asks (in `server serve`, the server
		// itself) and keeps that one app's services; that is bounded, so start measuring after it exists.
		await Run(new CountingReporter());
		var first = await RunAndForget();
		// Later requests replace anything that is only "the latest request" (loggers and the like).
		for (var i = 0; i < 3; i++)
		{
			await Run(new CountingReporter());
		}

		for (var i = 0; i < 3; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		Assert.That(first.IsAlive, Is.False, "The first request's services are still reachable after it finished.");
	}

	[Test]
	public async Task Finished_execute_requests_leave_nothing_running()
	{
		// Through the real /execute handler and reporter, whose connection heartbeat must stop with the command.
		await using var server = new ServerServiceTests.TestServer(null);
		async Task Execute() => (await server.Send(System.Net.Http.HttpMethod.Post, "execute", "{\"commandLine\":\"config\"}")).Dispose();
		for (var i = 0; i < 5; i++)
		{
			await Execute();
		}
		var before = GC.GetTotalMemory(true);
		for (var i = 0; i < 40; i++)
		{
			await Execute();
		}
		var growthMb = (GC.GetTotalMemory(true) - before) / (1024.0 * 1024);
		Assert.That(growthMb, Is.LessThan(25), $"40 finished requests left {growthMb:F1} MB on the heap.");
		server.AssertNoErrors();
	}

	[Test]
	[Explicit("Benchmark")]
	[Category("Benchmark")]
	public async Task Many_requests_neither_grow_memory_nor_slow_down()
	{
		const int count = 300;
		await Run(new CountingReporter()); // warm up
		GC.Collect();
		var heapBefore = GC.GetTotalMemory(true);

		var latencies = new List<double>();
		for (var i = 0; i < count; i++)
		{
			var stopwatch = Stopwatch.StartNew();
			await Run(new CountingReporter());
			latencies.Add(stopwatch.Elapsed.TotalMilliseconds);
		}

		var heapGrowthMb = (GC.GetTotalMemory(true) - heapBefore) / (1024.0 * 1024);
		double P95(IEnumerable<double> xs) => xs.OrderBy(x => x).ElementAt((int)(xs.Count() * 0.95));
		var early = P95(latencies.Take(50));
		var late = P95(latencies.Skip(count - 50));
		TestContext.Progress.WriteLine(
			$"BENCH soak: requests={count} p50={latencies.OrderBy(x => x).ElementAt(count / 2):F1}ms " +
			$"p95-first50={early:F1}ms p95-last50={late:F1}ms heap-growth={heapGrowthMb:F1}MB");

		Assert.That(heapGrowthMb, Is.LessThan(50), "Managed heap must not grow with the number of requests.");
		Assert.That(late, Is.LessThanOrEqualTo(early * 1.2 + 2), "Requests must not get slower over time.");
	}
}
