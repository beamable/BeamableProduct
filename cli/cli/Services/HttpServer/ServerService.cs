using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Server;
using Beamable.Server.Common;
using cli.CliServerCommand;
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
			Log.Verbose($"Starting request. inflight=[{Interlocked.Read(ref _inflightRequests)}]");

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
					Log.Verbose($"Finishing request. inflight=[{Interlocked.Read(ref _inflightRequests)}]");
				}
			});
		}
		Log.Information("Shutting down...");
		
	}

	static async Task HandleRequest(ServeCliCommandArgs args, HttpListenerContext ctx,
		ulong inflightRequests)
	{
		// Peel out the requests and response objects
		HttpListenerRequest req = ctx.Request;
		HttpListenerResponse resp = ctx.Response;

		// http://base:port/
		var routePath = ctx.Request.Url.ToString().Substring(ctx.Request.Url.ToString().LastIndexOf('/') + 1);
		Log.Verbose("got message at route: " + routePath);
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
				case EXEC_ROUTE:
					await HandleExec(args, req.InputStream, resp);
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


		resp.Close();

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
			inflightCommands = cliInvocations
		});
	}

	public static List<string> cliInvocations = new List<string>();

	static async Task HandleExec(ServeCliCommandArgs args, Stream networkRequestStream, HttpListenerResponse response)
	{
		using var inputStream = new StreamReader(networkRequestStream);
		response.Headers.Set(HttpResponseHeader.ContentType, "text/event-stream; charset=utf-8");
		var input = await inputStream.ReadToEndAsync();
		Log.Verbose("Raw input received: " + input);
		var req = JsonConvert.DeserializeObject<ServerRequest>(input);
		cliInvocations.Add(input);
		Log.Verbose("virtualizing " + req.commandLine);
		
		var app = new App();

		var sw = new Stopwatch();
		sw.Start();
		app.Configure(builder =>
		{
			// inject the existing config service into the sub-app.
			builder.RemoveIfExists<ConfigService>();
			builder.AddSingleton<ConfigService>(args.ConfigService);
			
			builder.Remove<IDataReporterService>();
			builder.AddSingleton<IDataReporterService, ServerReporterService>(provider => new ServerReporterService(provider, response));
		}, overwriteLogger: false);
		app.Build();
		sw.Stop();
		Log.Verbose("build virtual app in " + sw.ElapsedMilliseconds);

		int exitCode = -1;
		try
		{
			exitCode = await app.RunWithSingleString(req.commandLine, args.useCustomSplitter);
		}
		catch (Exception ex)
		{
			Log.Error($"CLI EXEC FINISHED WITH FAIL MESSAGE=[{ex.Message}]");
		}
		finally
		{
			cliInvocations.Remove(input);
			Log.Verbose($"CLI EXEC FINISHED WITH EXIT=[{exitCode}] REQ=[{req.commandLine}]");
		}
	}

	public const int DEFAULT_PORT = 8342;
	public const int MAX_PORT = DEFAULT_PORT + 3000;
}

public class ServerReporterService : IDataReporterService
{
	private readonly HttpListenerResponse _resp;
	private StreamWriter _streamWriter;
	private IDependencyProvider _provider;
	private Task _connectionMonitorTask;
	private AppLifecycle _lifecycle;

	public ServerReporterService(IDependencyProvider provider, HttpListenerResponse resp)
	{
		_provider = provider;
		_resp = resp;
		_streamWriter = new StreamWriter(_resp.OutputStream);
		_lifecycle = _provider.GetService<AppLifecycle>();
		_connectionMonitorTask = Task.Run(async () =>
		{
			try
			{
				while (!_lifecycle.IsCancelled)
				{
					await Task.Delay(1000);

					// check if the pipe is still open, because if it isn't, the invocation should cancel.
					try
					{
						lock(_streamWriter)
						{
							_streamWriter.Write("\u200b"); // write a zero-width character to test the connection.
							_streamWriter.Flush();
						}

					}
					catch (HttpListenerException)
					{
						// the pipe is broken, so can assume the client is no longer connected, and we can cancel this invocation.
						App.GlobalLogger.LogTrace("monitor found that client is no longer connected; cancelling app lifecycle.");
						_lifecycle.Cancel();
						break;
					}
				}
			}
			catch (Exception ex)
			{
				// remember, async void methods don't report exceptions automatically, so always log it to save brain later.
				App.GlobalLogger.ZLogError($"cli-server reporter service monitor task failed! type=[{ex.GetType().Name}] message=[{ex.Message}] stack=[{ex.StackTrace}]");
			}
		});
	} 
	
	public void Report(string rawMessage)
	{
		try
		{
			var data = "data: " + rawMessage + Environment.NewLine;
			var bytes = Encoding.UTF8.GetBytes(data);
			lock(_streamWriter)
			{
				_streamWriter.BaseStream.Write(bytes, 0, bytes.Length);
				_streamWriter.Flush();
			}

		}
		catch (HttpListenerException ex)
		{
			// the pipe is broken, so can assume the client is no longer connected, and we can cancel this invocation.
			App.GlobalLogger.LogTrace("client is no longer connected; cancelling app lifecycle." + ex.Message);
			_lifecycle.Cancel();
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
		var json = JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance);
		Report(json);
	}
}
