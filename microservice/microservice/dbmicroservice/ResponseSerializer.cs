using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Core.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Beamable.Server
{
	public interface IResponseSerializer
	{
		string SerializeResponse(RequestContext ctx, object result);
	}

	public class CustomResponseSerializer : IResponseSerializer
	{
		private readonly CustomResponseSerializationAttribute _attribute;

		public CustomResponseSerializer(CustomResponseSerializationAttribute attribute)
		{
			_attribute = attribute;
		}

		public string SerializeResponse(RequestContext ctx, object result)
		{
			var raw = _attribute.SerializeResponse(result);
			return $"{{\"id\": {ctx.Id}, \"status\": 200, \"body\": {raw} }}";
		}
	}

	public class DefaultResponseSerializer : IResponseSerializer
	{
		private readonly bool _useLegacySerialization;

		public DefaultResponseSerializer(bool useLegacySerialization)
		{
			_useLegacySerialization = useLegacySerialization;
		}

		public string SerializeResponse(RequestContext ctx, object result)
		{
			var response = new GatewayResponse
			{
				id = ctx.Id,
				status = 200,
				body = result
			};
#pragma warning disable 618
			if (_useLegacySerialization)
#pragma warning restore 618
			{
				response = new GatewayResponse
				{
					id = ctx.Id,
					status = 200,
				};


				if (result is string strResult)
				{
					response.body = new ClientResponse
					{
						payload = strResult
					};
				}
				else
				{
					string serializedString = JsonConvert.SerializeObject(result, UnitySerializationSettings.Instance);
					response.body = new ClientResponse
					{
						payload = serializedString
					};
				}
			}

			var json = JsonConvert.SerializeObject(response, UnitySerializationSettings.Instance);
			return json;
		}
	}
}
