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
