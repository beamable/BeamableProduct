using EmbedIO;
using EmbedIO.Actions;
using Swan;
using Swan.Logging;
using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Server
{
	public class LocalDebugService
	{
		private readonly BeamableMicroService _beamableService;

		private string _lastToken;

		public LocalDebugService(BeamableMicroService beamableService)
		{
			_beamableService = beamableService;
			Swan.Logging.ConsoleLogger.Instance.LogLevel = LogLevel.Error;
			var server = new WebServer(HEALTH_PORT)
				.WithModule(new ActionModule("/health", HttpVerbs.Any, HealthCheck))
				;
			server.RunAsync();
		}

		private async Task HealthCheck(IHttpContext context)
		{
			var healthy = _beamableService.HasInitialized;
			healthy &= _beamableService.GetWebsocketPromise()?.IsCompleted ?? false;
			if (healthy && !_beamableService.IsShuttingDown)
			{
				var connection = _beamableService.GetWebsocketPromise().GetResult();
				healthy &= connection.State == WebSocketState.Open;
			}
			if (healthy)
			{
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
