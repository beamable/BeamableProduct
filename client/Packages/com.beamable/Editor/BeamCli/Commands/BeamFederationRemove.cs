
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class FederationRemoveArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The BeamoId of the microservice to add</summary>
        public string microservice;
        /// <summary>The federation id. Empty string will remove all federation ids. This is applied as an "AND" filter with the `fed-type` argument</summary>
        public string fedId;
        /// <summary>The type of federation to remove. Empty string will remove all federations. This is applied as an "AND" filter with the `fed-id` argument</summary>
        public Beamable.Api.Autogenerated.Models.FederationType fedTypes;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the microservice value to the list of args.
            genBeamCommandArgs.Add(this.microservice.ToString());
            // Add the fedId value to the list of args.
            genBeamCommandArgs.Add(this.fedId.ToString());
            // Add the fedTypes value to the list of args.
            genBeamCommandArgs.Add(this.fedTypes.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual FederationRemoveWrapper FederationRemove(FederationRemoveArgs removeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("federation");
            genBeamCommandArgs.Add("remove");
            genBeamCommandArgs.Add(removeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            FederationRemoveWrapper genBeamCommandWrapper = new FederationRemoveWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class FederationRemoveWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual FederationRemoveWrapper OnStreamRemoveFederationCommandOutput(System.Action<ReportDataPoint<BeamRemoveFederationCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
