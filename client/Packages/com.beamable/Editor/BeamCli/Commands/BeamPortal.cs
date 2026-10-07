
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class PortalArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Open the new Beamable portal (console) instead of the legacy portal. Combine with --portal-url to open another deployment, e.g. http://localhost:4950</summary>
        public bool console;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the console value was not default, then add it to the list of args.
            if ((this.console != default(bool)))
            {
                genBeamCommandArgs.Add(("--console=" + this.console));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual PortalWrapper Portal(PortalArgs portalArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("portal");
            genBeamCommandArgs.Add(portalArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            PortalWrapper genBeamCommandWrapper = new PortalWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class PortalWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
