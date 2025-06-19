
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeploymentRegistryArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The beamo id for the service to get images for</summary>
        public string serviceId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the serviceId value to the list of args.
            genBeamCommandArgs.Add(this.serviceId.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual DeploymentRegistryWrapper DeploymentRegistry(DeploymentRegistryArgs registryArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("deployment");
            genBeamCommandArgs.Add("registry");
            genBeamCommandArgs.Add(registryArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeploymentRegistryWrapper genBeamCommandWrapper = new DeploymentRegistryWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeploymentRegistryWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeploymentRegistryWrapper OnStreamCheckRegistryCommandResults(System.Action<ReportDataPoint<BeamCheckRegistryCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
