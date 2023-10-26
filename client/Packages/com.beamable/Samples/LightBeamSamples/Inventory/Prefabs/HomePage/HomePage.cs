using Beamable.Common;
using Beamable.Player;
using Beamable.Runtime.LightBeams;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomePage : MonoBehaviour, ILightComponent
{
	[Header("Scene References")]
	public Transform itemsContainer;
	public GameObject itemsParent;
	public Transform currenciesContainer;
	public GameObject currenciesParent;
	public Button showCurrencies;
	public Button showItems;
	public TextMeshProUGUI playerId;

	private LightBeam _ctx;
	
	public async Promise OnInstantiated(LightBeam ctx)
	{
		_ctx = ctx;
		ClearAllData();

		playerId.text = $"Player Id: {ctx.BeamContext.PlayerId}";
		Debug.Log(playerId.text);

		await ShowAllItems();
		
		showCurrencies.HandleClicked(async () =>
		{
			await ShowAllCurrencies();
		});
		
		showItems.HandleClicked(async () =>
		{
			await ShowAllItems();
		});
	}

	//TODO: change all of this to Promise
	private async Task ShowAllItems()
	{
		ClearAllData();
		itemsParent.SetActive(true);
		
		var items = _ctx.BeamContext.Inventory.GetItems();
		await items.Refresh();

		foreach (var item in items)
		{
			await _ctx.Instantiate<ItemDisplayBehaviour, PlayerItem>(itemsContainer, item);
		}
	}

	private async Task ShowAllCurrencies()
	{
		ClearAllData();
		currenciesParent.SetActive(true);
		
		var currencies = _ctx.BeamContext.Inventory.GetCurrencies();
		await currencies.Refresh();
		
		foreach (var currency in currencies)
		{
			await _ctx.Instantiate<CurrencyDisplayBehaviour, PlayerCurrency>(currenciesContainer, currency);
		}
	}

	private void ClearAllData()
	{
		itemsContainer.Clear();
		itemsParent.SetActive(false);
		currenciesContainer.Clear();
		currenciesParent.SetActive(false);
	}
}
