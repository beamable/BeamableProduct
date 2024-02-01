using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Events;

namespace Beamable.Server.Api.Events
{
   public class MicroserviceEventsApi : AbsEventsApi, IMicroserviceEventsApi
   {
      private BeamableGetApiResource<EventsGetResponse> _getter;
      public MicroserviceEventsApi(IBeamableRequester requester, IUserContext ctx) : base(requester, ctx)
      {
         _getter = new BeamableGetApiResource<EventsGetResponse>();
      }

      public override Promise<EventsGetResponse> GetCurrent(string scope = "")
      {
         return _getter.RequestData(Requester, Ctx, SERVICE_NAME, scope).Map(response =>
         {
	         response.Init();
	         return response;
         });
      }
   }
}
