using Beamable.Common.Announcements;
using Beamable.Common.Content.Validation;
using System;
using Beamable.Common.Inventory;
using UnityEngine;

namespace Beamable.Common.Content
{
   [Serializable]
   public class PlayerReward
   {
      [ContentField("description")]
      [Tooltip("An optional textural description of the reward. Use this to quickly summarize for development what the reward grants.")]
      public OptionalString description;

      [ContentField("changeCurrencies")]
      [Tooltip("Optionally, each reward can grant a set of currencies. ")]
      public OptionalCurrencyChangeList currencies;

      [ContentField("addItems")]
      [Tooltip("Optionally, each reward can grant a set of items with properties. ")]
      public OptionalNewItemList items;

      [ContentField("applyVipBonus")]
      [Tooltip("Optionally, when a reward is claimed, the vip bonus can be applied to the currenices")]
      public OptionalBoolean applyVipBonus;
   }

   [Serializable]
   public class PlayerReward<TOptionalApiRewards> : PlayerReward
   {
      [ContentField("callWebhooks")]
      [HideUnlessServerPackageInstalled]
      public TOptionalApiRewards webhooks;
   }

}
