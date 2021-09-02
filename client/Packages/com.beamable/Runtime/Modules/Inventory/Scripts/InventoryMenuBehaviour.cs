using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Api;
using Beamable.Common;
using Beamable.UI.Scripts;
using Beamable.Platform.SDK;
using Beamable.Api.Inventory;
using Beamable.Common.Api.Inventory;
using UnityEngine;

namespace Beamable.Inventory.Scripts
{
   [HelpURL(BeamableConstants.URL_FEATURE_INVENTORY_FLOW)]
   public class InventoryMenuBehaviour : MonoBehaviour
    {
        public MenuManagementBehaviour MenuManager;

        private Promise<PlatformSubscription<InventoryView>> _inventorySubscription;

        private Promise<Unit> _inventoryViewPromise = new Promise<Unit>();
        private InventoryView _inventoryView;

        public void HandleToggle(bool shouldShow)
        {
            if (!shouldShow && MenuManager.IsOpen)
            {
                MenuManager.CloseAll();
            }
            else if (shouldShow && !MenuManager.IsOpen)
            {
                MenuManager.Show<InventoryMainMenu>();
            }
        }

        void Start()
        {
        }

        private void OnDestroy()
        {
            //_inventorySubscription.Then(s => s.Unsubscribe());
        }

        void HandleInventoryEvent(InventoryView inventory)
        {
            _inventoryView = inventory;
            _inventoryViewPromise.CompleteSuccess(PromiseBase.Unit);
        }
    }
}