using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace cli.Notifications;

/// <summary>
/// The outcome of <c>beam listen player --probe</c>: one realtime session opened the same way the Web SDK opens it.
/// </summary>
[CliContractType, Serializable]
public class ListenPlayerProbeResult
{
	public const string STAGE_REALM_CONFIG = "realm-config";
	public const string STAGE_TOKEN = "token";
	public const string STAGE_CONNECT = "connect";
	public const string STAGE_SESSION_START = "session-start";
	public const string STAGE_LISTEN = "listen";

	/// <summary>True when the socket opened, the session-start frame was sent, and the server did not close the socket during the listen window.</summary>
	public bool success;
	/// <summary>A human readable summary. On failure, it says what to try next.</summary>
	public string message;
	/// <summary>Empty on success, otherwise one of "realm-config", "token", "connect", "session-start" or "listen".</summary>
	public string failedStage;
	/// <summary>"guest" for a newly created guest player, or "current" for the logged-in identity (or --refresh-token).</summary>
	public string identity;
	/// <summary>The realm's websocket provider from the client defaults.</summary>
	public string provider;
	/// <summary>The realm's websocket URI from the client defaults. The access token is never included.</summary>
	public string socketUri;
	/// <summary>The HTTP status of the websocket upgrade: 101 when accepted, the rejection status (such as 401) otherwise, or 0 when no response was read.</summary>
	public int handshakeStatusCode;
	public bool opened;
	/// <summary>True when the socket was still connecting after the connect timeout.</summary>
	public bool connectTimedOut;
	public double connectTimeoutSeconds;
	/// <summary>Milliseconds from starting the connection until the socket opened.</summary>
	public long timeToOpenMs;
	public bool sessionStartSent;
	/// <summary>The session-start frame sent as the first message.</summary>
	public string sessionStartFrame;
	public double listenSeconds;
	/// <summary>The number of frames received during the listen window.</summary>
	public int framesReceived;
	/// <summary>The first few frames received, each truncated.</summary>
	public List<string> frames = new List<string>();
	/// <summary>True when the server closed (or dropped) the socket during the listen window.</summary>
	public bool closedByServer;
	/// <summary>Milliseconds after opening when the server closed the socket.</summary>
	public long closedAfterMs;
	/// <summary>The websocket close status code sent by the server, or 0.</summary>
	public int closeStatusCode;
	public string closeStatusDescription;
	/// <summary>The underlying error, if any.</summary>
	public string error;
}

/// <summary>
/// The pieces of <c>beam listen player --probe</c> that don't need the Beamable backend: building the connect URL
/// and session-start frame, running the socket part of the probe, and turning its outcome into a verdict.
/// </summary>
public static class ListenPlayerProbe
{
	public const int DEFAULT_PROBE_SECONDS = 5;
	public const int CONNECT_TIMEOUT_SECONDS = 15;
	public const int MAX_REPORTED_FRAMES = 5;
	public const int MAX_FRAME_LENGTH = 500;
	public const string SESSION_START_PLATFORM = "CLI";
	public const string SESSION_START_MODEL = "Desktop";

	static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Returns the error message for a realm whose websocket provider the Beamable socket can't serve, or null when the provider is supported.
	/// </summary>
	public static string GetUnsupportedProviderMessage(string provider)
	{
		if (!string.Equals(provider, "pubnub", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return $@"Only realms with beam notifications are supported. This realm currently has {provider}.
Try setting the realm config to beam with this command,
""beam config realm set --key-values 'notification|publisher::beamable'""";
	}

	/// <summary>
	/// Builds the URL the Web SDK connects to: <c>{uri}/connect?access_token=...&amp;send-session-start=true</c>.
	/// </summary>
	public static string BuildConnectUrl(string socketUri, string accessToken)
	{
		var baseUri = (socketUri ?? string.Empty).TrimEnd('/');
		return $"{baseUri}/connect?access_token={Uri.EscapeDataString(accessToken ?? string.Empty)}&send-session-start=true";
	}

	/// <summary>
	/// Builds the session-start frame, mirroring <c>buildSessionStartFrame</c> in the Web SDK's BeamWebSocket.ts.
	/// </summary>
	public static string BuildSessionStartFrame(string platform = SESSION_START_PLATFORM, string model = SESSION_START_MODEL)
	{
		return JsonConvert.SerializeObject(new
		{
			type = "session-start",
			device = new { platform, model }
		});
	}

	public static string TruncateFrame(string frame)
	{
		if (frame == null || frame.Length <= MAX_FRAME_LENGTH)
		{
			return frame;
		}
		return frame.Substring(0, MAX_FRAME_LENGTH) + $"... ({frame.Length} chars)";
	}

	/// <summary>
	/// Finds the HTTP status carried by a Beamable request error, or 0.
	/// </summary>
	public static long FindRequestStatus(Exception ex)
	{
		for (var e = ex; e != null; e = e.InnerException)
		{
			if (e is IRequestErrorWithStatus withStatus)
			{
				return withStatus.Status;
			}
		}
		return 0;
	}

	/// <summary>
	/// Connects to <paramref name="socketUri"/>, sends the session-start frame, listens for <paramref name="listenWindow"/>,
	/// and records what happened on <paramref name="result"/>. It does not decide success; see <see cref="Interpret"/>.
	/// </summary>
	public static async Task RunSocketProbe(ListenPlayerProbeResult result, string socketUri, string accessToken,
		TimeSpan connectTimeout, TimeSpan listenWindow, CancellationToken token)
	{
		result.socketUri = socketUri;
		result.connectTimeoutSeconds = connectTimeout.TotalSeconds;
		result.listenSeconds = listenWindow.TotalSeconds;
		result.sessionStartFrame = BuildSessionStartFrame();

		using var ws = new ClientWebSocket();
		ws.Options.CollectHttpResponseDetails = true;

		var connectWatch = Stopwatch.StartNew();
		using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token))
		{
			connectCts.CancelAfter(connectTimeout);
			try
			{
				await ws.ConnectAsync(new Uri(BuildConnectUrl(socketUri, accessToken)), connectCts.Token);
				result.opened = true;
				result.timeToOpenMs = connectWatch.ElapsedMilliseconds;
			}
			catch (Exception ex) when (connectCts.IsCancellationRequested && !token.IsCancellationRequested)
			{
				result.connectTimedOut = true;
				result.error = ex.Message;
			}
			catch (Exception ex) when (!token.IsCancellationRequested)
			{
				result.error = DescribeException(ex);
			}
		}

		result.handshakeStatusCode = (int)ws.HttpStatusCode;
		if (!result.opened)
		{
			return;
		}

		try
		{
			var frame = Encoding.UTF8.GetBytes(result.sessionStartFrame);
			await ws.SendAsync(frame, WebSocketMessageType.Text, true, token);
			result.sessionStartSent = true;
		}
		catch (Exception ex) when (!token.IsCancellationRequested)
		{
			result.error = DescribeException(ex);
			return;
		}

		var listenWatch = Stopwatch.StartNew();
		Task<(WebSocketMessageType type, string body)> pending = null;
		try
		{
			while (ws.State == WebSocketState.Open)
			{
				var remaining = listenWindow - listenWatch.Elapsed;
				if (remaining <= TimeSpan.Zero)
				{
					break;
				}

				pending ??= ReceiveFrame(ws, token);
				var done = await Task.WhenAny(pending, Task.Delay(remaining, token));
				if (done != pending)
				{
					break;
				}

				var (type, body) = await pending;
				pending = null;
				if (type == WebSocketMessageType.Close)
				{
					result.closedByServer = true;
					result.closedAfterMs = result.timeToOpenMs + listenWatch.ElapsedMilliseconds;
					result.closeStatusCode = (int)(ws.CloseStatus ?? 0);
					result.closeStatusDescription = ws.CloseStatusDescription;
					break;
				}

				result.framesReceived++;
				if (result.frames.Count < MAX_REPORTED_FRAMES)
				{
					result.frames.Add(TruncateFrame(body));
				}
			}
		}
		catch (Exception ex) when (!token.IsCancellationRequested)
		{
			// the connection dropped without a close handshake (the browser reports this as close code 1006)
			pending = null;
			result.closedByServer = true;
			result.closedAfterMs = result.timeToOpenMs + listenWatch.ElapsedMilliseconds;
			result.error = DescribeException(ex);
		}

		// a receive may still be in flight; observe it so a later failure isn't unobserved
		pending?.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);

		if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
		{
			try
			{
				using var closeCts = new CancellationTokenSource(CloseTimeout);
				await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "probe complete", closeCts.Token);
			}
			catch
			{
				// the probe is over; a failed close doesn't change the outcome
			}
		}
	}

	static async Task<(WebSocketMessageType, string)> ReceiveFrame(ClientWebSocket ws, CancellationToken token)
	{
		var buffer = new byte[8192];
		using var stream = new MemoryStream();
		WebSocketReceiveResult received;
		do
		{
			received = await ws.ReceiveAsync(buffer, token);
			stream.Write(buffer, 0, received.Count);
		} while (!received.EndOfMessage && received.MessageType != WebSocketMessageType.Close);

		return (received.MessageType, Encoding.UTF8.GetString(stream.ToArray()));
	}

	static string DescribeException(Exception ex)
	{
		return ex.InnerException == null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})";
	}

	static string DescribeHost(string socketUri)
	{
		return Uri.TryCreate(socketUri ?? string.Empty, UriKind.Absolute, out var uri) ? uri.Authority : (socketUri ?? "the realtime server");
	}

	/// <summary>
	/// Decides whether the socket part of the probe succeeded, and sets <see cref="ListenPlayerProbeResult.success"/>,
	/// <see cref="ListenPlayerProbeResult.failedStage"/> and <see cref="ListenPlayerProbeResult.message"/>.
	/// </summary>
	public static void Interpret(ListenPlayerProbeResult result)
	{
		var host = DescribeHost(result.socketUri);
		var status = result.handshakeStatusCode;
		result.success = false;

		if (!result.opened)
		{
			result.failedStage = ListenPlayerProbeResult.STAGE_CONNECT;
			if (result.connectTimedOut)
			{
				result.message = $"The realtime socket at {host} was still connecting after {result.connectTimeoutSeconds:0.#} seconds. " +
				                 "WebSockets may be blocked by a proxy or firewall.";
			}
			else if (status == 401 || status == 403)
			{
				result.message = $"The WebSocket handshake with {host} was rejected (HTTP {status}): the realtime access token was not accepted. " +
				                 "The socket expects the JWT from /api/auth/tokens/refresh-token for this realm, not the token from /basic/auth/token. " +
				                 "Check that the refresh token belongs to this cid and pid, or try --guest.";
			}
			else if (status != 0 && status != 101)
			{
				result.message = $"The WebSocket handshake with {host} failed (HTTP {status}). " +
				                 "If other requests to Beamable succeed, WebSockets may be blocked by a proxy or firewall.";
			}
			else
			{
				result.message = $"The WebSocket handshake with {host} failed: {result.error}. " +
				                 "WebSockets may be blocked by a proxy or firewall, or the server rejected the connection.";
			}
			return;
		}

		if (!result.sessionStartSent)
		{
			result.failedStage = ListenPlayerProbeResult.STAGE_SESSION_START;
			result.message = $"The realtime socket at {host} opened, but the session-start frame could not be sent: {result.error}.";
			return;
		}

		if (result.closedByServer)
		{
			result.failedStage = ListenPlayerProbeResult.STAGE_LISTEN;
			var close = result.closeStatusCode != 0
				? $"close status {result.closeStatusCode}{(string.IsNullOrEmpty(result.closeStatusDescription) ? "" : $" \"{result.closeStatusDescription}\"")}"
				: $"no close status{(string.IsNullOrEmpty(result.error) ? "" : $", {result.error}")}";
			result.message = $"The server closed the realtime socket at {host} {result.closedAfterMs} ms after it opened ({close}), " +
			                 $"after {result.framesReceived} frame(s). A close right after session-start usually means the session-start frame or the token was rejected.";
			return;
		}

		result.success = true;
		result.failedStage = string.Empty;
		result.message = $"The realtime socket at {host} opened in {result.timeToOpenMs} ms, the session-start frame was sent, " +
		                 $"and it was still open after {result.listenSeconds:0.#} seconds ({result.framesReceived} frame(s) received).";
	}
}
