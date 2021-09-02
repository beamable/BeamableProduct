using System;
using System.Collections.Generic;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Events;

namespace Beamable.Api.Events
{

   public class EventSubscription : PlatformSubscribable<EventsGetResponse, EventsGetResponse>
   {
      public EventSubscription(PlatformService platform, IBeamableRequester requester) : base(platform, requester, AbsEventsApi.SERVICE_NAME)
      {
      }

      public void ForceRefresh()
      {
         Refresh();
      }

      protected override void OnRefresh(EventsGetResponse data)
      {
         data.Init();
         Notify(data);
      }
   }

   /// <summary>
   /// This type defines the %Client main entry point for the %Events feature.
   /// 
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   /// 
   /// #### Related Links
   /// - See the <a target="_blank" href="https://docs.beamable.com/docs/events-feature">Events</a> feature documentation
   /// - See Beamable.API script reference
   /// 
   /// ![img beamable-logo]
   /// 
   /// </summary>
   public class EventsService : AbsEventsApi, IHasPlatformSubscriber<EventSubscription, EventsGetResponse, EventsGetResponse>
   {
      public EventSubscription Subscribable { get; }

      public EventsService(PlatformService platform, IBeamableRequester requester) : base(requester, platform)
      {
         Subscribable = new EventSubscription(platform, requester);
      }

      public override Promise<EventClaimResponse> Claim(string eventId)
      {
         return base.Claim(eventId).Then(_ => Subscribable.ForceRefresh());
      }

      public override Promise<EventsGetResponse> GetCurrent(string scope = "") => Subscribable.GetCurrent(scope);
   }

}