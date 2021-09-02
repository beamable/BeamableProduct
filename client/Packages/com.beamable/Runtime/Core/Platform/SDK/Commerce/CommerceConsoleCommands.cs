using UnityEngine;
using Beamable.Service;
using Beamable.ConsoleCommands;
using UnityEngine.Scripting;

namespace Beamable.Api.Commerce
{
    [BeamableConsoleCommandProvider]
    public class CommerceConsoleCommands
    {
        private BeamableConsole Console => ServiceManager.Resolve<BeamableConsole>();

        [Preserve]
        public CommerceConsoleCommands()
        {
        }

        [BeamableConsoleCommand("STORE-VIEW", "Outputs the player view of a specific store to the console (in json)", "STORE-VIEW stores.default")]
        protected string PreviewCurrency(params string[] args)
        {
            string store;
            if(args.Length > 0)
            {
                store = args[0];
            } else
            {
                return "Please specify the content id of the store.";
            }

            var platform = ServiceManager.Resolve<PlatformService>();

            platform.Commerce.GetCurrent(store).Then(response =>
            {
                string json = JsonUtility.ToJson(response);
                Debug.Log($"Player Store View: {json}");
            });

            return "Fetching player store view...";
        }
    }

}