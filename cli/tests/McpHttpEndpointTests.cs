using cli.CliServerCommand;
using cli.Commands.Mcp;
using cli.Services.HttpServer;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tests;

/// <summary>
/// The MCP endpoint on `server serve`'s listener, driven by the SDK's own client the way Claude Code or the MCP
/// Inspector would, next to Unity-style /execute traffic.
/// </summary>
[NonParallelizable]
public class McpHttpEndpointTests
{
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
	private const string Token = "mcp-test-token";

	private McpHttpEndpoint _endpoint = null!;
	private ServeCliCommandArgs _args = null!;
	private Func<string, cli.Services.IDataReporterService, CancellationToken, Task<int>> _run = null!;

	[SetUp]
	public async Task SetUp()
	{
		_run = (_, _, _) => Task.FromResult(0);
		var executor = new McpToolExecutor { AppFactory = () => new McpToolExecutorTests.ScriptedApp((c, r, t) => _run(c, r, t)) };
		_endpoint = await McpHttpEndpoint.Start("1.2.3", "test workspace", executor);
		_args = new ServeCliCommandArgs { owner = "mcp-tests", token = Token, mcpEndpoint = _endpoint, useCustomSplitter = true };
	}

	[TearDown]
	public async Task TearDown()
	{
		await _endpoint.DisposeAsync();
	}

	private async Task<McpClient> Connect(ServerServiceTests.TestServer server, string? protocolVersion = null, string token = Token)
	{
		var transport = new HttpClientTransport(new HttpClientTransportOptions
		{
			Endpoint = new Uri(server.Client.BaseAddress!, "mcp"),
			TransportMode = HttpTransportMode.StreamableHttp,
			AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + token }
		});
		var options = new McpClientOptions();
		if (protocolVersion != null)
		{
			options.ProtocolVersion = protocolVersion;
		}
		return await McpClient.CreateAsync(transport, options).WaitAsync(Deadline);
	}

	private static string Text(CallToolResult result) => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

	public class Named { public string name = ""; }

	// Every version the Server Card advertises must really connect.
	public static IEnumerable<string> AdvertisedVersions() => McpHttpEndpoint.SupportedProtocolVersions;

	[TestCase(null, Description = "The SDK's default: 2026-07-28, discovered with server/discover")]
	[TestCaseSource(nameof(AdvertisedVersions))]
	public async Task Clients_on_each_protocol_version_list_and_call_tools(string? protocolVersion)
	{
		_run = (_, reporter, _) =>
		{
			reporter.Report("stream", new Named { name = "from-cli" });
			return Task.FromResult(0);
		};
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		await using var client = await Connect(server, protocolVersion);

		Assert.That(client.ServerInfo?.Name, Is.EqualTo(McpHttpEndpoint.SERVER_NAME));
		Assert.That(client.ServerInfo?.Version, Is.EqualTo("1.2.3"));
		Assert.That(client.ServerInstructions, Does.Contain("test workspace"));
		var tools = await client.ListToolsAsync().AsTask().WaitAsync(Deadline);
		Assert.That(tools.Select(t => t.Name), Does.Contain("beam_exec").And.Contain("beam_list_commands"));

		var result = await client.CallToolAsync("beam_exec", new Dictionary<string, object?> { ["command"] = "config" })
			.AsTask().WaitAsync(Deadline);
		Assert.That(result.IsError, Is.Not.True);
		Assert.That(Text(result), Does.Contain("from-cli"));
		server.AssertNoErrors();
	}

	[Test]
	public async Task Missing_token_is_rejected()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		using var response = await server.Send(HttpMethod.Post, "mcp",
			"{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}",
			configure: r => r.Headers.Accept.ParseAdd("application/json, text/event-stream"));
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

		Assert.ThrowsAsync(Is.InstanceOf<Exception>(), async () => await Connect(server, "2025-06-18", "wrong-token"));
		server.AssertNoErrors();
	}

	[Test]
	public async Task Web_origins_are_rejected()
	{
		// MCP's Streamable HTTP rules: validate Origin to stop DNS rebinding and cross-site calls.
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		using var response = await server.Send(HttpMethod.Post, "mcp",
			"{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}",
			configure: r =>
			{
				r.Headers.Authorization = new("Bearer", Token);
				r.Headers.Add("Origin", "https://attacker.example");
				r.Headers.Accept.ParseAdd("application/json, text/event-stream");
			});
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
		server.AssertNoErrors();
	}

	[Test]
	public async Task Endpoint_is_absent_unless_enabled()
	{
		var args = new ServeCliCommandArgs { owner = "mcp-tests", token = Token };
		await using var server = new ServerServiceTests.TestServer(null, args: args);
		using var response = await server.Send(HttpMethod.Post, "mcp", "{}",
			configure: r => r.Headers.Authorization = new("Bearer", Token));
		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Unknown route without --enable-mcp.");
	}

	[Test]
	public async Task Info_advertises_the_endpoint()
	{
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		var info = JsonConvert.DeserializeObject<ServerInfoResponse>(await server.Client.GetStringAsync("info"))!;
		Assert.That(info.features, Does.Contain(ServerService.FEATURE_MCP));
		Assert.That(info.mcpPath, Is.EqualTo(McpHttpEndpoint.PATH));
	}

	[Test]
	public async Task Progress_reaches_the_client()
	{
		_run = (_, reporter, _) =>
		{
			reporter.Report("progressStream", new McpToolExecutorTests.Ratio { ratio = 0.5f });
			reporter.Report("progressStream", new McpToolExecutorTests.Ratio { ratio = 1f });
			return Task.FromResult(0);
		};
		await using var server = new ServerServiceTests.TestServer(null, args: _args);
		await using var client = await Connect(server, "2025-06-18");
		var seen = new ConcurrentQueue<ProgressNotificationValue>();
		await client.CallToolAsync("beam_exec", new Dictionary<string, object?> { ["command"] = "content publish" },
			new SyncProgress(seen.Enqueue)).AsTask().WaitAsync(Deadline);
		// Notifications are sent independently, so only their set is guaranteed, not their arrival order.
		Assert.That(() => seen.Select(v => v.Progress).ToList(), Is.EquivalentTo(new[] { 0.5f, 1f }).After(5000, 50));
	}

	private sealed class SyncProgress : IProgress<ProgressNotificationValue>
	{
		private readonly Action<ProgressNotificationValue> _report;
		public SyncProgress(Action<ProgressNotificationValue> report) => _report = report;
		public void Report(ProgressNotificationValue value) => _report(value);
	}

	[Test]
	public async Task Cancelled_notification_cancels_the_command()
	{
		// What the TypeScript SDK clients (Claude Code, MCP Inspector) send when a user stops a call: a separate
		// POST in the same session. HttpListener cannot see a client simply hang up until the next write, so this
		// is the cancellation path that reaches a silent, long-running command.
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_run = async (_, _, lifecycle) =>
		{
			started.TrySetResult();
			try
			{
				await Task.Delay(Timeout.Infinite, lifecycle);
			}
			catch (OperationCanceledException)
			{
				cancelled.TrySetResult();
			}
			return 0;
		};
		await using var server = new ServerServiceTests.TestServer(null, args: _args);

		Task<HttpResponseMessage> Post(string json, string? session) => server.Send(HttpMethod.Post, "mcp", json, configure: r =>
		{
			r.Headers.Authorization = new("Bearer", Token);
			r.Headers.Accept.ParseAdd("application/json, text/event-stream");
			r.Headers.Add("MCP-Protocol-Version", "2025-06-18");
			if (session != null)
			{
				r.Headers.Add("Mcp-Session-Id", session);
			}
		});

		using var init = await Post("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{},\"clientInfo\":{\"name\":\"tests\",\"version\":\"1\"}}}", null);
		var session = init.Headers.GetValues("Mcp-Session-Id").Single();
		(await Post("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}", session)).Dispose();

		var call = Post("{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"tools/call\",\"params\":{\"name\":\"beam_exec\",\"arguments\":{\"command\":\"project run\"}}}", session);
		await started.Task.WaitAsync(Deadline);
		using var cancel = await Post("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/cancelled\",\"params\":{\"requestId\":7}}", session);
		Assert.That(cancel.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

		await cancelled.Task.WaitAsync(Deadline);
		(await call.WaitAsync(Deadline)).Dispose();
		server.AssertNoErrors();
	}

	[Test]
	public async Task Mcp_and_execute_run_side_by_side()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var mcpRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_run = async (_, reporter, _) =>
		{
			mcpRunning.TrySetResult();
			await release.Task;
			reporter.Report("stream", new Named { name = "mcp-done" });
			return 0;
		};
		var executeRan = false;
		await using var server = new ServerServiceTests.TestServer(() => new ServerServiceTests.ControlledApp((_, reporter) =>
		{
			executeRan = true;
			reporter.Report("stream", new Named { name = "unity-done" });
			return Task.FromResult(0);
		}), args: _args);
		await using var client = await Connect(server, "2025-06-18");

		var mcpCall = client.CallToolAsync("beam_exec", new Dictionary<string, object?> { ["command"] = "services ps" }).AsTask();
		await mcpRunning.Task.WaitAsync(Deadline);

		// Unity's request completes while the agent's command is still running.
		using var unity = await server.Send(HttpMethod.Post, "execute",
			JsonConvert.SerializeObject(new ServerRequest { commandLine = "content ps" }),
			configure: r => r.Headers.Authorization = new("Bearer", Token));
		var events = tests.ServerProtocol.ExecuteGoldenTests.ParseEvents(await unity.Content.ReadAsStringAsync());
		Assert.That(executeRan, Is.True);
		Assert.That(events.Single()["data"]?["name"]?.ToString(), Is.EqualTo("unity-done"));

		release.SetResult();
		var result = await mcpCall.WaitAsync(Deadline);
		Assert.That(Text(result), Does.Contain("mcp-done"));
		server.AssertNoErrors();
	}
}
