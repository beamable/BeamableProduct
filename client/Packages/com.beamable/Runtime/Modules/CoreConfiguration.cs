using Beamable.Common;
using UnityEngine;

namespace Beamable
{
    [CreateAssetMenu(
        fileName="CoreConfiguration",
        menuName= BeamableConstants.MENU_ITEM_PATH_ASSETS_BEAMABLE_CONFIGURATIONS + "/" +
                  "Core Configuration")]
    public class CoreConfiguration : ModuleConfigurationObject
    {
        public static CoreConfiguration Instance => Get<CoreConfiguration>();

        public enum EventHandlerConfig { Guarantee, Replace, Add,  }

        [Tooltip("By default, Beamable gives you a default uncaught promise exception handler.\n\n" +
                 "You can set your own via PromiseBase.SetPotentialUncaughtErrorHandler.\n\n" +
                 "In Beamable's Initialization, we guarantee, replace or add our default handler based on this configuration.\n\n" +
                 "- Guarantee => Guarantees at least Beamable's default handler will exist if you don't configure one yourself\n" +
                 "- Replace => Replaces any handler configured with Beamable's default handler.\n" +
                 "- Add => Adds Beamable's default handler to the list of handlers, but keep existing handlers configured.\n")]
        public EventHandlerConfig DefaultUncaughtPromiseHandlerConfiguration;

        public enum OfflineStrategy { Optimistic, Disable }

        [Tooltip("By default, when your player isn't connected to the internet, Beamable will accrue inventory writes " +
                 "in a buffer and optimistically simulate the effects locally in memory. When your player comes back " +
                 "online, the buffer will be replayed. If this isn't desirable, you should disable the feature.")]
        public OfflineStrategy InventoryOfflineMode = OfflineStrategy.Optimistic;
    }
}
