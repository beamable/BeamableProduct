using Beamable.Common.Inventory;
using Beamable.UI.Scripts;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Beamable.Common.Constants.URLs;

namespace Beamable.CurrencyHUD
{
	[HelpURL(Documentations.URL_DOC_CURRENCY_HUD)]
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
			var ctx = BeamContext.InParent(this);

			ctx.Inventory.GetCurrency(content).OnAmountUpdated += amount =>
			{
				targetAmount = amount;
				ctx.CoroutineService.StartCoroutine(DisplayCurrency());
			};

			var currency = await content.Resolve();
			var currencyAddress = currency.icon;
			var isValid = currencyAddress.IsValid();
			if(isValid)
			{
				img.texture = await currencyAddress.LoadTexture();
			}
			else
			{
				img.texture = null;
#if UNITY_EDITOR
				if (!isValid && currencyAddress.editorAsset != null)
				{
					var path = UnityEditor.AssetDatabase.GetAssetPath(currencyAddress.editorAsset);
					Debug.LogError($"Currency icon is not an Addressable asset, check asset at path: <b>{path}</b>. Setting sprite image to <b>null</b> for now.", this);
				}
#endif
			}
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
