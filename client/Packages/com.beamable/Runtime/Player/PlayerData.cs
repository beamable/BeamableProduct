using Beamable.Common.Player;
using UnityEngine;

namespace Beamable.Player
{
   [System.Serializable]
   public class PlayerData
   {
      private readonly IBeamableAPI _api;

      // Lazy initialization of services.
      [SerializeField]
      private PlayerAnnouncements _announcements;

      public PlayerAnnouncements Announcements =>
         (_announcements?.IsInitialized ?? false) ? _announcements : (_announcements = new PlayerAnnouncements(_api.AnnouncementService,
            _api.NotificationService,
            _api.SdkEventService));

      [SerializeField]
      private PlayerCurrencyGroup _currencyGroup;

      public PlayerCurrencyGroup Currencies => (_currencyGroup?.IsInitialized ?? false) ? _currencyGroup : (_currencyGroup =
         new PlayerCurrencyGroup(_api.InventoryService, _api.NotificationService, _api.SdkEventService));

      public PlayerData(IBeamableAPI api)
      {
         _api = api;
      }
   }
}