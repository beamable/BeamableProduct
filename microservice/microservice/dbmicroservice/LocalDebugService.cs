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
using ZLogger;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Beamable.Server {


	public class ContainerDiagnosticService
	{
		private WebServer _server;

		public ContainerDiagnosticService(IMicroserviceArgs args, BeamableMicroService service, DebugLogProcessor debugLogSink)
		{
			ConsoleLogger.Instance.LogLevel = LogLevel.Error;
			Log.Verbose($"Debug server starting at port={args.HealthPort}");
			_server = new WebServer(args.HealthPort)
				.WithWebApi("/", m => m.WithController(() => new SampleController(service, debugLogSink)));
		}

		public async Task Run()
		{
			await _server.RunAsync();
		}
		
		public class SampleController : WebApiController 
		{
			private readonly BeamableMicroService _beamableService;
			private readonly DebugLogProcessor _debugLogSink;
			private IUsageApi _ecsService;

			public SampleController(BeamableMicroService service, DebugLogProcessor debugLogSink)
			{
				_ecsService = service.Provider.GetService<IUsageApi>();
				_beamableService = service;
				_debugLogSink = debugLogSink;
			}

			[Route(HttpVerbs.Get, "/metadata")]
			public async Task<string> GetMetadata()
			{
				var metadata = _ecsService.GetMetadata();
				var json = JsonConvert.SerializeObject(metadata);
				return json;
			}
			
			[Route(HttpVerbs.Get, "/usage")]
			public async Task<string> GetUsage()
			{
				var usage = _ecsService.GetUsage();
				var json = JsonConvert.SerializeObject(usage);
				return json;
			}

			[Route(HttpVerbs.Get, "/stop")]
			public string Stop([QueryField]string reason)
			{
				Task.Run(async () =>
				{
					await Task.Delay(100);
					Log.Information($"Stopping service through debug-server due to reason=[{reason}]");
					Environment.Exit(0);
				});
				return "stopping";
			}

			[Route(HttpVerbs.Get, "/logs")]
			public string StreamLogs()
			{
				this.Response.ContentType = "text/event-stream";

				using var writer = this.HttpContext.OpenResponseText();
				var id = Guid.NewGuid().ToString();
				var channel = _debugLogSink.GetMessageSubscription(id);
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
			
			[Route(HttpVerbs.Any, "/health")]
			public string HealthCheck()
			{
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
