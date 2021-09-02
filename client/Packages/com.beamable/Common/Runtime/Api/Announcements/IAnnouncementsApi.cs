using System;
using System.Collections.Generic;
using Beamable.Common.Announcements;

namespace Beamable.Common.Api.Announcements
{
   public interface IAnnouncementsApi : ISupportsGet<AnnouncementQueryResponse>
   {
      Promise<EmptyResponse> MarkRead(string id);
      Promise<EmptyResponse> MarkRead(List<string> ids);
      Promise<EmptyResponse> MarkDeleted(string id);
      Promise<EmptyResponse> MarkDeleted(List<string> ids);
      Promise<EmptyResponse> Claim(string id);
      Promise<EmptyResponse> Claim(List<string> ids);
   }


   [Serializable]
   public class AnnouncementQueryResponse
   {
      public List<AnnouncementView> announcements;
   }

   [Serializable]
   public class AnnouncementView : CometClientData
   {
      public string id;
      public string channel;
      public string startDate;
      public string endDate;
      public long secondsRemaining;
      public DateTime endDateTime;
      public string title;
      public string summary;
      public string body;
      public List<AnnouncementAttachment> attachments;
      public bool isRead;
      public bool isClaimed;

      public bool HasClaimsAvailable()
      {
         return !isClaimed && attachments.Count > 0;
      }

      internal void Init()
      {
         endDateTime = DateTime.UtcNow.AddSeconds(secondsRemaining);
      }
   }


   [Serializable]
   public class AnnouncementRequest
   {
      public List<string> announcements;

      public AnnouncementRequest(List<string> announcements)
      {
         this.announcements = announcements;
      }
   }
}