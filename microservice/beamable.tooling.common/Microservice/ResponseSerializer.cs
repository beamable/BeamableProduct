using System;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Core.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Beamable.Server
{
	/// <summary>
	/// Represents a response serializer interface.
	/// </summary>
	public interface IResponseSerializer
	{
		/// <summary>
		/// Serializes a response using the specified request context and result object.
		/// </summary>
		/// <param name="ctx">The request context associated with the response.</param>
		/// <param name="result">The result object to be serialized.</param>
		/// <returns>The serialized response as a string.</returns>
        string SerializeResponse(RequestContext ctx, object result);
    }

	/// <summary>
	/// Custom response serializer based on a specified attribute.
	/// </summary>
	public class CustomResponseSerializer : IResponseSerializer
	{
		private readonly CustomResponseSerializationAttribute _attribute;

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomResponseSerializer"/> class.
		/// </summary>
		/// <param name="attribute">The custom response serialization attribute.</param>
        public CustomResponseSerializer(CustomResponseSerializationAttribute attribute)
        {
            _attribute = attribute;
        }

        /// <inheritdoc />
        public string SerializeResponse(RequestContext ctx, object result)
        {
            var raw = _attribute.SerializeResponse(result);
            return $"{{\"id\": {ctx.Id}, \"status\": 200, \"body\": {raw} }}";
        }
    }

	/// <summary>
	/// Default response serializer implementation.
	/// </summary>
	public class DefaultResponseSerializer : IResponseSerializer
	{
		private readonly bool _useLegacySerialization;

		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultResponseSerializer"/> class.
		/// </summary>
		/// <param name="useLegacySerialization">Whether to use legacy serialization.</param>
        public DefaultResponseSerializer(bool useLegacySerialization)
        {
            _useLegacySerialization = useLegacySerialization;
        }

        /// <inheritdoc />
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

            
            
            var type = result?.GetType();
            if (type?.IsEnum ?? false)
            {
	            response.body = Convert.ChangeType(result, typeof(int)).ToString();
            }
            
            var json = JsonConvert.SerializeObject(response, UnitySerializationSettings.Instance);
            
            return json;
        }
    }
}
