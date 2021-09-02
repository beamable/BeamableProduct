using Core.Server.Common;
using Newtonsoft.Json;

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
               body = new ClientResponse
               {
                  payload = JsonConvert.SerializeObject(result)
               }
            };
         }

         var json = JsonConvert.SerializeObject(response);
         return json;
      }
   }
}