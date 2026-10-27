using cli.CliServerCommand;
using cli.Commands.Mcp;
using cli.Services.HttpServer;
using ModelContextProtocol.Client;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace tests.ServerProtocol;

/// <summary>
/// The same command as <see cref="ServerStreamBenchmarks"/> called as an MCP tool over /mcp. A tool call returns
/// once, so the first event arrives with the whole result. Run explicitly: --filter "Category=Benchmark".
/// </summary>
[Explicit("Benchmark")]
[Category("Benchmark")]
[NonParallelizable]
public class McpStreamBenchmarks
{
	private const int Runs = 5;

	[TestCase(10_000)]
	[TestCase(ServerStreamBenchmarks.EventCount)]
	public async Task Mcp_tool_call_throughput(int eventCount)
	{
		var executor = new McpToolExecutor
		{
			AppFactory = () => new McpToolExecutorTests.ScriptedApp((_, reporter, _) =>
			{
				for (var i = 0; i < eventCount; i++)
				{
					reporter.Report("stream", new ServerStreamBenchmarks.Item { index = i, name = $"content.item_{i:D6}", status = "modified" });
				}
				return Task.FromResult(0);
			})
		};
		await using var endpoint = await McpHttpEndpoint.Start("bench", "bench", executor);
		var args = new ServeCliCommandArgs { owner = "bench", token = "bench-token", mcpEndpoint = endpoint };
		await using var server = new ServerServiceTests.TestServer(null, args: args);
		await using var client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
		{
			Endpoint = new Uri(server.Client.BaseAddress!, "mcp"),
			TransportMode = HttpTransportMode.StreamableHttp,
			AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer bench-token" }
		}));

		var runs = new List<ServerStreamBenchmarks.Measurement>();
		for (var run = 0; run <= Runs; run++)
		{
			GC.Collect();
			var allocatedBefore = GC.GetTotalAllocatedBytes(true);
			var stopwatch = Stopwatch.StartNew();
			var result = await client.CallToolAsync("beam_exec", new Dictionary<string, object?> { ["command"] = "bench" });
			var total = stopwatch.Elapsed.TotalMilliseconds;
			var events = result.StructuredContent!.Value.GetProperty("events").GetArrayLength();
			Assert.That(events, Is.EqualTo(eventCount));
			if (run > 0)
			{
				runs.Add(new ServerStreamBenchmarks.Measurement(total, total, events, GC.GetTotalAllocatedBytes(true) - allocatedBefore));
			}
		}
		ServerStreamBenchmarks.Print("/mcp", ServerStreamBenchmarks.Median(runs));
	}
}
