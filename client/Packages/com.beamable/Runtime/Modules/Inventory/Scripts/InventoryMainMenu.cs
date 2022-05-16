using Beamable.UI.Scripts;
using UnityEngine;

namespace Beamable.Inventory.Scripts
{
	public class InventoryMainMenu : MenuBase
	{
		public RectTransform GroupContainer;
		public InventoryGroupUI GroupUIPrefab;
		public InventoryMenuBehaviour RootMenu;
		public InventoryMenuConfiguration Data => RootMenu.InventoryConfig;

		// Start is called before the first frame update
		void Start()
		{


		}

		public override void OnOpened()
		{
			base.OnOpened();
			RefreshGroups();

		}

		// Update is called once per frame
		void Update()
		{

		}

		void RefreshGroups()
		{
			for (var i = 0; i < GroupContainer.childCount; i++)
			{
				Destroy(GroupContainer.GetChild(i).gameObject);
			}

			foreach (var group in Data.Groups)
			{
				var gob = Instantiate(GroupUIPrefab, GroupContainer);
				gob.InventoryObjectUIPrefab = Data.ItemPreviewPrefab;
				gob.Setup(group);
			}
		}
	}
}
