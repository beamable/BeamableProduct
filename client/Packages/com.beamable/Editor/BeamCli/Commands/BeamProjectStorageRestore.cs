
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProjectStorageRestoreArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>the beamoId for the storage object</summary>
		public string beamoId;
		/// <summary>when true, merges the snapshot into the existing data</summary>
		public bool merge;
		/// <summary>the input for the snapshot</summary>
		public string input;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the beamoId value to the list of args.
			genBeamCommandArgs.Add(this.beamoId.ToString());
			// If the merge value was not default, then add it to the list of args.
			if ((this.merge != default(bool)))
			{
				genBeamCommandArgs.Add(("--merge=" + this.merge));
			}
			// If the input value was not default, then add it to the list of args.
			if ((this.input != default(string)))
			{
				genBeamCommandArgs.Add((("--input=\"" + this.input)
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
		public virtual ProjectStorageRestoreWrapper ProjectStorageRestore(ProjectStorageRestoreArgs restoreArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("storage");
			genBeamCommandArgs.Add("restore");
			genBeamCommandArgs.Add(restoreArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectStorageRestoreWrapper genBeamCommandWrapper = new ProjectStorageRestoreWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProjectStorageRestoreWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ProjectStorageRestoreWrapper OnStreamRestoreStorageObjectCommandOutput(System.Action<ReportDataPoint<BeamRestoreStorageObjectCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
		public virtual ProjectStorageRestoreWrapper OnMongoLogsCliLogMessage(System.Action<ReportDataPoint<Beamable.Common.BeamCli.Contracts.CliLogMessage>> cb)
		{
			this.Command.On("mongoLogs", cb);
			return this;
		}
	}
}
