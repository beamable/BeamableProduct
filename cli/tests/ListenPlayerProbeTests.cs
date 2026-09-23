using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cli.Notifications;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace tests;

/// <summary>
/// Tests for `beam listen player --probe`. The decision logic is tested directly; the socket part runs against
/// an in-process server on a free localhost port, so nothing here touches Beamable.
/// </summary>
public class ListenPlayerProbeTests
{
	[Test]
	public void BuildConnectUrl_MatchesTheWebSdk()
	{
		var url = ListenPlayerProbe.BuildConnectUrl("wss://socket.beamable.com", "a.b.c");
		Assert.AreEqual("wss://socket.beamable.com/connect?access_token=a.b.c&send-session-start=true", url);
	}

	[Test]
	public void BuildConnectUrl_TrimsTrailingSlashAndEscapesToken()
	{
		var url = ListenPlayerProbe.BuildConnectUrl("wss://socket.beamable.com/", "a+b/c=");
		Assert.AreEqual("wss://socket.beamable.com/connect?access_token=a%2Bb%2Fc%3D&send-session-start=true", url);
	}

	[Test]
	public void BuildSessionStartFrame_HasTypeAndDevice()
	{
		var frame = JObject.Parse(ListenPlayerProbe.BuildSessionStartFrame());
		Assert.AreEqual("session-start", frame["type"]?.Value<string>());
		Assert.AreEqual(ListenPlayerProbe.SESSION_START_PLATFORM, frame["device"]?["platform"]?.Value<string>());
		Assert.AreEqual(ListenPlayerProbe.SESSION_START_MODEL, frame["device"]?["model"]?.Value<string>());
	}

	[Test]
	public void UnsupportedProvider_PubnubIsRejectedWithTheRealmConfigCommand()
	{
		var message = ListenPlayerProbe.GetUnsupportedProviderMessage("pubnub");
		Assert.IsNotNull(message);
		StringAssert.Contains("pubnub", message);
		StringAssert.Contains("beam config realm set --key-values 'notification|publisher::beamable'", message);
	}

	[Test]
	public void UnsupportedProvider_BeamableIsAccepted()
	{
		Assert.IsNull(ListenPlayerProbe.GetUnsupportedProviderMessage("beamable"));
		Assert.IsNull(ListenPlayerProbe.GetUnsupportedProviderMessage(null));
	}

	[Test]
	public void TruncateFrame_LimitsLongFrames()
	{
		var longFrame = new string('x', ListenPlayerProbe.MAX_FRAME_LENGTH + 10);
		var truncated = ListenPlayerProbe.TruncateFrame(longFrame);
		StringAssert.StartsWith(new string('x', ListenPlayerProbe.MAX_FRAME_LENGTH) + "...", truncated);
		StringAssert.Contains($"({longFrame.Length} chars)", truncated);
		Assert.AreEqual("short", ListenPlayerProbe.TruncateFrame("short"));
	}

	[Test]
	public void Interpret_Unauthorized_PointsAtTheToken()
	{
		var result = new ListenPlayerProbeResult { socketUri = "wss://socket.beamable.com", handshakeStatusCode = 401, error = "status 401" };
		ListenPlayerProbe.Interpret(result);
		Assert.IsFalse(result.success);
		Assert.AreEqual(ListenPlayerProbeResult.STAGE_CONNECT, result.failedStage);
		StringAssert.Contains("HTTP 401", result.message);
		StringAssert.Contains("access token was not accepted", result.message);
		StringAssert.Contains("/api/auth/tokens/refresh-token", result.message);
	}

	[Test]
	public void Interpret_ConnectTimeout_SaysWebSocketsMayBeBlocked()
	{
		var result = new ListenPlayerProbeResult { socketUri = "wss://socket.beamable.com", connectTimedOut = true, connectTimeoutSeconds = 15 };
		ListenPlayerProbe.Interpret(result);
		Assert.IsFalse(result.success);
		Assert.AreEqual(ListenPlayerProbeResult.STAGE_CONNECT, result.failedStage);
		StringAssert.Contains("15 seconds", result.message);
		StringAssert.Contains("WebSockets may be blocked", result.message);
	}

	[Test]
	public void Interpret_OtherHandshakeStatus_ReportsIt()
	{
		var result = new ListenPlayerProbeResult { socketUri = "wss://socket.beamable.com", handshakeStatusCode = 502 };
		ListenPlayerProbe.Interpret(result);
		Assert.IsFalse(result.success);
		StringAssert.Contains("HTTP 502", result.message);
	}

	[Test]
	public void Interpret_ServerCloseDuringWindow_FailsWithCloseInfo()
	{
		var result = new ListenPlayerProbeResult
		{
			socketUri = "wss://socket.beamable.com",
			handshakeStatusCode = 101,
			opened = true,
			sessionStartSent = true,
			closedByServer = true,
			closedAfterMs = 120,
			closeStatusCode = 1008,
			closeStatusDescription = "bad session-start"
		};
		ListenPlayerProbe.Interpret(result);
		Assert.IsFalse(result.success);
		Assert.AreEqual(ListenPlayerProbeResult.STAGE_LISTEN, result.failedStage);
		StringAssert.Contains("1008", result.message);
		StringAssert.Contains("bad session-start", result.message);
		StringAssert.Contains("120 ms", result.message);
	}

	[Test]
	public void Interpret_OpenedAndStillOpen_Succeeds()
	{
		var result = new ListenPlayerProbeResult
		{
			socketUri = "wss://socket.beamable.com",
			handshakeStatusCode = 101,
			opened = true,
			timeToOpenMs = 42,
			sessionStartSent = true,
			listenSeconds = 5,
			framesReceived = 2
		};
		ListenPlayerProbe.Interpret(result);
		Assert.IsTrue(result.success);
		Assert.AreEqual(string.Empty, result.failedStage);
		StringAssert.Contains("42 ms", result.message);
		StringAssert.Contains("2 frame(s)", result.message);
	}

	[Test]
	public async Task RunSocketProbe_SendsSessionStartFirst_AndRecordsFrames()
	{
		string firstMessage = null;
		string query = null;
		using var server = new LocalSocketServer(async (context, token) =>
		{
			query = context.Request.Url?.Query;
			var socketContext = await context.AcceptWebSocketAsync(null);
			var ws = socketContext.WebSocket;
			firstMessage = await ReceiveText(ws, token);
			await ws.SendAsync(Encoding.UTF8.GetBytes("{\"context\":\"hello\"}"), WebSocketMessageType.Text, true, token);
			// keep the socket open until the client closes it
			await ReceiveText(ws, token);
		});

		var result = new ListenPlayerProbeResult();
		await ListenPlayerProbe.RunSocketProbe(result, server.SocketUri, "a.b.c",
			TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), CancellationToken.None);
		ListenPlayerProbe.Interpret(result);

		Assert.IsTrue(result.success, result.message);
		Assert.AreEqual(101, result.handshakeStatusCode);
		Assert.IsTrue(result.opened);
		Assert.IsTrue(result.sessionStartSent);
		Assert.IsFalse(result.closedByServer);
		Assert.AreEqual(1, result.framesReceived);
		CollectionAssert.AreEqual(new[] { "{\"context\":\"hello\"}" }, result.frames);
		Assert.AreEqual("?access_token=a.b.c&send-session-start=true", query);
		Assert.AreEqual(ListenPlayerProbe.BuildSessionStartFrame(), firstMessage);
	}

	[Test]
	public async Task RunSocketProbe_ServerClose_IsReported()
	{
		using var server = new LocalSocketServer(async (context, token) =>
		{
			var socketContext = await context.AcceptWebSocketAsync(null);
			var ws = socketContext.WebSocket;
			await ReceiveText(ws, token);
			await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "bad session-start", token);
		});

		var result = new ListenPlayerProbeResult();
		await ListenPlayerProbe.RunSocketProbe(result, server.SocketUri, "a.b.c",
			TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), CancellationToken.None);
		ListenPlayerProbe.Interpret(result);

		Assert.IsFalse(result.success);
		Assert.IsTrue(result.closedByServer);
		Assert.AreEqual((int)WebSocketCloseStatus.PolicyViolation, result.closeStatusCode);
		Assert.AreEqual("bad session-start", result.closeStatusDescription);
		Assert.AreEqual(ListenPlayerProbeResult.STAGE_LISTEN, result.failedStage);
	}

	[Test]
	public async Task RunSocketProbe_RejectedUpgrade_ReportsHttpStatus()
	{
		using var server = new LocalSocketServer((context, token) =>
		{
			context.Response.StatusCode = 401;
			context.Response.Close();
			return Task.CompletedTask;
		});

		var result = new ListenPlayerProbeResult();
		await ListenPlayerProbe.RunSocketProbe(result, server.SocketUri, "opaque-token",
			TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), CancellationToken.None);
		ListenPlayerProbe.Interpret(result);

		Assert.IsFalse(result.success);
		Assert.IsFalse(result.opened);
		Assert.AreEqual(401, result.handshakeStatusCode);
		StringAssert.Contains("access token was not accepted", result.message);
	}

	[Test]
	public async Task RunSocketProbe_NoHandshakeResponse_TimesOut()
	{
		// accepts the TCP connection but never answers the upgrade, like a proxy that swallows WebSockets
		var tcp = new TcpListener(IPAddress.Loopback, 0);
		tcp.Start();
		try
		{
			var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
			var acceptTask = tcp.AcceptTcpClientAsync();

			var result = new ListenPlayerProbeResult();
			await ListenPlayerProbe.RunSocketProbe(result, $"ws://127.0.0.1:{port}", "a.b.c",
				TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), CancellationToken.None);
			ListenPlayerProbe.Interpret(result);

			Assert.IsFalse(result.success);
			Assert.IsTrue(result.connectTimedOut);
			StringAssert.Contains("WebSockets may be blocked", result.message);

			if (acceptTask.IsCompletedSuccessfully)
			{
				acceptTask.Result.Dispose();
			}
		}
		finally
		{
			tcp.Stop();
		}
	}

	static async Task<string> ReceiveText(WebSocket ws, CancellationToken token)
	{
		var buffer = new byte[8192];
		var builder = new StringBuilder();
		WebSocketReceiveResult received;
		do
		{
			received = await ws.ReceiveAsync(buffer, token);
			if (received.MessageType == WebSocketMessageType.Close)
			{
				if (ws.State == WebSocketState.CloseReceived)
				{
					await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, token);
				}
				return null;
			}
			builder.Append(Encoding.UTF8.GetString(buffer, 0, received.Count));
		} while (!received.EndOfMessage);
		return builder.ToString();
	}

	/// <summary>
	/// An <see cref="HttpListener"/> on a free localhost port that hands each request to a handler.
	/// </summary>
	class LocalSocketServer : IDisposable
	{
		readonly HttpListener _listener = new HttpListener();
		readonly CancellationTokenSource _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		readonly Task _loop;

		public string SocketUri { get; }

		public LocalSocketServer(Func<HttpListenerContext, CancellationToken, Task> handler)
		{
			var port = GetFreePort();
			_listener.Prefixes.Add($"http://127.0.0.1:{port}/");
			_listener.Start();
			SocketUri = $"ws://127.0.0.1:{port}";
			_loop = Task.Run(async () =>
			{
				while (!_cts.IsCancellationRequested)
				{
					HttpListenerContext context;
					try
					{
						context = await _listener.GetContextAsync();
					}
					catch
					{
						return;
					}
					try
					{
						await handler(context, _cts.Token);
					}
					catch
					{
						// the client going away mid-handler is fine in these tests
					}
				}
			});
		}

		static int GetFreePort()
		{
			var probe = new TcpListener(IPAddress.Loopback, 0);
			probe.Start();
			var port = ((IPEndPoint)probe.LocalEndpoint).Port;
			probe.Stop();
			return port;
		}

		public void Dispose()
		{
			_cts.Cancel();
			try
			{
				_listener.Stop();
				_listener.Close();
			}
			catch
			{
				// already stopped
			}
		}
	}
}
