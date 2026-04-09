
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class PortalExtensionSetConfigArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Extra file extensions to include in the watch process (e.g., '.md,.json'). These run alongside default extensions</summary>
        public string[] fileExtensionsToObserve;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the fileExtensionsToObserve value was not default, then add it to the list of args.
            if ((this.fileExtensionsToObserve != default(string[])))
            {
                for (int i = 0; (i < this.fileExtensionsToObserve.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--file-extensions-to-observe=" + this.fileExtensionsToObserve[i]));
                }
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual PortalExtensionSetConfigWrapper PortalExtensionSetConfig(PortalExtensionSetConfigArgs setConfigArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("portal");
            genBeamCommandArgs.Add("extension");
            genBeamCommandArgs.Add("set-config");
            genBeamCommandArgs.Add(setConfigArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            PortalExtensionSetConfigWrapper genBeamCommandWrapper = new PortalExtensionSetConfigWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class PortalExtensionSetConfigWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual PortalExtensionSetConfigWrapper OnStreamSetPortalExtensionConfigCommandResults(System.Action<ReportDataPoint<BeamSetPortalExtensionConfigCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
