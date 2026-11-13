using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Common.Util;
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
	public string requestId;
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
	/// The cli invocations currently inflight
	/// </summary>
	public List<string> inflightCommands = new List<string>();
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

		// http://base:port/
		var routePath = ctx.Request.Url.ToString().Substring(ctx.Request.Url.ToString().LastIndexOf('/') + 1);
		Log.Trace("got message at route: " + routePath);
		string response = null;
		int status = 200;
		byte[] data;
		try
		{
			switch (routePath)
			{
				case INFO_ROUTE:
					
					
					var info = await HandleInfo(args, inflightRequests);
					response = JsonConvert.SerializeObject(info);
					data = Encoding.UTF8.GetBytes(response);
					await resp.OutputStream.WriteAsync(data, 0, data.Length);
					resp.StatusCode = status;
					break;
				case "stop":
					await HandleStop(req.InputStream);
					resp.StatusCode = 204;
					break;
				case EXEC_ROUTE:
					await HandleExec(args, req.InputStream, resp, app);
					break;
				default:
					Log.Information($"Unknown route=[{routePath}]");
					response = JsonConvert.SerializeObject(new ServerErrorResponse
					{
						message = $"unknown route=[{routePath}]", type = "UnhandledRoute"
					});
					status = 400;
					data = Encoding.UTF8.GetBytes(response);
					await resp.OutputStream.WriteAsync(data, 0, data.Length);
					resp.StatusCode = status;
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
			response = JsonConvert.SerializeObject(new ServerErrorResponse
			{
				message = ex.Message, type = ex.GetType().Name, stack = ex.StackTrace
			});
			status = 500;
			Log.Error($"Message failed msg=[{ex.Message}]");
			data = Encoding.UTF8.GetBytes(response);
			await resp.OutputStream.WriteAsync(data, 0, data.Length);
			resp.StatusCode = status;
		}
		finally
		{
			resp.Close();
		}

	}

	static Task<ServerInfoResponse> HandleInfo(ServeCliCommandArgs args, ulong inflightRequests)
	{
		// Discovery compares the executing package identity, including development revisions.
		// Dependency normalization (for example 0.0.123.1 -> 0.0.123) belongs to package resolution.
		var version = BeamAssemblyVersionUtil.GetVersion<ServerService>();

		List<string> invocationSnapshot;
		lock (_cliInvocationsLock)
		{
			invocationSnapshot = new List<string>(cliInvocations);
		}

		return Task.FromResult(new ServerInfoResponse
		{
			version = version,
			owner = args.owner,
			inflightRequests = (long)inflightRequests,
			pid = Environment.ProcessId,
			inflightCommands = invocationSnapshot
		});
	}

	private static readonly object _cliInvocationsLock = new object();
	private static readonly List<string> cliInvocations = new List<string>();

	private sealed class Invocation
	{
		public readonly AppLifecycle Lifecycle = new();
		public readonly TaskCompletionSource Completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}
	private static readonly Dictionary<string, Invocation> _stoppableInvocations = new();

	static async Task HandleStop(Stream request)
	{
		using var reader = new StreamReader(request);
		var requestId = JsonConvert.DeserializeObject<ServerRequest>(await reader.ReadToEndAsync())?.requestId;
		if (string.IsNullOrEmpty(requestId)) throw new ArgumentException("A stop request must include requestId.");
		Invocation invocation;
		lock (_cliInvocationsLock) _stoppableInvocations.TryGetValue(requestId, out invocation);
		if (invocation == null) return; // Already completed. Clients wait for registration before stopping.
		lock (invocation)
		{
			if (!invocation.Completed.Task.IsCompleted) invocation.Lifecycle.Cancel();
		}
		await invocation.Completed.Task;
	}

	static async Task HandleExec(ServeCliCommandArgs args, Stream networkRequestStream, HttpListenerResponse response, App app)
	{
		using var inputStream = new StreamReader(networkRequestStream);
		response.Headers.Set(HttpResponseHeader.ContentType, "text/event-stream; charset=utf-8");
		string input = null;
		string commandLine = null;
		IDataReporterService reporter = null;
		var registered = false;
		Invocation invocation = null;
		string requestId = null;
		int exitCode = -1;
		try
		{
			input = await inputStream.ReadToEndAsync();
			Log.Trace("Raw input received: " + input);
			var request = JsonConvert.DeserializeObject<ServerRequest>(input);
			commandLine = request?.commandLine;
			requestId = request?.requestId;
			if (string.IsNullOrWhiteSpace(commandLine))
			{
				throw new ArgumentException("An execute request must include a non-empty commandLine.");
			}
			Log.Trace("virtualizing " + commandLine);
			app ??= new App();
			if (!string.IsNullOrEmpty(requestId)) invocation = new Invocation();

			var sw = Stopwatch.StartNew();
			// Building attaches shared static options to this app's commands; see CommandTreeDetacher.
			lock (CommandTreeDetacher.TreeLock)
			{
				app.Configure(builder =>
				{
					// ConfigureServices reruns this callback for the actual invocation scope.
					if (invocation != null)
					{
						builder.Remove<AppLifecycle>();
						builder.AddSingleton(invocation.Lifecycle);
					}
					builder.Remove<IDataReporterService>();
					builder.AddSingleton<IDataReporterService, ServerReporterService>(provider =>
					{
						var serverReporter = new ServerReporterService(provider, response);
						reporter = serverReporter;
						return serverReporter;
					});
				}, overwriteLogger: false);
				app.Build();
			}
			sw.Stop();
			Log.Trace("build virtual app in " + sw.ElapsedMilliseconds);

			lock (_cliInvocationsLock)
			{
				if (invocation != null) _stoppableInvocations.Add(requestId, invocation);
				cliInvocations.Add(input);
				registered = true;
			}
			if (invocation != null)
			{
				// Flush only after registration, so a client can safely stop even before command startup.
				response.Headers.Set("X-Beam-Request-Id", requestId);
				await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(": ready\n"));
				await response.OutputStream.FlushAsync();
			}
			exitCode = await app.RunWithSingleString(
				commandLine,
				args.useCustomSplitter);
		}
		catch (Exception ex)
		{
			exitCode = ex is CliException cliException ? cliException.NonZeroOrOneExitCode : 1;
			Log.Error($"CLI EXEC FINISHED WITH FAIL MESSAGE=[{ex.Message}]");
			if (reporter != null)
			{
				// Use the same writer/lock as command output and the connection heartbeat.
				reporter.Exception(ex, exitCode, commandLine);
			}
			else
			{
				// Setup failed before a reporter (and its heartbeat) existed. Unity still needs
				// a ReportDataPoint<ErrorOutput>, not ServerErrorResponse or an HTTP-only error.
				var error = new ErrorOutput();
				CliException.Apply(ex, ref error, exitCode, commandLine);
				var report = new ReportDataPoint<ErrorOutput>
				{
					type = DefaultErrorStream.CHANNEL, data = error,
					ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
				};
				var json = JsonConvert.SerializeObject(report, UnitySerializationSettings.Instance);
				var bytes = Encoding.UTF8.GetBytes("data: " + json + Environment.NewLine);
				await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
			}
		}
		finally
		{
			// The command is over: send what is still buffered and stop the heartbeat, which must not outlive it
			// (see ServerReporterService.Complete).
			(reporter as ServerReporterService)?.Complete();

			try
			{
				app?.EndInvocation();
			}
			catch (Exception ex)
			{
				// Releasing the command's resources must not turn a finished command into a failed one.
				Log.Error($"Could not end the CLI invocation cleanly. message=[{ex.Message}]");
			}

			if (registered)
			{
				lock (_cliInvocationsLock)
				{
					cliInvocations.Remove(input);
				}
			}

			if (invocation != null)
			{
				try
				{
					try { lock (invocation) invocation.Lifecycle.Cancel(); }
					finally
					{
						if (reporter is ServerReporterService serverReporter)
							await serverReporter.Completion;
					}
				}
				finally
				{
					lock (invocation)
					{
						invocation.Lifecycle.Source.Dispose();
						invocation.Completed.TrySetResult();
					}
					if (registered)
						lock (_cliInvocationsLock) _stoppableInvocations.Remove(requestId);
				}
			}

			Log.Trace($"CLI EXEC FINISHED WITH EXIT=[{exitCode}] REQ=[{commandLine}]");
		}
	}

	public const int DEFAULT_PORT = 8342;
	public const int MAX_PORT = DEFAULT_PORT + 3000;
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
	private static readonly byte[] Heartbeat = Utf8.GetBytes("​"); // a zero-width character that tests the connection

	private readonly HttpListenerResponse _resp;
	private readonly Stream _output;
	private IDependencyProvider _provider;
	private Task _connectionMonitorTask;
	internal Task Completion => _connectionMonitorTask;
	private AppLifecycle _lifecycle;
	private readonly CancellationTokenSource _completed = new CancellationTokenSource();

	// Everything below is guarded by _lock: the events waiting to be sent, and the writer that encodes into them.
	private readonly object _lock = new object();
	private readonly MemoryStream _pending = new MemoryStream();
	private StreamWriter _pendingWriter;
	private readonly Timer _flushTimer;
	private bool _flushScheduled;
	private bool _isComplete;

	public ServerReporterService(IDependencyProvider provider, HttpListenerResponse resp)
	{
		_provider = provider;
		_resp = resp;
		_output = _resp.OutputStream;
		_pendingWriter = CreatePendingWriter();
		_flushTimer = new Timer(_ => FlushFromTimer(), null, Timeout.Infinite, Timeout.Infinite);
		_lifecycle = _provider.GetService<AppLifecycle>();
		// The heartbeat stops when the command is cancelled (a client hang-up or a /stop request) or once it has
		// finished (Complete).
		var stopMonitor = CancellationTokenSource.CreateLinkedTokenSource(_lifecycle.CancellationToken, _completed.Token);
		var cancellationToken = stopMonitor.Token;
		_connectionMonitorTask = Task.Run(async () =>
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					await Task.Delay(1000, cancellationToken);

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
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				// The command finished or was cancelled.
			}
			catch (Exception ex)
			{
				// remember, async void methods don't report exceptions automatically, so always log it to save brain later.
				App.GlobalLogger?.ZLogError($"cli-server reporter service monitor task failed! type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}]");
			}
			finally
			{
				stopMonitor.Dispose();
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
