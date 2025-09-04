
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentRestoreArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Defines the name of the manifest on which the snapshot will be restored. The default value is `global`</summary>
        public string manifestId;
        /// <summary>Defines the name or path for the snapshot to be restored. If passed a name, it will first get the snapshot from shared folder '.beamable/content-snapshots' than from the local only under '.beamable/temp/content-snapshots'. If a path is passed, it is going to try get the json file from the path</summary>
        public string name;
        /// <summary>Defines if the snapshot file should be deleted after restoring</summary>
        public bool deleteAfterRestore;
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
            // If the deleteAfterRestore value was not default, then add it to the list of args.
            if ((this.deleteAfterRestore != default(bool)))
            {
                genBeamCommandArgs.Add(("--delete-after-restore=" + this.deleteAfterRestore));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentRestoreWrapper ContentRestore(ContentRestoreArgs restoreArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("restore");
            genBeamCommandArgs.Add(restoreArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentRestoreWrapper genBeamCommandWrapper = new ContentRestoreWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentRestoreWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentRestoreWrapper OnStreamContentRestoreResult(System.Action<ReportDataPoint<BeamContentRestoreResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ContentRestoreWrapper OnErrorContentRestoreErrorReport(System.Action<ReportDataPoint<BeamContentRestoreErrorReport>> cb)
        {
            this.Command.On("errorContentRestoreErrorReport", cb);
            return this;
        }
    }
}
