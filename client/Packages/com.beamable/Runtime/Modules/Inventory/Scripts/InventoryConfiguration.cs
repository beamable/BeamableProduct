using System.Collections.Generic;
using Beamable.Common.Inventory;
using Beamable.Content;
using UnityEngine;

namespace Beamable.Inventory.Scripts
{

   [System.Serializable]
   public struct InventoryGroup
   {
      public ItemRef ItemRef;
      public string DisplayName;
   }

   [CreateAssetMenu(
      fileName = "Inventory Configuration",
      menuName = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
      "Inventory Configuration",
      order = BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1)]
   public class InventoryConfiguration : ModuleConfigurationObject
   {
      public static InventoryConfiguration Instance => Get<InventoryConfiguration>();

      public List<InventoryGroup> Groups;

      public InventoryObjectUI DefaultObjectPrefab;
   }
}