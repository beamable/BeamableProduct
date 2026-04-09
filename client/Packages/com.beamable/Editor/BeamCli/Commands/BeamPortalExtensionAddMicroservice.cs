
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class PortalExtensionAddMicroserviceArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The Portal Extension name that the microservice will be added to</summary>
        public string extension;
        /// <summary>The Microservice that will be a new dependency of the specified Portal Extension</summary>
        public string microservice;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the extension value to the list of args.
            genBeamCommandArgs.Add(this.extension.ToString());
            // Add the microservice value to the list of args.
            genBeamCommandArgs.Add(this.microservice.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual PortalExtensionAddMicroserviceWrapper PortalExtensionAddMicroservice(PortalExtensionAddMicroserviceArgs addMicroserviceArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("portal");
            genBeamCommandArgs.Add("extension");
            genBeamCommandArgs.Add("add-microservice");
            genBeamCommandArgs.Add(addMicroserviceArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            PortalExtensionAddMicroserviceWrapper genBeamCommandWrapper = new PortalExtensionAddMicroserviceWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class PortalExtensionAddMicroserviceWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
