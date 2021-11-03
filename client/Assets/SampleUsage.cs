using Beamable.Common.Player;
using UnityEngine;

namespace DefaultNamespace
{
   public class SampleUsage : MonoBehaviour
   {
      public PlayerAnnouncements Announcements;

      async void Start()
      {
         var b = await Beamable.API.Instance; // TODO: how does changing use accounts work?
         Announcements = new PlayerAnnouncements(b.AnnouncementService, b.NotificationService, b.SdkEventService);

         Announcements.OnLoadingFinished += () => Debug.Log("Loading finished");
         Announcements.OnLoadingStarted += () => Debug.Log("Loading started");
         Announcements.OnUpdated += OnUpdated;
      }

      private void OnUpdated()
      {
         Debug.Log("updated!");
         Print();
      }

      [ContextMenu("Refresh")]
      void Refresh()
      {
         Debug.Log("Refreshing...");
         Announcements.Refresh();
      }

      [ContextMenu("print")]
      void Print()
      {
         Debug.Log($"Printing {Announcements.Count} Announcements");
         foreach (var announcement in Announcements)
         {
            Debug.Log($"[{announcement.Id}] {announcement.Title} / read=[{announcement.IsRead}]");
         }
      }

      [ContextMenu("read")]
      async void ReadFirst()
      {
         await Announcements[0].Read();
         Print();
      }
   }
}