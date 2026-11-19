using Beamable.Server.Api.Usage;
using Beamable.Server.Common;
using System.Net.WebSockets;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Newtonsoft.Json;

using Swan.Logging;
using System;
using System.Threading;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Beamable.Server {


	public class ContainerDiagnosticService : IDisposable
	{
		private WebServer _server;
		private readonly CancellationTokenSource _shutdown = new();
		private int _disposed;

		public ContainerDiagnosticService(IMicroserviceArgs args, BeamableMicroService service, DebugLogProcessor debugLogSink, string serviceName)
			: this(args.HealthPort, service, debugLogSink, serviceName, null) { }

		public ContainerDiagnosticService(IMicroserviceArgs args, BeamableMicroService service, DebugLogProcessor debugLogSink,
			string serviceName, ProcessHealthMonitor processHealth)
			: this(args.HealthPort, service, debugLogSink, serviceName, processHealth) { }

		internal ContainerDiagnosticService(int port, BeamableMicroService service, DebugLogProcessor debugLogSink,
			string serviceName, ProcessHealthMonitor processHealth)
		{
			ConsoleLogger.Instance.LogLevel = LogLevel.Error;
			Log.Verbose($"Debug server starting at port={port}");
			_server = new WebServer(port)
				.WithWebApi("/", m => m.WithController(() => new SampleController(service, debugLogSink, serviceName, processHealth)));
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
			_shutdown.Cancel();
			_server.Dispose();
			_shutdown.Dispose();
		}

		public async Task Run()
		{
			await _server.RunAsync(_shutdown.Token);
		}
		
		public class SampleController : WebApiController 
		{
			private readonly BeamableMicroService _beamableService;
			private readonly DebugLogProcessor _debugLogSink;
			private readonly string _serviceName;
			private IUsageApi _ecsService;
			private readonly ProcessHealthMonitor _processHealth;

			public SampleController(BeamableMicroService service, DebugLogProcessor debugLogSink, string serviceName)
				: this(service, debugLogSink, serviceName, null) { }

			public SampleController(BeamableMicroService service, DebugLogProcessor debugLogSink, string serviceName,
				ProcessHealthMonitor processHealth)
			{
				_beamableService = service;
				_processHealth = processHealth;
				_debugLogSink = debugLogSink;
				_serviceName = serviceName;
			}

			[Route(HttpVerbs.Get, "/metadata")]
			public async Task<string> GetMetadata()
			{
				var metadata = (_ecsService ??= _beamableService.Provider.GetService<IUsageApi>()).GetMetadata();
				var json = JsonConvert.SerializeObject(metadata);
				return json;
			}
			
			[Route(HttpVerbs.Get, "/usage")]
			public async Task<string> GetUsage()
			{
				var usage = (_ecsService ??= _beamableService.Provider.GetService<IUsageApi>()).GetUsage();
				var json = JsonConvert.SerializeObject(usage);
				return json;
			}

			[Route(HttpVerbs.Get, "/stop")]
			public string Stop([QueryField]string reason)
			{
				Task.Run(async () =>
				{
					Log.Information($"Stopping service through debug-server due to reason=[{reason}]");
					await Task.Delay(100);
					Environment.Exit(0);
				});
				return "stopping";
			}

		[Route(HttpVerbs.Get, "/logs")]
			public string StreamLogs()
			{
				this.Response.ContentType = "text/event-stream";

				using var writer = this.HttpContext.OpenResponseText();
				var (id, channel) = _debugLogSink.CreateUniqueListenerSubscription(_serviceName);
				try
				{
					while (!HttpContext.CancellationToken.IsCancellationRequested)
					{
						if (!channel.Reader.TryRead(out var message))
						{
							Thread.Sleep(10);
							continue;
						}

						writer.WriteLine($"data: {message}");
						writer.Flush();
					}

					return "";
				}
				finally
				{
					_debugLogSink.ReleaseSubscription(id);
				}
			}
			
			/// <summary>Cached observation only. This route does not trigger sampling or incident collection.</summary>
			[Route(HttpVerbs.Get, "/health/process")]
			public async Task ProcessHealth()
			{
				var monitor = _processHealth ?? _beamableService.HealthMonitor;
				Response.Headers.Set("X-Beam-Health-Diagnostic-Records-Dropped", (monitor?.DroppedDiagnosticRecords ?? 0).ToString());
				Response.Headers.Set("X-Beam-Health-Diagnostic-Records-Failed", (monitor?.FailedDiagnosticRecords ?? 0).ToString());
				await WriteProcessSnapshot(monitor?.Snapshot ?? ProcessHealthSnapshot.Starting);
			}

			/// <summary>The first snapshot of the latest incident, retained even when diagnostic delivery fails.</summary>
			[Route(HttpVerbs.Get, "/health/process/incident")]
			public Task ProcessHealthIncident() => WriteProcessSnapshot((_processHealth ?? _beamableService.HealthMonitor)?.LastIncident);

			private async Task WriteProcessSnapshot(ProcessHealthSnapshot snapshot)
			{
				Response.ContentType = "application/json";
				using var writer = HttpContext.OpenResponseText();
				await writer.WriteAsync(JsonConvert.SerializeObject(snapshot));
			}

			[Route(HttpVerbs.Any, "/health")]
			public string HealthCheck()
			{
				var process = (_processHealth ?? _beamableService.HealthMonitor)?.Snapshot ?? ProcessHealthSnapshot.Starting;
				Response.Headers.Set("X-Beam-Process-Health", process.State.ToString());
				Response.Headers.Set("X-Beam-Process-Health-Reason", process.Reason);
				Response.Headers.Set("X-Beam-Process-Health-Observation-Only", "true");
				// Observation mode deliberately preserves the legacy first-instance status and text below.
				// have we completed setup, which includes auth and spinning up the service providers?
				var healthy = _beamableService.HasInitialized;

				// okay, is the websocket promise still complete? We are temporarily unhealthy if there is no connection...
				var isSocketFinished = _beamableService.GetWebsocketPromise()?.IsCompleted ?? false;
				healthy &= isSocketFinished;

				var isConnectionOpen = false;
				var isAuthed = false;
				if (healthy && !_beamableService.IsShuttingDown)
				{
					// assuming we aren't in the process of a shutdown, get the connection, and make sure its open
					var connection = _beamableService.GetWebsocketPromise().GetResult();
					isConnectionOpen = connection.State == WebSocketState.Open;
					healthy &= isConnectionOpen;

					// the authorization needs to be valid. If we lapse in auth, we may fail a health check, and that would be okay, but to fail multiple health checks would be bad.
					isAuthed = _beamableService.AuthenticationDaemon.NoPendingOrInProgressAuth;
					healthy &= isAuthed;
				}

				if (healthy)
				{
					return "Healthy: true";
				}
				else
				{
					Log.Verbose("Failed a health check. {init} {isSocketFinished} {isConnectionOpen} {isAuthed}", _beamableService.HasInitialized, isSocketFinished, isConnectionOpen, isAuthed);
					HttpContext.Response.StatusCode = 409;
					return "Healthy: false";
				}
			}
		}
	}
}
