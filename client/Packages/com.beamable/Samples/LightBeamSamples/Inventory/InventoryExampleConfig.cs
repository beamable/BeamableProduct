using UnityEngine;

[CreateAssetMenu(menuName = "Beamable/Examples/Inventory Example Config")]
public class InventoryExampleConfig : ScriptableObject
{
	public HomePage homePage;
	public ItemDisplayBehaviour itemDisplay;
	public CurrencyDisplayBehaviour currencyDisplay;
	public CurrencyInfoBehaviour currencyInfo;
	public ItemInfoPage itemPage;
	public PropertyDisplayBehaviour propertyDisplay;
}
