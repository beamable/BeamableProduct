using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Coroutines;
using Beamable.Platform.SDK;
using Beamable.Platform.SDK.Auth;
using Beamable.Api;
using Beamable.Common.Api.Announcements;
using Beamable.AccountManagement;
using Beamable.UI.Layouts;
using Beamable.UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.Announcements
{
   public class AnnouncementMainMenu : MenuBase
   {
      public AnnouncementSummary SummaryPrefab;
      public Transform AnnouncementList;
      private PlatformSubscription<AnnouncementQueryResponse> Subscription;
      private List<AnnouncementView> Announcements;

      protected async void Start()
      {
         var de = await API.Instance;
         Subscription = PlatformSubscribableExtensions.Subscribe(de.AnnouncementService, announcements =>
         {
            Announcements = announcements.announcements;

            // Clear all data
            for (var i = 0; i < AnnouncementList.childCount; i++)
            {
               Destroy(AnnouncementList.GetChild(i).gameObject);
            }

            // Populate summaries
            foreach (var announcement in Announcements)
            {
               var summary = Instantiate(SummaryPrefab, AnnouncementList);
               summary.Apply(Manager, announcement);
            }
         });
      }

      private void OnDestroy()
      {
         Subscription.Unsubscribe();
      }

      public async void OnReadAll()
      {
         List<string> ids = new List<string>();
         foreach (var announcement in Announcements)
         {
            ids.Add(announcement.id);
         }

         var de = await API.Instance;
         await de.AnnouncementService.MarkRead(ids);
      }

      public async void OnClaimAll()
      {
         List<string> ids = new List<string>();
         foreach (var announcement in Announcements)
         {
            ids.Add(announcement.id);
         }

         var de = await API.Instance;
         await de.AnnouncementService.Claim(ids);
      }

      public async void OnDeleteAll()
      {
         List<string> ids = new List<string>();
         foreach (var announcement in Announcements)
         {
            ids.Add(announcement.id);
         }

         var de = await API.Instance;
         await de.AnnouncementService.MarkDeleted(ids);
      }
   }
}