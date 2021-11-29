
using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Platform.SDK;
using Beamable;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Serialization;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Beamable.Serialization.SmallerJSON;
using Utf8Json;

namespace Beamable.Server
{
   public class MicroserviceClient
   {
      [System.Serializable]
      public class ResponseObject
      {
         public string payload;
      }

      [System.Serializable]
      public class RequestObject
      {
         public string payload;
      }

      protected string _prefix;

      protected string SerializeArgument<T>(T arg)
      {
	      try
	      {
		      return BeamableJson.Serialize(arg);
	      }
	      catch(Exception e)
	      {
		      Debug.LogError(e);
	      }
         // JSONUtility will serialize objects correctly, but doesn't handle primitives well.
         if (arg == null)
         {
            return "null";
         }

         switch (arg)
         {
            case IEnumerable enumerable when !(enumerable is string):
               var output = new List<string>();
               foreach (var elem in enumerable)
               {
                  output.Add(SerializeArgument(elem));
               }

               var outputJson = "[" + string.Join(",", output) + "]";
               return outputJson;

            case bool prim:
               return prim ? "true": "false";
            case long prim:
               return prim.ToString();
            case string prim:
               return Json.IsValidJson(prim) ? "[" + prim + "]" : "\"" + prim + "\"";
            case double prim:
               return prim.ToString();
            case float prim:
               return prim.ToString();
            case int prim:
               return prim.ToString();
            case Vector2Int prim:
               return JsonUtility.ToJson(new Vector2IntEx(prim));
            case Vector3Int prim:
               return JsonUtility.ToJson(new Vector3IntEx(prim));
            }
         return JsonUtility.ToJson(arg);
      }

      protected T DeserializeResult<T>(string json)
      {
	      try
	      {
		      return BeamableJson.Deserialize<T>(json);
	      }
	      catch(Exception e)
	      {
		      Debug.LogError(e);
	      }

	      return default;
      }

      protected string CreateUrl(string cid, string pid, string serviceName, string endpoint)
      {
         var prefix = _prefix ?? (_prefix = MicroserviceIndividualization.GetServicePrefix(serviceName));
         var path = $"{prefix}micro_{serviceName}/{endpoint}";
         var url = $"/basic/{cid}.{pid}.{path}";
         return url;
      }

      protected async Promise<T> Request<T>(string serviceName, string endpoint, string[] serializedFields)
      {
         Debug.Log($"Client called {endpoint} with {serializedFields.Length} arguments");
         var argArray = "[ " + string.Join(",", serializedFields) + " ]";
         Debug.Log(argArray);

         T Parser(string json)
         {
            // TODO: Remove this in 0.13.0
            if (!(json?.StartsWith("{\"payload\":\"") ?? false)) return DeserializeResult<T>(json);

#pragma warning disable 618
            Debug.LogWarning($"Using legacy payload string. Redeploy the {serviceName} service without the UseLegacySerialization setting.");
#pragma warning restore 618
            var responseObject = DeserializeResult<ResponseObject>(json);
            var result = DeserializeResult<T>(responseObject.payload);
            return result;
         }

         var b = await API.Instance;
         var requester = b.Requester;
         var url = CreateUrl(b.Token.Cid, b.Token.Pid, serviceName, endpoint);
         var req = new RequestObject
         {
            payload = argArray
         };
         Debug.Log($"Sending Request uri=[{url}]");
         return await requester.Request<T>(Method.POST, url, req, parser: Parser);
      }

      protected static string CreateEndpointPrefix(string serviceName)
      {
         #if UNITY_EDITOR

         #endif

         return serviceName;
      }

   }
}
