using System;
using Beamable.Common.Inventory;

namespace Beamable.Common.Content
{
   [Serializable]
   public class PlayerReward
   {
      public OptionalString description;

      public OptionalCurrencyChangeList currencies;
      public OptionalNewItemList items;
   }
}