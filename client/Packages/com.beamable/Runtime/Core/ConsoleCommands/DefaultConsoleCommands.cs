using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Beamable.Service;
using UnityEngine;
using UnityEngine.Scripting;

namespace Beamable.ConsoleCommands
{
    [BeamableConsoleCommandProvider]
    public class DefaultConsoleCommands
    {
        private BeamableConsole Console => ServiceManager.Resolve<BeamableConsole>();

        [Preserve]
        public DefaultConsoleCommands()
        {
        }


        [BeamableConsoleCommand("ECHO", "Repeat message to console.", "ECHO <message>")]
        private string Echo(params string[] args)
        {
            return string.Join(" ", args);
        }

        [BeamableConsoleCommand(nameof(Where), "Find where a specific console command was registered from, if it was registered with a BeamableConsoleCommand attribute", "WHERE <command>")]
        private string Where(params string[] args)
        {
            if (args.Length == 0)
            {
                return Console.Help(nameof(Where));
            }

            return Console.Origin(args[0]);
        }

    }
}