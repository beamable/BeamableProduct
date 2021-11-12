using System;
using System.Collections.Generic;
using Beamable.Common.Inventory;
using UnityEngine;

namespace Beamable.Common.Content
{
   [Serializable]
   public class PlayerReward
   {
      [ContentField("description")]
      public OptionalString description;

      [ContentField("changeCurrencies")]
      public OptionalCurrencyChangeList currencies;

      [ContentField("addItems")]
      public OptionalNewItemList items;
   }

   [Serializable]
   public class PlayerReward<TOptionalApiRewards> : PlayerReward
   {
      [ContentField("callWebhooks")]
      [HideUnlessServerPackageInstalled]
      public TOptionalApiRewards webhooks;
   }

}