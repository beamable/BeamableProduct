
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentSyncArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>Defines the semantics for the `filter` argument. When no filters are given, affects all existing content.
        ///ExactIds => Will only add the given tags to the ','-separated list of filters
        ///Regexes => Will add the given tags to any content whose Id is matched by any of the ','-separated list of filters (C# regex string)
        ///TypeHierarchy => Will add the given tags to any content of the ','-separated list of filters (content type strings with full hierarchy --- StartsWith comparison)
        ///Tags => Will add the given tags to any content that currently has any of the ','-separated list of filters (tags)</summary>
        public Beamable.Common.Content.ContentFilterType filterType;
        /// <summary>Accepts different strings to filter which content files will be affected. See the `filter-type` option</summary>
        public string filter;
        /// <summary>Deletes any created content that is not present in the latest manifest. If filters are provided, will only delete the created content that matches the filter</summary>
        public bool syncCreated;
        /// <summary>This will discard your local changes ONLY on files that are NOT conflicted. If filters are provided, will only do this for content that matches the filter</summary>
        public bool syncModified;
        /// <summary>This will discard your local changes ONLY on files that ARE conflicted. If filters are provided, will only do this for content that matches the filter</summary>
        public bool syncConflicts;
        /// <summary>This will revert all your deleted files. If filters are provided, will only do this for content that matches the filter</summary>
        public bool syncDeleted;
        /// <summary>If you pass in a Manifest's UID, we'll sync with that as the target. If filters are provided, will only do this for content that matches the filter</summary>
        public string target;
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
            // If the filterType value was not default, then add it to the list of args.
            if ((this.filterType != default(Beamable.Common.Content.ContentFilterType)))
            {
                genBeamCommandArgs.Add(("--filter-type=" + this.filterType));
            }
            // If the filter value was not default, then add it to the list of args.
            if ((this.filter != default(string)))
            {
                genBeamCommandArgs.Add(("--filter=" + this.filter));
            }
            // If the syncCreated value was not default, then add it to the list of args.
            if ((this.syncCreated != default(bool)))
            {
                genBeamCommandArgs.Add(("--sync-created=" + this.syncCreated));
            }
            // If the syncModified value was not default, then add it to the list of args.
            if ((this.syncModified != default(bool)))
            {
                genBeamCommandArgs.Add(("--sync-modified=" + this.syncModified));
            }
            // If the syncConflicts value was not default, then add it to the list of args.
            if ((this.syncConflicts != default(bool)))
            {
                genBeamCommandArgs.Add(("--sync-conflicts=" + this.syncConflicts));
            }
            // If the syncDeleted value was not default, then add it to the list of args.
            if ((this.syncDeleted != default(bool)))
            {
                genBeamCommandArgs.Add(("--sync-deleted=" + this.syncDeleted));
            }
            // If the target value was not default, then add it to the list of args.
            if ((this.target != default(string)))
            {
                genBeamCommandArgs.Add(("--target=" + this.target));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentSyncWrapper ContentSync(ContentSyncArgs syncArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("sync");
            genBeamCommandArgs.Add(syncArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentSyncWrapper genBeamCommandWrapper = new ContentSyncWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentSyncWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentSyncWrapper OnStreamContentSyncResult(System.Action<ReportDataPoint<BeamContentSyncResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ContentSyncWrapper OnProgressStreamContentProgressUpdateData(System.Action<ReportDataPoint<BeamContentProgressUpdateData>> cb)
        {
            this.Command.On("progressStream", cb);
            return this;
        }
    }
}
