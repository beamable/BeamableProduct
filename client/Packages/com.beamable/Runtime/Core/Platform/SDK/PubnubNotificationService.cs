using System;
using Beamable.Common;
using Beamable.Common.Api;

namespace Beamable.Api
{
   public class PubnubNotificationService
   {
      private PlatformRequester _requester;

      public PubnubNotificationService (PlatformRequester requester)
      {
         _requester = requester;
      }

      public Promise<SubscriberDetailsResponse> GetSubscriberDetails ()
      {
         return _requester.Request<SubscriberDetailsResponse>(Method.GET, "/basic/notification");
      }
   }

   [Serializable]
   public class SubscriberDetailsResponse
   {
      public string subscribeKey;
      public string gameNotificationChannel;
      public string gameGlobalNotificationChannel;
      public string playerChannel;
      public string playerForRealmChannel;
      public string customChannelPrefix;
      public string authenticationKey;
   }
}