using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
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
                .WithAction("/routes/scan", HttpVerbs.Post, RebuildRoutes)
                .WithAction("/token", HttpVerbs.Get, GetChangeToken)
                ;
            server.RunAsync();
        }

        private async Task GetChangeToken(IHttpContext context)
        {
            /*
             * Load up a specific class that
             */
            try
            {
                var typeName =
                    _beamableService.MicroserviceType.AssemblyQualifiedName.Replace(
                        _beamableService.MicroserviceType.Name, "Beamable__Change_Token_Class");
                var type = Type.GetType(typeName, true);

                var method = type.GetMethod("GetToken", BindingFlags.Static | BindingFlags.Public);
                var value = method.Invoke(null, new object[] { });
                var token = value?.ToString() ?? throw new Exception("Invalid token" + value);

                if (!string.IsNullOrEmpty(_lastToken) && !string.Equals(_lastToken, token))
                {
                    _beamableService.RebuildRouteTable();
                }

                _lastToken = token;
                await context.SendStringAsync(token, "text", Encoding.Default);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.SendStringAsync(ex.Message + "\n\n" + ex.StackTrace, "text", Encoding.Default);
            }
        }

        private async Task RebuildRoutes(IHttpContext context)
        {
            try
            {
                _beamableService.RebuildRouteTable();
                context.SetHandled();
                await context.SendStringAsync("Rebuilt", "text", Encoding.Default);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.SendStringAsync(ex.Message + "\n\n" + ex.StackTrace, "text", Encoding.Default);
            }
        }

        private async Task HealthCheck(IHttpContext context) {
            var healthy = _beamableService.HasInitialized;
            healthy &= _beamableService.GetWebsocketPromise()?.IsCompleted ?? false;
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