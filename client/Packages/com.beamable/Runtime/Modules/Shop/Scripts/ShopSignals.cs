using System;
using Beamable.AccountManagement;
using Beamable.Signals;
using Beamable.Api.Payments;

namespace Beamable.Shop
{
   [System.Serializable]
   public class StoreListingEvent : DeSignal<PlayerListingView>
   {

   }

   [System.Serializable]
   public class StoreRenderEvent : DeSignal<PlayerStoreView>
   {

   }

   public class ShopSignals : DeSignalTower
   {
      public ToggleEvent OnToggle;
      public StoreListingEvent OnPurchase;

      private static bool _toggleState;

      public static bool ToggleState => _toggleState;


      private void Broadcast<TArg>(TArg arg, Func<ShopSignals, DeSignal<TArg>> getter)
      {
         this.BroadcastSignal(arg, getter);
      }

      public void Toggle(bool desiredState)
      {
         if (desiredState == ToggleState) return;

         _toggleState = desiredState;

         Broadcast(_toggleState, s => s.OnToggle);

      }
   }
}