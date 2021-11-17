using Beamable.Common.Inventory;
using Beamable.Coroutines;
using Beamable.Service;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.CurrencyHUD
{
	[HelpURL(BeamableConstants.URL_FEATURE_CURRENCY_HUD)]
	public class CurrencyHUDFlow : MonoBehaviour
	{
		public CurrencyRef content;
		public Canvas canvas;
		public RawImage img;
		public TextMeshProUGUI txtAmount;
		private long targetAmount;
		private long currentAmount;

		private void Awake()
		{
			canvas.enabled = false;
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
			await img.SetTexture(currencyAddress);

			canvas.enabled = true;
		}

		private IEnumerator DisplayCurrency()
		{
			var deltaTotal = targetAmount - currentAmount;
			var deltaStep = deltaTotal / 50;
			if (deltaStep == 0)
				deltaStep = deltaTotal < 0 ? -1 : 1;
			while (currentAmount != targetAmount)
			{
				currentAmount += deltaStep;
				if (deltaTotal > 0 && currentAmount > targetAmount)
					currentAmount = targetAmount;
				else if (deltaTotal < 0 && currentAmount < targetAmount)
					currentAmount = targetAmount;
				txtAmount.text = currentAmount.ToString();
				yield return new WaitForSeconds(0.02f);
			}
		}
	}
}
