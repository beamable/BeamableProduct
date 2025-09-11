
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentPublishArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>Defines if after publish the Content System should take snapshots of the content.
        ///None => Will not save any snapshot after publishing
        ///LocalOnly => Will save the snapshot under `.beamable/temp/content-snapshots/[PID]` folder
        ///SharedOnly => Will save the snapshot under `.beamable/content-snapshots/[PID]` folder
        ///Both => Will save two snapshots, under local and shared folders</summary>
        public Beamable.Common.BeamCli.Contracts.AutoSnapshotType autoSnapshotType;
        /// <summary>Defines the max stored local snapshots taken by the auto snapshot generation by this command. When the number hits, the older one will be deletd and replaced by the new snapshot</summary>
        public int maxLocalSnapshots;
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
            // If the autoSnapshotType value was not default, then add it to the list of args.
            if ((this.autoSnapshotType != default(Beamable.Common.BeamCli.Contracts.AutoSnapshotType)))
            {
                genBeamCommandArgs.Add(("--auto-snapshot-type=" + this.autoSnapshotType));
            }
            // If the maxLocalSnapshots value was not default, then add it to the list of args.
            if ((this.maxLocalSnapshots != default(int)))
            {
                genBeamCommandArgs.Add(("--max-local-snapshots=" + this.maxLocalSnapshots));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentPublishWrapper ContentPublish(ContentPublishArgs publishArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("publish");
            genBeamCommandArgs.Add(publishArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentPublishWrapper genBeamCommandWrapper = new ContentPublishWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentPublishWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentPublishWrapper OnStreamContentPublishResult(System.Action<ReportDataPoint<BeamContentPublishResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ContentPublishWrapper OnProgressStreamContentProgressUpdateData(System.Action<ReportDataPoint<BeamContentProgressUpdateData>> cb)
        {
            this.Command.On("progressStream", cb);
            return this;
        }
    }
}
