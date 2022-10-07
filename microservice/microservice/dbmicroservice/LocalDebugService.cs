using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Serilog;
using Swan;
using Swan.Logging;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Server {
    public class LocalDebugService {
        private readonly BeamableMicroService _beamableService;

        private string _lastToken;

        public LocalDebugService(BeamableMicroService beamableService) {
            _beamableService = beamableService;
            Swan.Logging.ConsoleLogger.Instance.LogLevel = LogLevel.Error;
            var server = new WebServer(HEALTH_PORT)
                .WithModule(new ActionModule("/health", HttpVerbs.Any, HealthCheck))
                ;
            server.RunAsync();
        }

        private async Task HealthCheck(IHttpContext context)
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

            if (healthy) {
                context.SetHandled();
                await context.SendStringAsync("Healthy: true", "text", Encoding.Default);
            }
            else
            {
	            Log.Verbose("Failed a health check. {init} {isSocketFinished} {isConnectionOpen} {isAuthed}", _beamableService.HasInitialized, isSocketFinished, isConnectionOpen, isAuthed);
	            context.Response.StatusCode = 409;
                await context.SendStringAsync("Healthy: false", "text", Encoding.Default);
            }
        }
    }
}
