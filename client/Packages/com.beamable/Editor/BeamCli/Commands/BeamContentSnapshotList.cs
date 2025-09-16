
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentSnapshotListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Defines the name of the manifest that will be used to compare the changes between the manifest and the snapshot. The default value is `global`</summary>
        public string manifestId;
        /// <summary>An optional field to set the PID from where you would like to get the snapshot to list. The default will get for all the realms</summary>
        public string pid;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the manifestId value was not default, then add it to the list of args.
            if ((this.manifestId != default(string)))
            {
                genBeamCommandArgs.Add(("--manifest-id=" + this.manifestId));
            }
            // If the pid value was not default, then add it to the list of args.
            if ((this.pid != default(string)))
            {
                genBeamCommandArgs.Add(("--pid=" + this.pid));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentSnapshotListWrapper ContentSnapshotList(ContentSnapshotListArgs snapshotListArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("snapshot-list");
            genBeamCommandArgs.Add(snapshotListArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentSnapshotListWrapper genBeamCommandWrapper = new ContentSnapshotListWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentSnapshotListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentSnapshotListWrapper OnStreamContentSnapshotListResult(System.Action<ReportDataPoint<BeamContentSnapshotListResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
