
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentSnapshotArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Defines the name of the manifest that the snapshot will be created from. The default value is `global`</summary>
        public string manifestId;
        /// <summary>Defines the name for the snapshot to be created</summary>
        public string name;
        /// <summary>Defines where the snapshot will be stored to.
        ///Local => Will save the snapshot under `.beamable/temp/content-snapshots/[PID]` folder
        ///Shared => Will save the snapshot under `.beamable/content-snapshots/[PID]` folder</summary>
        public Beamable.Common.BeamCli.Contracts.ContentSnapshotType snapshotType;
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
            // If the name value was not default, then add it to the list of args.
            if ((this.name != default(string)))
            {
                genBeamCommandArgs.Add(("--name=" + this.name));
            }
            // If the snapshotType value was not default, then add it to the list of args.
            if ((this.snapshotType != default(Beamable.Common.BeamCli.Contracts.ContentSnapshotType)))
            {
                genBeamCommandArgs.Add(("--snapshot-type=" + this.snapshotType));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentSnapshotWrapper ContentSnapshot(ContentSnapshotArgs snapshotArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("snapshot");
            genBeamCommandArgs.Add(snapshotArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentSnapshotWrapper genBeamCommandWrapper = new ContentSnapshotWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentSnapshotWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentSnapshotWrapper OnStreamContentSnapshotResult(System.Action<ReportDataPoint<BeamContentSnapshotResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
