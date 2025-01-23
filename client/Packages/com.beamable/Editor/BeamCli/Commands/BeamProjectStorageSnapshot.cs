
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectStorageSnapshotArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The beamoId for the storage object</summary>
        public string beamoId;
        /// <summary>The output for the snapshot</summary>
        public string output;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the beamoId value to the list of args.
            genBeamCommandArgs.Add(this.beamoId.ToString());
            // If the output value was not default, then add it to the list of args.
            if ((this.output != default(string)))
            {
                genBeamCommandArgs.Add((("--output=\"" + this.output) 
                                + "\""));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectStorageSnapshotWrapper ProjectStorageSnapshot(ProjectStorageSnapshotArgs snapshotArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("storage");
            genBeamCommandArgs.Add("snapshot");
            genBeamCommandArgs.Add(snapshotArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectStorageSnapshotWrapper genBeamCommandWrapper = new ProjectStorageSnapshotWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectStorageSnapshotWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectStorageSnapshotWrapper OnStreamSnapshotStorageObjectCommandOutput(System.Action<ReportDataPoint<BeamSnapshotStorageObjectCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ProjectStorageSnapshotWrapper OnMongoLogsCliLogMessage(System.Action<ReportDataPoint<Beamable.Common.BeamCli.Contracts.CliLogMessage>> cb)
        {
            this.Command.On("mongoLogs", cb);
            return this;
        }
    }
}
