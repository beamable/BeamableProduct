// using Beamable.Common.Player;
// using Beamable.Player;
// using UnityEngine;
//
// namespace DefaultNamespace
// {
//    public class PlayerSDKTesting : MonoBehaviour
//    {
//       public PlayerData Player;
//       public int index;
//
//       async void Start()
//       {
//          var b = await Beamable.API.Instance;
//          Player = b.Player;
//
//          //Player.Currencies.Add("currency.gems", 2)
//
//          Player.Announcements.OnUpdated += AnnouncementsOnOnUpdated;
//       }
//
//       private void CurrenciesOnOnUpdated()
//       {
//          Debug.Log("Updated currencies!");
//          foreach (var currency in Player.Currencies)
//          {
//             Debug.Log($"  {currency.CurrencyId} = {currency.Amount}");
//          }
//       }
//
//       private void AnnouncementsOnOnUpdated()
//       {
//          Debug.Log("Announcements updated:");
//          foreach (var announcement in Player.Announcements)
//          {
//             Debug.Log($"  {announcement.Id} - title=[{announcement.Title}] read=[{announcement.IsRead}]");
//          }
//       }
//
//       [ContextMenu("Listen for Currency")]
//       public void ListenForCurrency()
//       {
//          Debug.Log("starting listen operation");
//          Player.Currencies.OnUpdated += CurrenciesOnOnUpdated;
//       }
//
//       [ContextMenu("Add some gems")]
//       public async void AddGems()
//       {
//          await Player.Currencies.Add("currency.gems", 3);
//          Debug.Log("The add is over");
//       }
//
//       [ContextMenu("Read at Index")]
//       public async void ReadIndex()
//       {
//          Debug.Log($"Starting to read {index} / {Player.Announcements.Count}");
//          await Player.Announcements[index].Read();
//          Debug.Log("read finished");
//       }
//
//       void Update()
//       {
//          // gameobject.text = Player.Currency[0].Amount
//       }
//    }
// }
