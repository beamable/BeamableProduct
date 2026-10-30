using cli.Commands.Mcp;
using cli.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace cli.Services.HttpServer;

/// <summary>
/// The MCP Streamable HTTP endpoint `server serve --enable-mcp` offers AI agents at <see cref="PATH"/>, next to
/// Unity's /execute. It is the SDK's own handler (protocol negotiation, sessions for clients that still use the
/// initialize handshake, stateless serving for 2026-07-28 clients), fed by the server's listener after it has
/// checked the request's host, origin and session token.
/// </summary>
public sealed class McpHttpEndpoint : IAsyncDisposable
{
	public const string PATH = "/mcp";
	public const string SERVER_NAME = "beam-cli";

	/// <summary>
	/// The protocol revisions this endpoint serves, as advertised in the Server Card: 2026-07-28 statelessly, the
	/// earlier ones through the initialize handshake. McpHttpEndpointTests connects with each of them.
	/// </summary>
	public static readonly string[] SupportedProtocolVersions = { "2026-07-28", "2025-11-25", "2025-06-18", "2025-03-26" };

	private readonly WebApplication _app;
	private readonly HttpListenerBridgeServer _server;

	// Requests still waiting for their response, by session and JSON-RPC id, so a cancellation can end them.
	private readonly ConcurrentDictionary<(string session, string id), CancellationTokenSource> _inflight = new();

	private McpHttpEndpoint(WebApplication app, HttpListenerBridgeServer server)
	{
		_app = app;
		_server = server;
	}

	/// <param name="version">The CLI version the endpoint reports as its server version.</param>
	/// <param name="instructions">What the agent should know about this server, such as which project it serves.</param>
	public static async Task<McpHttpEndpoint> Start(string version, string instructions, McpToolExecutor executor = null)
	{
		var server = new HttpListenerBridgeServer();
		var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
		builder.Services.AddSingleton<IServer>(server);
		// `server serve` owns process shutdown; the bridged app must not install its own signal handling.
		builder.Services.AddSingleton<IHostLifetime, NoSignalLifetime>();
		builder.Services.AddRouting();
		builder.Logging.ClearProviders();

		var tools = new BeamMcpTools(executor ?? new McpToolExecutor(), resolveWorkspaceFromRoots: false);
		builder.Services
			.AddMcpServer(options =>
			{
				options.ServerInfo = new Implementation { Name = SERVER_NAME, Title = "Beamable CLI", Version = version };
				options.ServerInstructions = instructions;
			})
			.WithHttpTransport(options =>
			{
				// 2026-07-28 clients are served per request; older clients keep a session, which is also how
				// their notifications/cancelled reaches the call it cancels.
				options.SessionMode = HttpServerSessionMode.StatefulForInitializeClients;
				options.IdleTimeout = TimeSpan.FromMinutes(30);
			})
			.WithTools<BeamMcpTools>(tools, null);

		var app = builder.Build();
		app.UseRouting();
		app.MapMcp(PATH);
		await app.StartAsync();
		return new McpHttpEndpoint(app, server);
	}

	/// <summary>Whether <paramref name="routePath"/> (the request path without slashes) belongs to this endpoint.</summary>
	public static bool Owns(string routePath) =>
		routePath == PATH.Trim('/') || routePath.StartsWith(PATH.Trim('/') + "/", StringComparison.Ordinal);

	/// <summary>Answers an already authorized request. The caller closes the response.</summary>
	public async Task HandleAsync(HttpListenerContext context)
	{
		if (context.Request.HttpMethod != "POST")
		{
			await _server.HandleAsync(context);
			return;
		}

		// JSON-RPC messages are small; reading them first tells us which request a POST carries.
		using var body = new MemoryStream();
		await context.Request.InputStream.CopyToAsync(body);
		body.Position = 0;
		var session = context.Request.Headers["Mcp-Session-Id"] ?? "";
		var (method, id, cancelledId) = Describe(body.GetBuffer().AsSpan(0, (int)body.Length));

		if (method == NotificationMethods.CancelledNotification && cancelledId != null)
		{
			await _server.HandleAsync(context, body);
			// The SDK sends nothing for a cancelled request and waits for its client to hang up, which HttpListener
			// never reports; end the request here, as Kestrel would when the client disconnects.
			if (_inflight.TryRemove((session, cancelledId), out var abandoned))
			{
				abandoned.Cancel();
			}
			return;
		}

		if (id == null)
		{
			await _server.HandleAsync(context, body);
			return;
		}

		using var aborted = new CancellationTokenSource();
		var key = (session, id);
		_inflight[key] = aborted;
		try
		{
			await _server.HandleAsync(context, body, aborted.Token);
		}
		finally
		{
			_inflight.TryRemove(new KeyValuePair<(string, string), CancellationTokenSource>(key, aborted));
		}
	}

	/// <summary>The method and id of a JSON-RPC message, and for a cancellation the id it cancels.</summary>
	private static (string method, string id, string cancelledId) Describe(ReadOnlySpan<byte> json)
	{
		try
		{
			if (JsonNode.Parse(json) is not JsonObject message)
			{
				return (null, null, null);
			}
			return ((string)message["method"], message["id"]?.ToJsonString(), message["params"]?["requestId"]?.ToJsonString());
		}
		catch (JsonException)
		{
			// Not ours to judge; the SDK answers malformed messages.
			return (null, null, null);
		}
	}

	public async ValueTask DisposeAsync()
	{
		await _app.StopAsync();
		await _app.DisposeAsync();
	}

	private sealed class NoSignalLifetime : IHostLifetime
	{
		public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
