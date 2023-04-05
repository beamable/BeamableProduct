using Beamable.Server;

namespace Core.Server.Common
{
   public struct ClientRequest
   {

      public object[] Payload;
   }

   public struct GatewayResponse
   {
      public long id;
      public int status;
      public object body;
   }

   public struct GatewayErrorResponse
   {
      public long id;
      public int status;
      public WebsocketErrorResponse body;
   }

   public struct ClientResponse
   {
      public string payload;
   }


}

namespace Beamable.Server
{
	public class WebsocketErrorResponse
	{
		public int status;
		public string service;
		public string error;
		public string message;

		public override string ToString()
		{
			return
				$"Websocket Error Response. status=[{status}] service=[{service}] error=[{error}] message=[{message}]";
		}
	}

}
