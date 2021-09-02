using UnityEngine.Purchasing.Extension;

namespace Beamable.Purchasing.Steam
{
    public class SteamPurchasingModule : AbstractPurchasingModule, IStoreConfiguration
    {
        public override void Configure()
        {
            RegisterStore(SteamStore.Name, new SteamStore());
        }
    }
}

