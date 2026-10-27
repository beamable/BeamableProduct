using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Server.Common;
using cli.CliServerCommand;
using cli.Services.CliInvocation;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace cli.Services.HttpServer;


[Serializable]
public class ServerRequest
{
	public string commandLine;
}

[Serializable]
public class ServerInfoResponse
{
	/// <summary>
	/// the nuget id of the code executing the server
	/// </summary>
	public string version;
	
	/// <summary>
	/// a server must be created with an "owner", some form of identification
	/// that can be used to determine if the server is operating for a Unity-client, or
	/// some other Unity-client, or a user mode.
	/// </summary>
	public string owner;

	/// <summary>
	/// the number of requests the CLI server thinks are being handled. When this number
	/// is positive, it will prevent the TTL from self-destructing the server.
	/// </summary>
	public long inflightRequests;

	/// <summary>
	/// the process id of the server
	/// </summary>
	public int pid;

	/// <summary>
	/// The cli invocations currently inflight. Only listed for requests that carry the session token, because the
	/// command lines can hold credentials.
	/// </summary>
	public List<string> inflightCommands = new List<string>();

	/// <summary>
	/// Optional protocol features this server supports, so clients can detect them instead of comparing versions.
	/// "auth": /execute requires the session token as <c>Authorization: Bearer</c>.
	/// "mcp": an MCP Streamable HTTP endpoint is served at <see cref="mcpPath"/>, with the same token.
	/// </summary>
	public List<string> features = new List<string>();

	/// <summary>The path of the MCP endpoint, when the "mcp" feature is on.</summary>
	public string mcpPath;
}

[Serializable]
public class ServerMessageResponse
{
	public string message;
}

[Serializable]
public class ServerErrorResponse
{
	public string type;
	public string stack;
	public string message;
}

[Serializable]
public class ServerAvailableData
{
	public int port;
	public string uri;
}

public class ServerService
{
	private HttpListener _listener;
	private DateTimeOffset _selfDestructAt;
	private ulong _inflightRequests;

	private const string INFO_ROUTE = "info";
	private const string EXEC_ROUTE = "execute";

	public ServerService()
	{
		
	}

	public async Task RunServer(ServeCliCommandArgs args, Action<ServerAvailableData> onReady=null)
	{
		if (_listener != null) throw new InvalidOperationException("already running");

		// Any local process or web page can reach 127.0.0.1, so commands require this run's secret.
		if (string.IsNullOrEmpty(args.token))
		{
			args.token = LocalServerGuard.NewToken();
		}

		string uri = null;

		var started = false;
		do
		{
			try
			{
				_listener = new HttpListener();
				uri = $"http://127.0.0.1:{args.port}/";
				
				// it is important not to listen on ALL interfaces, because this server should only be accessible 
				//  from the machine itself. It would be bad if another machine could start triggering CLI commands
				//  because the server was public. 
				// _listener.Prefixes.Add($"http://localhost:{args.port}/");
				_listener.Prefixes.Add($"http://127.0.0.1:{args.port}/");
				_listener.Start();
				started = true;
			}
			catch (Exception ex)
			{
				Log.Information($"failed to start server at uri=[{uri}]. message=[{ex.Message}]");
			}

			if (!started && args.incPortUntilSuccess)
			{
				Log.Information("incrementing port...");
				args.port++;
				if (args.port is < DEFAULT_PORT or > MAX_PORT)
					throw new CliException($"Port must be between {DEFAULT_PORT} and {MAX_PORT}");

			}
		} while (!started && args.incPortUntilSuccess);

		onReady?.Invoke(new ServerAvailableData
		{
			port = args.port,
			uri = uri
		});
		
		Log.Information("Cli available at " + uri);

		var execString = @$" use /{EXEC_ROUTE} with a body of {{""commandLine"": ""config""}} to run commands. 
 the 'commandLine' string will be parsed as though the command was being passed directly to the BEAM cli.";
		var infoString = @$" use /{INFO_ROUTE} with no body to receive metadata about the cli server.";
		Log.Information(execString);
		Log.Information(infoString);
		_selfDestructAt = DateTimeOffset.Now + TimeSpan.FromSeconds(args.selfDestructTimeSeconds);

		var contexts = new List<HttpListenerContext>();
		var contextLock = new object();

		var keepAlive = true;
		var scheduler = new TaskFactory(TaskCreationOptions.PreferFairness, TaskContinuationOptions.PreferFairness);
		if (args.selfDestructTimeSeconds > 0)
		{
			var selfDestruct = scheduler.StartNew(async () =>
			{
				
			// })
			// var selfDestruct = Task.Run(async () =>
			// {
				try
				{
					while (keepAlive)
					{
						var untilDoom = _selfDestructAt - DateTimeOffset.Now;
						await Task.Delay(untilDoom);

						if (Interlocked.Read(ref _inflightRequests) > 0)
						{
							_selfDestructAt = DateTimeOffset.Now + TimeSpan.FromSeconds(args.selfDestructTimeSeconds);
							continue;
						}

						if (DateTimeOffset.Now >= _selfDestructAt)
						{
							
							keepAlive = false; // signal the program to stop accepting new requests.
							Log.Information("auto self-destruct started.");
						
							
							lock (contextLock)
							{
								foreach (var ctx in contexts.ToList())
								{
									// force-close all the ongoing requests...
									Log.Debug("cancelling " + ctx.Request.Url.ToString());
									ctx.Response.Close();
								}
							}
							
							_listener.Stop();

							Log.Debug("entering 1 second grace period...");
							await Task.Delay(1000);

							Log.Debug("exiting 1 second grace period...");
							lock (contextLock)
							{
								var remainingContexts = contexts.Count > 0;
								if (remainingContexts)
								{
									Log.Error("self-destruct still has open requests. These requests may not terminate correctly.");
								}
							}
							Log.Information("auto self-destruct finished.");
							Environment.Exit(0); 
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error($"self destruct monitor failed. message=[{ex.Message}] type=[{ex.GetType().Name}] stack=[{ex.StackTrace}]");
				}
			});
		}

		while (keepAlive)
		{
			HttpListenerContext ctx;
			try
			{
				ctx = await _listener.GetContextAsync();
				lock (contextLock)
				{
					contexts.Add(ctx);
				}
			}
			catch (ObjectDisposedException) when (!keepAlive)
			{
				Log.Debug($"exiting receive loop because {nameof(ObjectDisposedException)}");
				break;
			}
			catch (HttpListenerException) when (!keepAlive)
			{
				Log.Debug($"exiting receive loop because {nameof(HttpListenerException)}");
				break;
			}
			Interlocked.Increment(ref _inflightRequests);
			Log.Trace($"Starting request. inflight=[{Interlocked.Read(ref _inflightRequests)}]");

			var _ = scheduler.StartNew(async () =>
			{
				
			// })
			// var _ = Task.Run(async () =>
			// {
				try
				{
					await HandleRequest(args, ctx, Interlocked.Read(ref _inflightRequests));
				}
				catch (Exception ex)
				{
					Log.Error(
						$"error occured while handling cli request. type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}]");
					args.Provider.GetService<IDataReporterService>().Exception(ex, 0, "");
				}
				finally
				{
					lock(contextLock)
					{
						contexts.Remove(ctx);
					}

					_selfDestructAt = DateTimeOffset.Now + TimeSpan.FromSeconds(args.selfDestructTimeSeconds);
					Interlocked.Decrement(ref _inflightRequests);
					Log.Trace($"Finishing request. inflight=[{Interlocked.Read(ref _inflightRequests)}]");
				}
			});
		}
		Log.Information("Shutting down...");
		
	}

	static async Task HandleRequest(ServeCliCommandArgs args, HttpListenerContext ctx,
		ulong inflightRequests, App app = null)
	{
		// Peel out the requests and response objects
		HttpListenerRequest req = ctx.Request;
		HttpListenerResponse resp = ctx.Response;

		// http://base:port/route
		var routePath = req.Url!.AbsolutePath.Trim('/');
		Log.Trace("got message at route: " + routePath);
		try
		{
			var refusal = Authorize(args, req, routePath);
			if (refusal != null)
			{
				Log.Information($"Refused request route=[{routePath}] status=[{refusal.Value.status}] reason=[{refusal.Value.error.message}]");
				await WriteJson(resp, refusal.Value.status, refusal.Value.error);
				return;
			}

			switch (routePath)
			{
				case INFO_ROUTE:
					var info = await HandleInfo(args, inflightRequests);
					if (!HasToken(args, req))
					{
						info.inflightCommands.Clear();
					}
					await WriteJson(resp, 200, info);
					break;
				case EXEC_ROUTE:
					await HandleExec(args, req.InputStream, resp, app);
					break;
				case var _ when args.mcpEndpoint != null && ServerCardEndpoint.Owns(routePath):
					await ServerCardEndpoint.Handle(req, resp, routePath,
						VersionService.GetNugetPackagesForExecutingCliVersion().ToString());
					break;
				case var _ when args.mcpEndpoint != null && McpHttpEndpoint.Owns(routePath):
					await args.mcpEndpoint.HandleAsync(ctx);
					break;
				default:
					Log.Information($"Unknown route=[{routePath}]");
					await WriteJson(resp, 400, new ServerErrorResponse
					{
						message = $"unknown route=[{routePath}]", type = "UnhandledRoute"
					});
					break;
			}
		}
		catch (Exception ex) when (routePath == EXEC_ROUTE)
		{
			// HandleExec reports setup/execution errors in-band. Anything escaping that boundary
			// is a response/cleanup failure: never append raw JSON or reclassify a completed command.
			Log.Error(ex, "CLI execution response or cleanup failed");
		}
		catch (Exception ex)
		{
			Log.Error($"Message failed msg=[{ex.Message}]");
			try
			{
				await WriteJson(resp, 500, new ServerErrorResponse
				{
					message = ex.Message, type = ex.GetType().Name, stack = ex.StackTrace
				});
			}
			catch (Exception writeEx)
			{
				// The response may already be partly sent; there is nothing more to tell the client.
				Log.Error($"Could not report failure to client msg=[{writeEx.Message}]");
			}
		}
		finally
		{
			resp.Close();
		}

	}

	/// <summary>
	/// The status and error to refuse a request with, or null to serve it. The server runs commands with this
	/// CLI's credentials, so it must not be drivable by a web page (no Origin, Host must be 127.0.0.1, JSON only,
	/// which a cross-site form or no-cors fetch cannot send) or by another local process (session token).
	/// </summary>
	static (int status, ServerErrorResponse error)? Authorize(ServeCliCommandArgs args, HttpListenerRequest req, string routePath)
	{
		static (int, ServerErrorResponse) Refuse(int status, string type, string message) =>
			(status, new ServerErrorResponse { type = type, message = message });

		if (!LocalServerGuard.IsLoopbackHost(req.Headers["Host"], req.LocalEndPoint.Port))
		{
			return Refuse(403, "ForbiddenHost", "Address the CLI server by 127.0.0.1.");
		}

		if (args.mcpEndpoint != null && ServerCardEndpoint.Owns(routePath))
		{
			// Public, read-only discovery metadata that any page may read (the Server Card spec requires CORS *).
			return req.HttpMethod is "GET" or "HEAD" or "OPTIONS"
				? null
				: Refuse(405, "MethodNotAllowed", "Discovery documents are read-only.");
		}

		if (!string.IsNullOrEmpty(req.Headers["Origin"]))
		{
			return Refuse(403, "ForbiddenOrigin", "The CLI server does not accept requests from web pages.");
		}

		if (args.mcpEndpoint != null && McpHttpEndpoint.Owns(routePath))
		{
			// The MCP handler validates methods, media types and protocol versions itself.
			return HasToken(args, req) ? null : Refuse(401, "Unauthorized", TokenHint);
		}

		if (routePath != EXEC_ROUTE)
		{
			return null;
		}

		if (req.HttpMethod != "POST")
		{
			return Refuse(405, "MethodNotAllowed", $"/{EXEC_ROUTE} only accepts POST.");
		}

		var mediaType = req.ContentType?.Split(';')[0].Trim();
		if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
		{
			return Refuse(415, "UnsupportedMediaType", $"/{EXEC_ROUTE} only accepts application/json.");
		}

		if (!string.IsNullOrEmpty(args.token) && !HasToken(args, req))
		{
			return Refuse(401, "Unauthorized", TokenHint);
		}

		return null;
	}

	const string TokenHint = "Send the server's session token as 'Authorization: Bearer <token>'. It is printed when the server " +
	                         "starts and stored in .beamable/" + ConfigService.TEMP_FOLDER_NAME + "/" + ServerLocatorFile.FOLDER_NAME +
	                         "/<pid>.json; `beam mcp token` prints it."; 

	static bool HasToken(ServeCliCommandArgs args, HttpListenerRequest req) =>
		string.IsNullOrEmpty(args.token) || LocalServerGuard.HasBearerToken(req.Headers["Authorization"], args.token);

	static async Task WriteJson(HttpListenerResponse resp, int status, object body)
	{
		// The status line goes out with the first body byte, so set it first.
		resp.StatusCode = status;
		resp.ContentType = "application/json; charset=utf-8";
		var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
		await resp.OutputStream.WriteAsync(data, 0, data.Length);
	}

	static Task<ServerInfoResponse> HandleInfo(ServeCliCommandArgs args, ulong inflightRequests)
	{
		var version = VersionService.GetNugetPackagesForExecutingCliVersion();

		return Task.FromResult(new ServerInfoResponse
		{
			version = version.ToString(),
			owner = args.owner,
			inflightRequests = (long)inflightRequests,
			pid = Environment.ProcessId,
			inflightCommands = CliInvocationCore.Snapshot(),
			features = args.mcpEndpoint == null
				? new List<string> { FEATURE_AUTH }
				: new List<string> { FEATURE_AUTH, FEATURE_MCP },
			mcpPath = args.mcpEndpoint == null ? null : McpHttpEndpoint.PATH
		});
	}

	public const string FEATURE_AUTH = "auth";
	public const string FEATURE_MCP = "mcp";

	// The registry lives in CliInvocationCore, shared with every transport; this alias keeps the name
	// diagnostics and tests have always used for /execute's in-flight commands.
	private static readonly List<string> cliInvocations = CliInvocationCore.Invocations;

	static async Task HandleExec(ServeCliCommandArgs args, Stream networkRequestStream, HttpListenerResponse response, App app)
	{
		using var inputStream = new StreamReader(networkRequestStream);
		response.Headers.Set(HttpResponseHeader.ContentType, "text/event-stream; charset=utf-8");
		var recording = string.IsNullOrEmpty(args.recordTo) ? null : new ExecuteRecording();
		ServerReporterService reporter = null;
		try
		{
			await CliInvocationCore.Execute(new CliInvocationRequest
			{
				App = app,
				UseCustomSplitter = args.useCustomSplitter,
				ReadCommand = async () =>
				{
					var input = await inputStream.ReadToEndAsync();
					Log.Trace("Raw input received: " + input);
					recording?.SetRequest(input);
					// /info lists the raw request bodies, as it always has.
					return (JsonConvert.DeserializeObject<ServerRequest>(input)?.commandLine, input);
				},
				CreateReporter = provider => reporter = new ServerReporterService(provider, response) { OnEvent = recording == null ? null : recording.Add },
				ReportSetupFailure = async report =>
				{
					// Setup failed before a reporter (and its heartbeat) existed. Unity still needs
					// a ReportDataPoint<ErrorOutput>, not ServerErrorResponse or an HTTP-only error.
					var json = JsonConvert.SerializeObject(report, UnitySerializationSettings.Instance);
					recording?.Add(json);
					var bytes = Encoding.UTF8.GetBytes("data: " + json + Environment.NewLine);
					await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
				}
			});
		}
		finally
		{
			// The command is over; its heartbeat must not outlive it (see ServerReporterService.Complete).
			reporter?.Complete();
		}
		recording?.Save(args.recordTo);
	}

	public const int DEFAULT_PORT = CliServerPorts.RANGE_START;
	public const int MAX_PORT = CliServerPorts.RANGE_END;
}

public class ServerReporterService : IDataReporterService
{
	/// <summary>Buffered events are sent as soon as this many bytes are waiting.</summary>
	public const int FLUSH_THRESHOLD_BYTES = 16 * 1024;

	/// <summary>
	/// The longest an event waits in the buffer. Sending every event on its own costs a write and a chunk each,
	/// which dominated large streams; a few milliseconds is invisible to the editor.
	/// </summary>
	public static readonly TimeSpan MaxFlushDelay = TimeSpan.FromMilliseconds(2);

	// Built once: JsonConvert.SerializeObject creates a serializer from the settings on every call.
	private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault(UnitySerializationSettings.Instance);
	private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
	private static readonly byte[] DataPrefix = Utf8.GetBytes("data: ");
	private static readonly byte[] LineEnd = Utf8.GetBytes(Environment.NewLine);
	private static readonly byte[] Heartbeat = Utf8.GetBytes("\u200b"); // a zero-width character that tests the connection

	private readonly HttpListenerResponse _resp;
	private readonly Stream _output;
	private IDependencyProvider _provider;
	private Task _connectionMonitorTask;
	private AppLifecycle _lifecycle;
	private readonly CancellationTokenSource _completed = new CancellationTokenSource();

	// Everything below is guarded by _lock: the events waiting to be sent, and the writer that encodes into them.
	private readonly object _lock = new object();
	private readonly MemoryStream _pending = new MemoryStream();
	private StreamWriter _pendingWriter;
	private readonly Timer _flushTimer;
	private bool _flushScheduled;
	private bool _isComplete;

	/// <summary>Sees every event's JSON as it is sent (heartbeats excluded).</summary>
	public Action<string> OnEvent { get; set; }

	public ServerReporterService(IDependencyProvider provider, HttpListenerResponse resp)
	{
		_provider = provider;
		_resp = resp;
		_output = _resp.OutputStream;
		_pendingWriter = CreatePendingWriter();
		_flushTimer = new Timer(_ => FlushFromTimer(), null, Timeout.Infinite, Timeout.Infinite);
		_lifecycle = _provider.GetService<AppLifecycle>();
		_connectionMonitorTask = Task.Run(async () =>
		{
			try
			{
				while (!_lifecycle.IsCancelled && !_completed.IsCancellationRequested)
				{
					await Task.Delay(1000, _completed.Token);

					// check if the pipe is still open, because if it isn't, the invocation should cancel.
					try
					{
						lock (_lock)
						{
							FlushPendingLocked();
							_output.Write(Heartbeat, 0, Heartbeat.Length);
							_output.Flush();
						}

					}
					catch (HttpListenerException)
					{
						// the pipe is broken, so can assume the client is no longer connected, and we can cancel this invocation.
						_lifecycle.Cancel();
						App.GlobalLogger?.LogTrace("monitor found that client is no longer connected; cancelling app lifecycle.");
						break;
					}
				}
			}
			catch (OperationCanceledException) when (_completed.IsCancellationRequested)
			{
				// The command finished.
			}
			catch (Exception ex)
			{
				// remember, async void methods don't report exceptions automatically, so always log it to save brain later.
				App.GlobalLogger?.ZLogError($"cli-server reporter service monitor task failed! type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}]");
			}
		});
	} 

	/// <summary>
	/// Sends whatever is still buffered and stops the connection heartbeat once the command has finished. Writing to
	/// a closed response does not always fail, so without this the heartbeat would keep running, and keep this
	/// command's services alive, forever.
	/// </summary>
	public void Complete()
	{
		try
		{
			lock (_lock)
			{
				_isComplete = true;
				FlushPendingLocked();
			}
		}
		catch (Exception ex)
		{
			App.GlobalLogger?.LogTrace($"could not send the last events. type=[{ex.GetType().Name}] message=[{ex.Message}]");
		}
		finally
		{
			_flushTimer.Dispose();
			_completed.Cancel();
		}
	}
	
	public void Report(string rawMessage)
	{
		OnEvent?.Invoke(rawMessage);
		var bytes = Utf8.GetBytes(rawMessage);
		lock (_lock)
		{
			_pending.Write(DataPrefix, 0, DataPrefix.Length);
			_pending.Write(bytes, 0, bytes.Length);
			_pending.Write(LineEnd, 0, LineEnd.Length);
			ScheduleFlushLocked();
		}
	}
	
	public void Report<T>(string type, T data)
	{
		var pt = new ReportDataPoint<T>
		{
			data = data,
			type = type,
			ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};

		if (OnEvent != null)
		{
			// A recording needs the event's text anyway.
			Report(JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance));
			return;
		}

		lock (_lock)
		{
			var start = _pending.Length;
			try
			{
				_pending.Write(DataPrefix, 0, DataPrefix.Length);
				// The same writer setup as JsonConvert.SerializeObject, so the bytes are identical.
				var json = new JsonTextWriter(_pendingWriter) { Formatting = Serializer.Formatting, CloseOutput = false };
				Serializer.Serialize(json, pt);
				json.Flush();
				_pending.Write(LineEnd, 0, LineEnd.Length);
			}
			catch
			{
				// Drop the half-written event, including anything the writer still holds.
				_pending.SetLength(start);
				_pendingWriter = CreatePendingWriter();
				throw;
			}
			ScheduleFlushLocked();
		}
	}

	private StreamWriter CreatePendingWriter() => new StreamWriter(_pending, Utf8, 1024, leaveOpen: true);

	private void ScheduleFlushLocked()
	{
		if (_isComplete || _pending.Length >= FLUSH_THRESHOLD_BYTES)
		{
			FlushPendingLocked();
			return;
		}

		if (!_flushScheduled)
		{
			_flushScheduled = true;
			_flushTimer.Change(MaxFlushDelay, Timeout.InfiniteTimeSpan);
		}
	}

	private void FlushFromTimer()
	{
		try
		{
			lock (_lock)
			{
				FlushPendingLocked();
			}
		}
		catch (Exception ex)
		{
			// Nobody awaits the timer; a closed response just means there is no one left to send to.
			App.GlobalLogger?.LogTrace($"could not send buffered events. type=[{ex.GetType().Name}] message=[{ex.Message}]");
		}
	}

	private void FlushPendingLocked()
	{
		_flushScheduled = false;
		if (_pending.Length == 0)
		{
			return;
		}

		try
		{
			_output.Write(_pending.GetBuffer(), 0, (int)_pending.Length);
			_output.Flush();
		}
		catch (HttpListenerException ex)
		{
			// the pipe is broken, so can assume the client is no longer connected, and we can cancel this invocation.
			_lifecycle.Cancel();
			App.GlobalLogger?.LogTrace("client is no longer connected; cancelling app lifecycle." + ex.Message);
		}
		finally
		{
			_pending.SetLength(0);
		}
	}
}
