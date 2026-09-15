
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OrgPrListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Filter by status: Pending, Approved, Rejected, or Superseded</summary>
        public string status;
        /// <summary>Maximum number of pull requests to return</summary>
        public System.Nullable<int> limit;
        /// <summary>Number of pull requests to skip</summary>
        public System.Nullable<int> offset;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the status value was not default, then add it to the list of args.
            if ((this.status != default(string)))
            {
                genBeamCommandArgs.Add(("--status=" + this.status));
            }
            // If the limit value was not default, then add it to the list of args.
            if ((this.limit != default(System.Nullable<int>)))
            {
                genBeamCommandArgs.Add(("--limit=" + this.limit));
            }
            // If the offset value was not default, then add it to the list of args.
            if ((this.offset != default(System.Nullable<int>)))
            {
                genBeamCommandArgs.Add(("--offset=" + this.offset));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OrgPrListWrapper OrgPrList(OrgPrListArgs listArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("org");
            genBeamCommandArgs.Add("pr");
            genBeamCommandArgs.Add("list");
            genBeamCommandArgs.Add(listArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OrgPrListWrapper genBeamCommandWrapper = new OrgPrListWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OrgPrListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual OrgPrListWrapper OnStreamPrListCommandOutput(System.Action<ReportDataPoint<BeamPrListCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
