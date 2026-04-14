
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentHistorySyncChangelistArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>The manifest UID for the changelist to sync</summary>
        public string manifestUid;
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
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentHistorySyncChangelistWrapper ContentHistorySyncChangelist(ContentHistoryArgs historyArgs, ContentHistorySyncChangelistArgs syncChangelistArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("history");
            genBeamCommandArgs.Add(historyArgs.Serialize());
            genBeamCommandArgs.Add("sync-changelist");
            genBeamCommandArgs.Add(syncChangelistArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentHistorySyncChangelistWrapper genBeamCommandWrapper = new ContentHistorySyncChangelistWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentHistorySyncChangelistWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentHistorySyncChangelistWrapper OnStreamContentHistorySyncChangelistCommandOutput(System.Action<ReportDataPoint<BeamContentHistorySyncChangelistCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
