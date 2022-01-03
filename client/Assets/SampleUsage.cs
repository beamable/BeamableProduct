// using UnityEngine;
// using Beamable;
// using Beamable.Api.Sessions;
// using System.Collections.Generic;
// using System.Linq;
// using TMPro;
//
// namespace DefaultNamespace
// {
//    public class SampleUsage : MonoBehaviour
//    {
// 	   public BeamContext beamable;
//
// 	   public TextMeshProUGUI DebugText;
//
// 	   public int _announcementReadIndex;
//
//       void Start()
//       {
//
//
// 	      /*
// 	       * TODO list
// 	       * -. test offline mode for start-up, and for resourceful sdks
// 	       * -. implement inventory items resourceful sdk
// 	       * -. content with multi context?
// 	       * -. fully make api interface work (events, and device users)
// 	       * -.
// 	       * -. handle resourceful configuration
// 	       * -. implement C#-stack requester
// 	       * -. think about how the game-relay stuff can improve based on this
// 	       * -. events
// 	       * -. leaderboards
// 	       * -. tournaments
// 	       * -. groups
// 	       * -.
// 	       */
//
// 	      beamable = BeamContext.ForContext(this);
//
// 	      var api = beamable.Api;
//
// 	      beamable.Announcements.OnLoadingFinished += () => Debug.Log("Loading announcements finished");
// 	      beamable.Announcements.OnLoadingStarted += () => Debug.Log("Loading announcements started");
// 	      beamable.Announcements.OnUpdated += Print;
// 	      beamable.Announcements.OnDataUpdated += announcements =>
// 		      Debug.Log($"there are now {announcements.Count} announcements ");
//
// 	      beamable.Currencies.OnLoadingFinished += () => Debug.Log("Loading currency finished");
// 	      beamable.Currencies.OnLoadingStarted += () => Debug.Log("Loading currency started");
// 	      beamable.Currencies.OnUpdated += Print;
// 	      beamable.Currencies.OnDataUpdated += currencies =>
// 		      Debug.Log($"there are now {currencies.Count} currencies ");
//
// 	      beamable.OnReloadUser += () => {
// 		      beamable.Api.Stats.SetStats("public", new Dictionary<string, string> {["tuna"] = "abc"})
// 		              .Then(res => Debug.Log("Stats were set"));
// 		      beamable.Api.Stats.GetStats("client", "public", "player", beamable.PlayerId).Then(res => {
// 			      Debug.Log("Stats came back for player!");
// 		      });
// 	      };
//
// 	      beamable.Stats.OnDataUpdated += stats => {
// 		      Debug.Log($"Player stats updated! {beamable.PlayerId}");
// 		      foreach (var kvp in stats)
// 		      {
// 			      Debug.Log($"k=[{kvp.Key}], v=[{kvp.Value}]");
// 		      }
// 	      };
//
//
//       }
//
//       private void Update()
//       {
// 	      var currencyMessages = string.Join(
// 		      "\n",
// 		      beamable.Currencies.Select(
// 			      currency => $"currency: {currency.CurrencyId} - {currency.Amount}"));
// 	      var announcementMessages = string.Join(
// 		      "\n",
// 		      beamable.Announcements.Select(
// 			      announcement => $"announce: {announcement.Id} - {announcement.IsRead}/{announcement.IsClaimed}"));
// 	      var time = beamable.ServiceProvider.GetService<ISessionService>().TimeSinceLastSessionStart;
//
// 	      DebugText.text = $"{time}\n{beamable.PlayerId}\n\nCurrencies\n{currencyMessages}\n\nAnnouncements\n{announcementMessages}";
//       }
//
//       [ContextMenu("print")]
//       void Print()
//       {
//          Debug.Log($"user=[{beamable.PlayerId}] Printing {beamable.Announcements.Count} Announcements");
//          foreach (var announcement in beamable.Announcements)
//          {
//             Debug.Log($"[{announcement.Id}] {announcement.Title} / read=[{announcement.IsRead}] claimed=[{announcement.IsClaimed}]");
//          }
//
//          Debug.Log($"user=[{beamable.PlayerId}] Printing {beamable.Currencies.Count} Currencies");
//          foreach (var currency in beamable.Currencies)
//          {
// 	         Debug.Log($"[{currency.CurrencyId}] {currency.Amount} ");
//          }
//       }
//
//       [ContextMenu("read")]
//       async void Read()
//       {
//          await beamable.Announcements[_announcementReadIndex].Read();
//          Print();
//       }
//       [ContextMenu("claim")]
//       async void Claim()
//       {
// 	      await beamable.Announcements[_announcementReadIndex].Claim();
// 	      Print();
//       }
//
//       public string nextTunavalue;
//       [ContextMenu("set tuna")]
//       async void SetStat()
//       {
// 	      await beamable.Stats.Set("tuna2", nextTunavalue);
// 	      // var service = beamable.CacheDependentMS();
// 	      // beamable.Microservices().GetClient<CacheDependentMSClient>()
// 	      //
// 	      // var client = new CacheDependentMSClient(beamable);
// 	      // beamable.Microservices().CacheDependentMS().GetCachedView();
// 	      Debug.Log("The stat has been updated");
//       }
//
//       public long otherDbid;
//
//    }
// }
