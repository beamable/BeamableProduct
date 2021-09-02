using System.Collections.Generic;
using Beamable.Common.Shop;
using Beamable.Content;
using UnityEngine;

namespace Beamable.Shop
{
   [CreateAssetMenu(
      fileName = "Shop Configuration",
      menuName = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
      "Shop Configuration",
      order = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1)]
   public class ShopConfiguration : ModuleConfigurationObject
   {
      public static ShopConfiguration Instance => Get<ShopConfiguration>();

      public List<StoreRef> Stores = new List<StoreRef>();
      public ListingRenderer ListingRenderer;
      public ObtainRenderer ObtainRenderer;
   }
}