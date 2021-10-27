using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Common.Api.Announcements;

namespace Beamable.Common.Player2
{

   public interface IObservable
   {
      bool IsLoading { get; }
      event Action OnUpdated;
      event Action OnLoadingStarted;
      event Action OnLoadingFinished;
      Promise Refresh();
   }

   public abstract class AbsObservable : IObservable
   {
      public bool IsLoading { get; private set; }
      public event Action OnUpdated;
      public event Action OnLoadingStarted;
      public event Action OnLoadingFinished;

      private int _lastBroadcastHashcode;

      public async Promise Refresh()
      {
         IsLoading = true;
         try
         {
            OnLoadingStarted?.Invoke();
            await PerformRefresh();
            TriggerUpdate();
         }
         finally
         {
            IsLoading = false;
            OnLoadingFinished?.Invoke();
         }
      }

      protected void TriggerUpdate()
      {
         // is the data the same as it was before?
         // we make that decision based on the hash code of the element...
         var hash = GetBroadcastHashCode();
         var isDifferent = hash != _lastBroadcastHashcode;
         _lastBroadcastHashcode = hash;

         if (isDifferent)
         {
            OnUpdated?.Invoke();
         }
      }

      protected virtual int GetBroadcastHashCode()
      {
         return GetHashCode();
      }

      protected abstract Promise PerformRefresh();
   }

   public interface IObservableReadonlyList<out T> : IReadOnlyCollection<T>, IObservable
   {
      T this[int index] { get; }
   }

   public abstract class AbsObservableReadonlyList<T> : AbsObservable, IObservableReadonlyList<T>
   {
      private List<T> _data = new List<T>(); // set to new() to avoid null
      public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

      public int Count => _data.Count;
      public T this[int index] => _data[index];

      protected override int GetBroadcastHashCode()
      {
         /*
          * We want to use a hash code based on the elements of the list at the given moment.
          */
         var res = 0x2D2816FE;
         foreach(var item in this)
         {
            res = res * 31 + (item == null ? 0 : item.GetHashCode());
         }
         return res;
      }

      protected void SetData(List<T> nextData)
      {
         _data = nextData;
      }

   }

   public class Announcement
   {

      private readonly PlayerAnnouncements _group;

      public string Id;
      public string Title;
      public string Channel;
      public string Body;

      public bool IsRead, IsClaimed, IsIgnored;

      internal Announcement(PlayerAnnouncements group)
      {
         _group = group;
      }

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

   public class PlayerAnnouncements : AbsObservableReadonlyList<Announcement>
   {
      private readonly IAnnouncementsApi _announcementsApi;

      public PlayerAnnouncements(IAnnouncementsApi announcementsApi)
      {
         _announcementsApi = announcementsApi;
      }


      protected override async Promise PerformRefresh()
      {
         // goto the swagger, and update everything...
         await PromiseBase.SuccessfulUnit; // pending an actual API...

         // end up with a list of announcement...
         var nextAnnouncements = new List<Announcement>
         {
            new Announcement(this)
            {
               Id = "abc",
               Title = "abc"
            }
         };

         SetData(nextAnnouncements);
      }

      public async Promise Read(Announcement announcement)
      {
         // TODO: represent this as a serializable action to support offline mode.

         // assume that is all going to work out.
         if (announcement.IsRead) return;

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
   }
}