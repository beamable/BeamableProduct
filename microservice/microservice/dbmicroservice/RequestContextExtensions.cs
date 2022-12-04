using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Beamable.Common;

namespace Beamable.Server
{
   public static class RequestContextExtensions
   {
      public static bool TryBuildRequestContext(this string msg, IMicroserviceArgs args, out RequestContext context)
      {
         long id = 0;
         string path = "";
         string methodName = "";
         string body = ""; //is there an advantage to keeping it a JsonElement?
         long userID = 0;
         int status = 0;
         var headers = new Dictionary<string, string>();
         HashSet<string> scopes = new HashSet<string>();

         if (string.IsNullOrEmpty(msg))
         {
            // invalid JSON
            context = null;
            return false;
         }
         JsonElement bodyElement = default;
         JsonElement headerElement = default;
         try
         {
            using (JsonDocument document = JsonDocument.Parse(msg))
            {
               id = document.RootElement.GetProperty("id").GetInt32();
               JsonElement temp;

               if (document.RootElement.TryGetProperty("body", out temp))
               {
	               bodyElement = temp.Clone();
               }
               if (document.RootElement.TryGetProperty("path", out temp))
                  path = temp.GetString();
               if (document.RootElement.TryGetProperty("method", out temp))
                  methodName = temp.GetString();
               if (document.RootElement.TryGetProperty("from", out temp) && temp.ValueKind == JsonValueKind.Number)
               {
                  if (!temp.TryGetInt64(out userID))
                  {
                     userID = 0;
                  }
               }
               if (document.RootElement.TryGetProperty("status", out temp))
               {
                  status = temp.GetInt32();
               }

               if (document.RootElement.TryGetProperty("scopes", out temp) && temp.ValueKind == JsonValueKind.Array)
               {
                  var scopeList = temp.EnumerateArray().Select(elem => elem.GetString()).ToList();
                  scopes = new HashSet<string>(scopeList);
               }

               if (document.RootElement.TryGetProperty("headers", out temp) && temp.ValueKind == JsonValueKind.Object)
               {
	               headerElement = temp.Clone();
               }

            }
         }
         catch (Exception e)
         {
            BeamableLogger.LogException(e);
            throw;
         }

         var microserviceRequestContext = new MicroserviceRequestContext(args.CustomerID, args.ProjectName, id, status, userID, path, methodName, body, scopes, null)
         {
	         BodyElement = bodyElement,
	         HeaderElement = headerElement
         };
         context = microserviceRequestContext;
         return true;
      }
   }
}
