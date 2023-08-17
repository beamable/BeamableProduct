using System.Net.WebSockets;

namespace Beamable.Server
{
	/// <summary>
	/// Provides extension methods for handling MicroserviceException instances.
	/// </summary>
   public static class MicroserviceExceptionExtensions
   {
	   /// <summary>
	   /// Gets a WebsocketErrorResponse based on a MicroserviceException and a service name.
	   /// </summary>
	   /// <param name="ex">The MicroserviceException instance.</param>
	   /// <param name="serviceName">The name of the service associated with the exception.</param>
	   /// <returns>A WebsocketErrorResponse containing the error details.</returns>
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
