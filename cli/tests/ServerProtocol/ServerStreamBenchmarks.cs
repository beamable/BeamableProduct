using Beamable.Common.Dependencies;
using cli;
using cli.CliServerCommand;
using cli.Services;
using cli.Services.HttpServer;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace tests.ServerProtocol;

/// <summary>
/// Throughput of Unity's /execute stream: time to first event, events per second and bytes allocated for a command
/// that reports many results. Self-contained (it only needs ServerService.HandleRequest), so the same file runs
/// against older builds for a baseline. Run explicitly: --filter "Category=Benchmark".
/// </summary>
[Explicit("Benchmark")]
[Category("Benchmark")]
[NonParallelizable]
public class ServerStreamBenchmarks
{
	public const int EventCount = 100_000;
	private const int Runs = 5;

	public class Item
	{
		public int index;
		public string name = "";
		public string status = "";
	}

	/// <summary>An app whose command reports <see cref="EventCount"/> results through the real reporter.</summary>
	public sealed class StreamingApp : App
	{
		private readonly int _count;
		public StreamingApp(int count) => _count = count;

		public override void Configure(Action<IDependencyBuilder>? serviceConfigurator = null,
			Action<IDependencyBuilder>? commandConfigurator = null, Action<ILoggingBuilder>? configureLogger = null,
			bool overwriteLogger = true)
		{
			Commands.AddSingleton(new AppLifecycle());
			Commands.AddSingleton<IDataReporterService>(_ => throw new InvalidOperationException("Replaced by the server."));
			serviceConfigurator!(Commands);
		}

		public override void Build() => CommandProvider = Commands.Build();

		public override Task<int> RunWithSingleString(string commandLine, bool useCustomSplitter)
		{
			var reporter = CommandProvider.GetService<IDataReporterService>();
			for (var i = 0; i < _count; i++)
			{
				reporter.Report("stream", new Item { index = i, name = $"content.item_{i:D6}", status = "modified" });
			}
			return Task.FromResult(0);
		}
	}

	public record Measurement(double firstEventMs, double totalMs, int events, long allocatedBytes)
	{
		public double EventsPerSecond => events / (totalMs / 1000);
	}

	public static Measurement Median(List<Measurement> runs) => new(
		runs.Select(r => r.firstEventMs).OrderBy(x => x).ElementAt(runs.Count / 2),
		runs.Select(r => r.totalMs).OrderBy(x => x).ElementAt(runs.Count / 2),
		runs[0].events,
		runs.Select(r => r.allocatedBytes).OrderBy(x => x).ElementAt(runs.Count / 2));

	public static void Print(string label, Measurement m) => TestContext.Progress.WriteLine(
		$"BENCH {label}: events={m.events} first-event={m.firstEventMs:F1}ms total={m.totalMs:F0}ms " +
		$"throughput={m.EventsPerSecond:F0}/s allocated={m.allocatedBytes / (1024 * 1024)}MB");

	[Test]
	public async Task Execute_stream_throughput()
	{
		var handleRequest = typeof(ServerService).GetMethod("HandleRequest", BindingFlags.Static | BindingFlags.NonPublic)!;
		var args = new ServeCliCommandArgs { owner = "bench", useCustomSplitter = true };
		_ = new App(); // register MSBuild before timing

		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();
		using var listener = new HttpListener();
		listener.Prefixes.Add($"http://127.0.0.1:{port}/");
		listener.Start();
		_ = Task.Run(async () =>
		{
			while (listener.IsListening)
			{
				HttpListenerContext ctx;
				try
				{
					ctx = await listener.GetContextAsync();
				}
				catch (Exception)
				{
					return;
				}
				_ = Task.Run(async () =>
				{
					await (Task)handleRequest.Invoke(null, new object?[] { args, ctx, 0UL, new StreamingApp(EventCount) })!;
				});
			}
		});

		using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromMinutes(5) };
		var runs = new List<Measurement>();
		for (var run = 0; run <= Runs; run++)
		{
			var measurement = await Measure(client);
			Assert.That(measurement.events, Is.EqualTo(EventCount));
			if (run > 0) // the first run warms up the JIT
			{
				runs.Add(measurement);
			}
		}
		Print("/execute", Median(runs));
	}

	private static async Task<Measurement> Measure(HttpClient client)
	{
		GC.Collect();
		var allocatedBefore = GC.GetTotalAllocatedBytes(true);
		var stopwatch = Stopwatch.StartNew();
		using var request = new HttpRequestMessage(HttpMethod.Post, "execute")
		{
			Content = new StringContent(JsonConvert.SerializeObject(new ServerRequest { commandLine = "bench" }), Encoding.UTF8, "application/json")
		};
		using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
		double firstEvent = -1;
		var events = 0;
		var lines = new List<string>();
		while (await reader.ReadLineAsync() is { } line)
		{
			if (lines.Count < 3)
			{
				lines.Add(line);
			}
			if (line.Replace("​", "").StartsWith("data: "))
			{
				if (events++ == 0)
				{
					firstEvent = stopwatch.Elapsed.TotalMilliseconds;
				}
			}
		}
		var total = stopwatch.Elapsed.TotalMilliseconds;
		Assert.That(events, Is.GreaterThan(1), "Only one event: " + string.Join("\n", lines));
		return new Measurement(firstEvent, total, events, GC.GetTotalAllocatedBytes(true) - allocatedBefore);
	}
}
