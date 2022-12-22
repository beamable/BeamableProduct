using System.Net.WebSockets;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Serilog;
using Swan.Logging;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Server {


	public class ContainerDiagnosticService
	{
		private readonly BeamableMicroService _beamableService;
		private WebServer _server;

		public ContainerDiagnosticService(BeamableMicroService service)
		{
			ConsoleLogger.Instance.LogLevel = LogLevel.Error;
			_beamableService = service;
			_server = new WebServer(HEALTH_PORT)
				.WithWebApi("/", m => m.WithController(() => new SampleController(service)));
		}

		public async Task Run()
		{
			await _server.RunAsync();
		}
		
		public class SampleController : WebApiController 
		{
			private readonly BeamableMicroService _beamableService;
			public SampleController(BeamableMicroService service)
			{
				_beamableService = service;
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
