
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectStorageEraseArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>the beamoId for the storage object</summary>
        public string beamoId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the beamoId value to the list of args.
            genBeamCommandArgs.Add(this.beamoId.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectStorageEraseWrapper ProjectStorageErase(ProjectStorageEraseArgs eraseArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("storage");
            genBeamCommandArgs.Add("erase");
            genBeamCommandArgs.Add(eraseArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectStorageEraseWrapper genBeamCommandWrapper = new ProjectStorageEraseWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectStorageEraseWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectStorageEraseWrapper OnStreamEraseStorageObjectCommandOutput(System.Action<ReportDataPoint<BeamEraseStorageObjectCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ProjectStorageEraseWrapper OnMongoLogsCliLogMessage(System.Action<ReportDataPoint<Beamable.Common.BeamCli.Contracts.CliLogMessage>> cb)
        {
            this.Command.On("mongoLogs", cb);
            return this;
        }
    }
}
