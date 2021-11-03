using System;
using System.Linq;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Inventory;
using UnityEngine;

namespace Beamable.Common.Player
{
   [Serializable]
   public class PlayerCurrency
   {

      public string CurrencyId;
      public long Amount;
      // TODO: add the currencyRef? As a computed property of CurrencyId probably.

      #region Auto Generated Equality Members
      protected bool Equals(PlayerCurrency other)
      {
         return CurrencyId == other.CurrencyId && Amount == other.Amount;
      }

      public override bool Equals(object obj)
      {
         if (ReferenceEquals(null, obj)) return false;
         if (ReferenceEquals(this, obj)) return true;
         if (obj.GetType() != this.GetType()) return false;
         return Equals((PlayerCurrency) obj);
      }

      public override int GetHashCode()
      {
         unchecked
         {
            return ((CurrencyId != null ? CurrencyId.GetHashCode() : 0) * 397) ^ Amount.GetHashCode();
         }
      }
      #endregion

   }

   [Serializable]
   public class PlayerCurrencyGroup : AbsObservableReadonlyList<PlayerCurrency>
   {
      private readonly IInventoryApi _inventoryApi;
      private readonly INotificationService _notificationService;
      private readonly ISdkEventService _sdkEventService;

      public PlayerCurrencyGroup(IInventoryApi inventoryApi, INotificationService notificationService, ISdkEventService sdkEventService)
      {
         _inventoryApi = inventoryApi;
         _notificationService = notificationService;
         _sdkEventService = sdkEventService;

         _sdkEventService.Register(nameof(PlayerCurrencyGroup), HandleEvent);
         notificationService.Subscribe(notificationService.GetRefreshTokenForService("inventory"), HandleSubscriptionUpdate);

         Debug.Log("Currency service start...");
         var _ = Refresh(); // automatically start.
         IsInitialized = true;
      }


      private async Promise HandleEvent(SdkEvent evt)
      {
         switch (evt.Event)
         {
            case "add":

               // TODO: pull out into separate method
               var currencyId = evt.Args[0];
               var amount = long.Parse(evt.Args[1]);
               var currency = GetCurrency(currencyId);
               // currency.Amount += amount;
               // TriggerUpdate();
               try
               {
                  await _inventoryApi.AddCurrency(currencyId, amount);
               }
               finally
               {
                  TriggerUpdate();
               }

               break;
            default:
               throw new Exception($"Unhandled event: {evt.Event}");
         }
      }

      private void HandleSubscriptionUpdate(object raw)
      {
         var _ = Refresh(); // fire-and-forget.
      }

      protected override async Promise PerformRefresh()
      {
         var data = await _inventoryApi.GetCurrencies(new string[] { });

         var nextCurrencies = data.Select(kvp => new PlayerCurrency
         {
            CurrencyId = kvp.Key,
            Amount = kvp.Value
         }).ToList();
         SetData(nextCurrencies);
      }

      public PlayerCurrency GetCurrency(string id) => this.FirstOrDefault(a => string.Equals(a.CurrencyId, id));


      public Promise Add(string currencyId, long amount) =>
         _sdkEventService.Add(new SdkEvent(nameof(PlayerCurrencyGroup), "add", currencyId, amount.ToString()));
   }
}