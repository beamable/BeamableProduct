
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class PortalExtensionUpdateToolkitArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The @beamable/portal-toolkit version to update to; must exist in the npm registry or in verdaccio</summary>
        public string version;
        /// <summary>Update to the version currently published locally in verdaccio</summary>
        public bool local;
        /// <summary>The verdaccio registry URL used for --local and for version existence checks</summary>
        public string registry;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(string)))
            {
                genBeamCommandArgs.Add(("--version=" + this.version));
            }
            // If the local value was not default, then add it to the list of args.
            if ((this.local != default(bool)))
            {
                genBeamCommandArgs.Add(("--local=" + this.local));
            }
            // If the registry value was not default, then add it to the list of args.
            if ((this.registry != default(string)))
            {
                genBeamCommandArgs.Add(("--registry=" + this.registry));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual PortalExtensionUpdateToolkitWrapper PortalExtensionUpdateToolkit(PortalExtensionUpdateToolkitArgs updateToolkitArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("portal");
            genBeamCommandArgs.Add("extension");
            genBeamCommandArgs.Add("update-toolkit");
            genBeamCommandArgs.Add(updateToolkitArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            PortalExtensionUpdateToolkitWrapper genBeamCommandWrapper = new PortalExtensionUpdateToolkitWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class PortalExtensionUpdateToolkitWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual PortalExtensionUpdateToolkitWrapper OnStreamPortalExtensionUpdateToolkitCommandResults(System.Action<ReportDataPoint<BeamPortalExtensionUpdateToolkitCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
