
using UnityEngine;

namespace Beamable.Player
{
	[System.Serializable]
	public class PlayerData
	{
		public BeamContext Ctx { get; }


		// [SerializeField]
		// private PlayerCurrencyGroup _currencyGroup;
		//
		// public PlayerCurrencyGroup Currencies => (_currencyGroup?.IsInitialized ?? false) ? _currencyGroup : (_currencyGroup =
		//    new PlayerCurrencyGroup(_api.InventoryService, _api.NotificationService, _api.SdkEventService));

		public PlayerData(BeamContext ctx)
		{
			Ctx = ctx;
		}
	}
}
