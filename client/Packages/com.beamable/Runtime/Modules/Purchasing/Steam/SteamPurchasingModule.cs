// Unity IAP v5 removed AbstractPurchasingModule / IStoreConfiguration; the v5 Steam store is
// registered through UnityIAPServices in UnityBeamablePurchaser instead of this module.
#if !UNITY_PURCHASING_5_OR_NEWER
using Beamable.Common.Dependencies;
using UnityEngine.Purchasing.Extension;

namespace Beamable.Purchasing.Steam
{
	public class SteamPurchasingModule : AbstractPurchasingModule, IStoreConfiguration
	{
		private readonly IDependencyProvider _provider;

		public SteamPurchasingModule(IDependencyProvider provider)
		{
			_provider = provider;
		}
		public override void Configure()
		{
			RegisterStore(SteamStore.Name, new SteamStore(_provider));
		}
	}
}
#endif

