using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using UnityEngine;

namespace Beamable.Server
{
   public static class BeamableRequesterExtensions
   {
      public static async Promise RequestApi(this IBeamableRequester requester, ApiRef apiRef, ApiVariableBag variables=null)
      {
         Debug.Log("Running api " + apiRef.Id);

         var content = await apiRef.Resolve();
         Debug.Log("Running api content" + content.ServiceRoute.Service);

         await RequestApi(requester, content, variables);
      }

      public static async Promise RequestApi(this IBeamableRequester requester, ApiContent api, ApiVariableBag variables=null)
      {
         await MicroserviceClientHelper.Request<Unit>(
            api.ServiceRoute.Service,
            api.ServiceRoute.Endpoint,
            api.PrepareParameters(variables));
      }


   }
}