
using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Platform.SDK;
using Beamable;
using Beamable.Common;
using Beamable.Common.Api;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

      private string _prefix;

      protected string SerializeArgument<T>(T arg)
      {
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
               return "\"" + prim + "\"";
            case double prim:
               return prim.ToString();
            case float prim:
               return prim.ToString();
            case int prim:
               return prim.ToString();
         }
         return JsonUtility.ToJson(arg);
      }

      protected T DeserializeResult<T>(string json)
      {
         var defaultInstance = default(T);

         if (typeof(Unit).IsAssignableFrom(typeof(T)))
         {
            return (T)(object) PromiseBase.Unit;
         }

         if (typeof(T).Equals(typeof(string)))
         {
            return (T)(object) json;
         }
         switch (defaultInstance)
         {
            case float _:
               return (T) (object) float.Parse(json);
            case long _:
               return (T) (object) long.Parse(json);
            case double _:
               return (T) (object) double.Parse(json);
            case bool _:
               return (T) (object) bool.Parse(json);
            case int _:
               return (T) (object) int.Parse(json);
         }

         return JsonUtility.FromJson<T>(json);
      }

      protected Promise<T> Request<T>(string serviceName, string endpoint, string[] serializedFields)
      {
         var prefix = _prefix ?? (_prefix = MicroserviceIndividualization.GetServicePrefix(serviceName));
         var fullPath = $"{prefix}micro_{serviceName}/{endpoint}";


         Debug.Log($"Client called {endpoint} with {serializedFields.Length} arguments");
         var argArray = "[ " + string.Join(",", serializedFields) + " ]";
         Debug.Log(argArray);

         T Parser(string json)
         {
            // TODO: Remove this in 0.13.0
            if (!(json?.StartsWith("{\"payload\":\"") ?? false)) return DeserializeResult<T>(json);

#pragma warning disable 618
            Debug.LogWarning($"Using legacy payload string. Redeploy the {serviceName} service without the {nameof(MicroserviceAttribute.UseLegacySerialization)} setting.");
#pragma warning restore 618
            var responseObject = DeserializeResult<ResponseObject>(json);
            var result = DeserializeResult<T>(responseObject.payload);
            return result;
         }

         return API.Instance.Map(de => de.Requester).FlatMap(requester =>
            {
               var url = $"/basic/{requester.Cid}.{requester.Pid}.{fullPath}";

               var req = new RequestObject
               {
                  payload = argArray
               };

               Debug.Log($"Sending Request uri=[{url}]");
               return requester.Request<T>(Method.POST, url, req, parser: Parser);
            })
            .Error(err => { Debug.LogError(err); });
      }

      protected static string CreateEndpointPrefix(string serviceName)
      {
         #if UNITY_EDITOR

         #endif

         return serviceName;
      }

   }
}