using Beamable.Server;

namespace Core.Server.Common
{
	/// <summary>
	/// Represents a client request with payload data.
	/// </summary>
   public struct ClientRequest
   {
	   /// <summary>
	   /// The payload data associated with the client request.
	   /// </summary>
      public object[] Payload;
   }

	/// <summary>
	/// Represents a response from the gateway.
	/// </summary>
   public struct GatewayResponse
   {
	   /// <summary>
	   /// The ID of the response.
	   /// </summary>
	   public long id;

	   /// <summary>
	   /// The status code of the response.
	   /// </summary>
	   public int status;

	   /// <summary>
	   /// The body of the response.
	   /// </summary>
	   public object body;
   }

	/// <summary>
	/// Represents an error response from the gateway.
	/// </summary>
	public struct GatewayErrorResponse
	{
		/// <summary>
		/// The ID of the error response.
		/// </summary>
		public long id;

		/// <summary>
		/// The status code of the error response.
		/// </summary>
		public int status;

		/// <summary>
		/// The body of the error response.
		/// </summary>
		public WebsocketErrorResponse body;
	}

	/// <summary>
	/// Represents a response from the client.
	/// </summary>
	public struct ClientResponse
	{
		/// <summary>
		/// The payload of the client response.
		/// </summary>
		public string payload;
	}
}

namespace Beamable.Server
{
	/// <summary>
	/// Represents an error response for a WebSocket communication.
	/// </summary>
	public class WebsocketErrorResponse
	{
		/// <summary>
		/// The status code associated with the error.
		/// </summary>
		public int status;

		/// <summary>
		/// The service related to the error.
		/// </summary>
		public string service;

		/// <summary>
		/// The error message.
		/// </summary>
		public string error;

		/// <summary>
		/// The detailed error message.
		/// </summary>
		public string message;

		/// <summary>
		/// Returns a string representation of the WebsocketErrorResponse.
		/// </summary>
		/// <returns>A formatted string containing the error details.</returns>
		public override string ToString()
		{
			return
				$"Websocket Error Response. status=[{status}] service=[{service}] error=[{error}] message=[{message}]";
		}
	}

}
