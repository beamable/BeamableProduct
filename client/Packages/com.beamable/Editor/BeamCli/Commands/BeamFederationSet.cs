
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class FederationSetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The BeamoId of the microservice to add</summary>
        public string microservice;
        /// <summary>Erase all federations</summary>
        public bool clear;
        /// <summary>A federation id, must be in a parallel layout to the --fed-type option</summary>
        public string[] fedId;
        /// <summary>A federation type, must be in a parallel layout to the --fed-id option</summary>
        public string[] fedType;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the microservice value to the list of args.
            genBeamCommandArgs.Add(this.microservice.ToString());
            // If the clear value was not default, then add it to the list of args.
            if ((this.clear != default(bool)))
            {
                genBeamCommandArgs.Add(("--clear=" + this.clear));
            }
            // If the fedId value was not default, then add it to the list of args.
            if ((this.fedId != default(string[])))
            {
                for (int i = 0; (i < this.fedId.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--fed-id=" + this.fedId[i]));
                }
            }
            // If the fedType value was not default, then add it to the list of args.
            if ((this.fedType != default(string[])))
            {
                for (int i = 0; (i < this.fedType.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--fed-type=" + this.fedType[i]));
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
        public virtual FederationSetWrapper FederationSet(FederationSetArgs setArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("federation");
            genBeamCommandArgs.Add("set");
            genBeamCommandArgs.Add(setArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            FederationSetWrapper genBeamCommandWrapper = new FederationSetWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class FederationSetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual FederationSetWrapper OnStreamSetAllFederationsCommandOutput(System.Action<ReportDataPoint<BeamSetAllFederationsCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
