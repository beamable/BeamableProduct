
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class FederationListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Filter the federations by the type</summary>
        public string type;
        /// <summary>Filter the federations by the service name</summary>
        public string id;
        /// <summary>Filter the federation by its federation id</summary>
        public string fedIds;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the type value was not default, then add it to the list of args.
            if ((this.type != default(string)))
            {
                genBeamCommandArgs.Add(("--type=" + this.type));
            }
            // If the id value was not default, then add it to the list of args.
            if ((this.id != default(string)))
            {
                genBeamCommandArgs.Add(("--id=" + this.id));
            }
            // If the fedIds value was not default, then add it to the list of args.
            if ((this.fedIds != default(string)))
            {
                genBeamCommandArgs.Add(("--fed-ids=" + this.fedIds));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual FederationListWrapper FederationList(FederationListArgs listArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("federation");
            genBeamCommandArgs.Add("list");
            genBeamCommandArgs.Add(listArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            FederationListWrapper genBeamCommandWrapper = new FederationListWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class FederationListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual FederationListWrapper OnStreamListServicesCommandOutput(System.Action<ReportDataPoint<BeamListServicesCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
