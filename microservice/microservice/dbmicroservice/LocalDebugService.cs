using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;

namespace Beamable.Server {
    public class LocalDebugService {
        private readonly BeamableMicroService _beamableService;

        public LocalDebugService(BeamableMicroService beamableService) {
            _beamableService = beamableService;
            var server = new WebServer(SharedConstants.HEALTH_PORT)
                .WithModule(new ActionModule("/health", HttpVerbs.Any, HealthCheck));
            server.RunAsync();
        }

        private async Task HealthCheck(IHttpContext context) {
            var healthy = _beamableService.HasInitialized;
            healthy &= _beamableService.GetWebsocketPromise().IsCompleted;
            if (healthy && !_beamableService.IsShuttingDown) {
                var connection = _beamableService.GetWebsocketPromise().GetResult();
                healthy &= connection.State == WebSocketState.Open;
            }
            if (healthy) {
                context.SetHandled();
                await context.SendStringAsync("Healthy: true", "text", Encoding.Default);
            }
            else
            {
                context.Response.StatusCode = 409;
                await context.SendStringAsync("Healthy: false", "text", Encoding.Default);
            }
        }
    }
}