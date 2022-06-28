using System.Net.WebSockets;

namespace Beamable.Server
{
	public static class MicroserviceExceptionExtensions
	{
		public static WebsocketErrorResponse GetErrorResponse(this MicroserviceException ex, string serviceName)
		{
			return new WebsocketErrorResponse
			{
				error = ex.Error,
				message = ex.Message,
				service = serviceName,
				status = ex.ResponseStatus
			};
		}
	}
}
