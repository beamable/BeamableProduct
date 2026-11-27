
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesSourceListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>How many requests to show</summary>
        public int limit;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the limit value was not default, then add it to the list of args.
            if ((this.limit != default(int)))
            {
                genBeamCommandArgs.Add(("--limit=" + this.limit));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesSourceListWrapper BundlesSourceList(BundlesSourceListArgs listArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("source");
            genBeamCommandArgs.Add("list");
            genBeamCommandArgs.Add(listArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesSourceListWrapper genBeamCommandWrapper = new BundlesSourceListWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesSourceListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesSourceListWrapper OnStreamListSourceDeliveriesCommandOutput(System.Action<ReportDataPoint<BeamListSourceDeliveriesCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
