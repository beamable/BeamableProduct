
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentHistorySyncContentArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>The manifest UID for the content to sync</summary>
        public string manifestUid;
        /// <summary>The content IDs to sync. If not provided, syncs all content in the manifest</summary>
        public System.Collections.Generic.List<string> contentIds;
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
            if ((this.contentIds != default(System.Collections.Generic.List<string>)))
            {
                genBeamCommandArgs.Add(("--content-ids=" + this.contentIds));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentHistorySyncContentWrapper ContentHistorySyncContent(ContentHistoryArgs historyArgs, ContentHistorySyncContentArgs syncContentArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("history");
            genBeamCommandArgs.Add(historyArgs.Serialize());
            genBeamCommandArgs.Add("sync-content");
            genBeamCommandArgs.Add(syncContentArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentHistorySyncContentWrapper genBeamCommandWrapper = new ContentHistorySyncContentWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentHistorySyncContentWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentHistorySyncContentWrapper OnStreamContentHistorySyncContentCommandOutput(System.Action<ReportDataPoint<BeamContentHistorySyncContentCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
