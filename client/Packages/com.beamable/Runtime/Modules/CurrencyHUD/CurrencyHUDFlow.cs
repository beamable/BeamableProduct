using System.Collections;
using Beamable.Common.Inventory;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Inventory;
using Beamable.Service;
using Beamable.UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;


namespace Beamable.CurrencyHUD

{
    [HelpURL(BeamableConstants.URL_FEATURE_CURRENCY_HUD)]
    public class CurrencyHUDFlow : MonoBehaviour
    {
        public CurrencyRef content;
        public BeamableDisplayModule displayModule;
        public RawImage img;
        public TextMeshProUGUI txtAmount;
        private long targetAmount = 0;
        private long currentAmount = 0;
        readonly WaitForSeconds _waitForSeconds = new WaitForSeconds(0.02f);

        void Awake()
        {
            displayModule.SetVisible(false);
        }

        private async void Start()
        {
            var de = await API.Instance;
            de.InventoryService.Subscribe(content.Id, view =>
            {
                view.currencies.TryGetValue(content.Id, out targetAmount);
                ServiceManager.Resolve<CoroutineService>().StartCoroutine(DisplayCurrency());
            });
            var currency = await content.Resolve();
            var currencyAddress = currency.icon;
            img.texture = await currencyAddress.LoadTexture();
            displayModule.SetVisible();
        }

        private IEnumerator DisplayCurrency()
        {
            long deltaTotal = targetAmount - currentAmount;
            long deltaStep = deltaTotal / 50;

            if (deltaStep == 0)
            {
                deltaStep = deltaTotal < 0 ? -1 : 1;
            }

            while (currentAmount != targetAmount)

            {
                currentAmount += deltaStep;

                if (deltaTotal > 0 && currentAmount > targetAmount)

                {
                    currentAmount = targetAmount;
                }

                else if (deltaTotal < 0 && currentAmount < targetAmount)

                {
                    currentAmount = targetAmount;
                }


                txtAmount.text = currentAmount.ToString();
                yield return _waitForSeconds;
            }
        }
    }
}