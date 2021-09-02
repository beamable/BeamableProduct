using Beamable.UI.Scripts;
using Beamable.Api.Inventory;
using UnityEngine;

namespace Beamable.Inventory.Scripts
{
    public class InventoryMainMenu : MenuBase
    {
        public RectTransform GroupContainer;
        public InventoryGroupUI GroupUIPrefab;

        // Start is called before the first frame update
        void Start()
        {


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

            foreach (var group in InventoryConfiguration.Instance.Groups)
            {
                var gob = Instantiate(GroupUIPrefab, GroupContainer);
                gob.Setup(group);
            }
        }
    }
}
