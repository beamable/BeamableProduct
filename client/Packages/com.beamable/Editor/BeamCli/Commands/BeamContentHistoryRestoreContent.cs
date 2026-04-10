
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentHistoryRestoreContentArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>The manifest UID from history to restore content from</summary>
        public string manifestUid;
        /// <summary>The content IDs to restore. If not provided, restores all content in the manifest</summary>
        public string[] contentIds;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the manifestIds value was not default, then add it to the list of args.
            if ((this.manifestIds != default(string[])))
            {
                for (int i = 0; (i < this.manifestIds.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--manifest-ids=" + this.manifestIds[i]));
                }
            }
            // If the manifestUid value was not default, then add it to the list of args.
            if ((this.manifestUid != default(string)))
            {
                genBeamCommandArgs.Add(("--manifest-uid=" + this.manifestUid));
            }
            // If the contentIds value was not default, then add it to the list of args.
            if ((this.contentIds != default(string[])))
            {
                for (int i = 0; (i < this.contentIds.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--content-ids=" + this.contentIds[i]));
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
        public virtual ContentHistoryRestoreContentWrapper ContentHistoryRestoreContent(ContentHistoryArgs historyArgs, ContentHistoryRestoreContentArgs restoreContentArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("history");
            genBeamCommandArgs.Add(historyArgs.Serialize());
            genBeamCommandArgs.Add("restore-content");
            genBeamCommandArgs.Add(restoreContentArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentHistoryRestoreContentWrapper genBeamCommandWrapper = new ContentHistoryRestoreContentWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentHistoryRestoreContentWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentHistoryRestoreContentWrapper OnStreamContentHistoryRestoreCommandOutput(System.Action<ReportDataPoint<BeamContentHistoryRestoreCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
