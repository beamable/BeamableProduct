
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentListManifestsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Include content manifest ids that have been archive</summary>
        public bool includeArchived;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the includeArchived value was not default, then add it to the list of args.
            if ((this.includeArchived != default(bool)))
            {
                genBeamCommandArgs.Add(("--include-archived=" + this.includeArchived));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentListManifestsWrapper ContentListManifests(ContentListManifestsArgs listManifestsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("list-manifests");
            genBeamCommandArgs.Add(listManifestsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentListManifestsWrapper genBeamCommandWrapper = new ContentListManifestsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentListManifestsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentListManifestsWrapper OnStreamContentListManifestsCommandResults(System.Action<ReportDataPoint<BeamContentListManifestsCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
