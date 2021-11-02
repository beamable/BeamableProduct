using System;
using System.Linq;
using Beamable.Common.Api.Announcements;
using Beamable.Common.Api.Notifications;
using UnityEngine;

namespace Beamable.Common.Player
{


   [Serializable]
   public class Announcement
   {
      private readonly PlayerAnnouncements _group;

      public string Id;
      public string Title;
      public string Channel;
      public string Body;

      // public AnnouncementRef ContentRef; // TODO: It would be cool to know which piece of content spawned this.

      public bool IsRead, IsClaimed, IsIgnored;


      internal Announcement(PlayerAnnouncements group)
      {
         _group = group;
      }

      // TODO: _could_ have custom editor tooling to perform this method.
      public Promise Read() => _group.Read(this);


      #region auto generated equality members
      private bool Equals(Announcement other)
      {
         return Id == other.Id && Title == other.Title && Channel == other.Channel && Body == other.Body && IsRead == other.IsRead && IsClaimed == other.IsClaimed && IsIgnored == other.IsIgnored;
      }

      public override bool Equals(object obj)
      {
         if (ReferenceEquals(null, obj)) return false;
         if (ReferenceEquals(this, obj)) return true;
         if (obj.GetType() != this.GetType()) return false;
         return Equals((Announcement) obj);
      }

      public override int GetHashCode()
      {
         unchecked
         {
            var hashCode = (Id != null ? Id.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Title != null ? Title.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Channel != null ? Channel.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Body != null ? Body.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ IsRead.GetHashCode();
            hashCode = (hashCode * 397) ^ IsClaimed.GetHashCode();
            hashCode = (hashCode * 397) ^ IsIgnored.GetHashCode();
            return hashCode;
         }
      }
      #endregion
   }

   [Serializable]
   public class PlayerAnnouncements : AbsObservableReadonlyList<Announcement>
   {
      private readonly IAnnouncementsApi _announcementsApi;
      private readonly INotificationService _notifications;

      public PlayerAnnouncements(IAnnouncementsApi announcementsApi, INotificationService notifications)
      {
         _announcementsApi = announcementsApi;
         _notifications = notifications;

         // TODO: How do we handle multiple users? The dependencies here have the user id baked into them?
         // TODO: How do we handle user sign out?

         _notifications.Subscribe(_notifications.GetRefreshTokenForService("announcements"), HandleSubscriptionUpdate);

         var _ = Refresh(); // automatically start.
      }

      private void HandleSubscriptionUpdate(object raw)
      {
         var _ = Refresh(); // fire-and-forget.
      }

      protected override async Promise PerformRefresh()
      {
         // end up with a list of announcement...
         var data = await _announcementsApi.GetCurrent();

         var nextAnnouncements = data.announcements.Select(view => new Announcement(this)
         {
            // TODO: fill in rest of properties.
            Id = view.id,
            Title = view.title,
            Body = view.body,
            Channel = view.channel,
            IsRead = view.isRead,
            IsClaimed = view.isClaimed,
         }).ToList();

         SetData(nextAnnouncements);
      }

      public async Promise Read(Announcement announcement)
      {
         // TODO: represent this as a serializable action to support offline mode.

         // assume that is all going to work out.
         if (announcement.IsRead) return;

         // update state right away,
         // then run a network call to verify
         // then reconcile the result of the network call
         // if the call fails, then revert the state

         announcement.IsRead = true;
         try
         {
            await _announcementsApi.MarkRead(announcement.Id);
            TriggerUpdate();
         }
         catch
         {
            announcement.IsRead = false;
            throw;
         }
      }

      [Serializable]
      private class ReadAction : ISDKAction
      {
         public Announcement Announcement;

         public void Predict()
         {

         }

         public Promise Execute()
         {
         }

         public void Reconcile()
         {
         }
      }
   }
}