using Beamable.Common.Inventory;
using Beamable.Content;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Inventory.Scripts
{

	[System.Serializable]
	public struct InventoryGroup
	{
		public ItemRef ItemRef;
		public string DisplayName;
	}

#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(
	   fileName = "Inventory Configuration",
	   menuName = BeamableConstantsOLD.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
	   "Inventory Configuration",
	   order = BeamableConstantsOLD.MENU_ITEM_PATH_ASSETS_BEAMABLE_ORDER_1)]
#endif
	public class InventoryConfiguration : ModuleConfigurationObject
	{
		public static InventoryConfiguration Instance => Get<InventoryConfiguration>();

		public List<InventoryGroup> Groups;

		public InventoryObjectUI DefaultObjectPrefab;
	}
}
