using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using UnityEngine;

namespace Beamable.Server
{
   public static class ApiContentExtensions
   {
      private static readonly ApiVariableBag EMPTY_VARIABLES = new ApiVariableBag();

      public static async Promise RequestApi(this ApiRef apiRef, ApiVariableBag variables=null)
      {
         var content = await apiRef.Resolve();
         await RequestApi(content, variables);
      }

      public static async Promise RequestApi(this ApiContent api, ApiVariableBag variables=null)
      {
         Debug.Log("Sending API Request " + api.Id);
         await MicroserviceClientHelper.Request<Unit>(
            api.ServiceRoute.Service,
            api.ServiceRoute.Endpoint,
            api.PrepareParameters(variables));
      }


      public static string[] PrepareParameters(this ApiContent content, ApiVariableBag variableBag=null)
      {
         variableBag = variableBag ?? EMPTY_VARIABLES;

         var parameters = content.Parameters.Parameters;

         var outputs = new string[parameters.Length];

         for (var i = 0; i < outputs.Length; i++)
         {
            outputs[i] = parameters[i].ResolveParameter(variableBag);
         }

         Debug.Log($"outputs are {string.Join(",\n", outputs)}");
         return outputs;
      }

      public static string ResolveParameter(this RouteParameter parameter, ApiVariableBag variables = null)
      {
         variables = variables ?? EMPTY_VARIABLES;
         if (parameter.variableReference.HasValue)
         {
            var key = parameter.variableReference.Value.Name;
            if (variables.TryGetValue(key, out var raw))
            {
               return raw?.ToString();
            }
            else
            {
               Debug.LogWarning($"There is no variable for {key}. Sending null reference");
               return null;
            }
         }

         return parameter.Data;

      }
   }

}