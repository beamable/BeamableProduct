using System;
using Beamable.Common.Content;
using Beamable.Server;

namespace DefaultNamespace
{
   [ContentType("eventReward")]
   public class EventRewardWebhook : ApiContent
   {
      protected sealed override ApiVariable[] GetVariables()
      {
         return new[]
         {
            new ApiVariable
            {
               Name = "eventId", TypeName = ApiVariable.TYPE_STRING
            }
         };
      }
   }

   public class EventRewardWebhookRef : ApiRef<EventRewardWebhook> {

   }

   [Serializable]
   public class OptionalEventRewardWebhookRef : Optional<EventRewardWebhookRef> {}
}