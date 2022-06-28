using Core.Server.Common;
using Newtonsoft.Json;

namespace Beamable.Server
{
	public static class MicroserviceExtensions
	{
		public static GatewayResponse FormatResponse(this MicroserviceAttribute serviceAttribute, long requestId, int status, object body)
		{
			var response = new GatewayResponse
			{
				id = requestId,
				status = status,
				body = body
			};
#pragma warning disable 618
			if (serviceAttribute.UseLegacySerialization)
#pragma warning restore 618
			{
				response = new GatewayResponse
				{
					id = requestId,
					status = status,
					body = new ClientResponse
					{
						payload = JsonConvert.SerializeObject(body)
					}
				};
			}

			return response;
		}
	}
}
