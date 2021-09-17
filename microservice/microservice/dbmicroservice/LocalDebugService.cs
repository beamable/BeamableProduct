﻿using System.Net.Mime;
 using System.Net.WebSockets;
 using System.Text;
using System.Threading.Tasks;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Content;
using Beamable.Content;
using EmbedIO;
 using EmbedIO.Actions;
 using EmbedIO.Routing;
using EmbedIO.WebApi;
using ContentService = Beamable.Server.Content.ContentService;

namespace Beamable.Server {
    public class LocalDebugService {
        private readonly BeamableMicroService _beamableService;

        public LocalDebugService(BeamableMicroService beamableService) {
            _beamableService = beamableService;
            var server = new WebServer(6565)
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
                await context.SendStringAsync("Healthy: true", "text", Encoding.Default);
            }
            else {
                await context.SendStringAsync("Healthy: false", "text", Encoding.Default);
            }
        }
    }
}