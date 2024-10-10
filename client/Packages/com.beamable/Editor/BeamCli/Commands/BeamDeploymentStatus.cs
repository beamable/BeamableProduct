
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class DeploymentStatusArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>include archived (removed) services</summary>
		public bool showArchived;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the showArchived value was not default, then add it to the list of args.
			if ((this.showArchived != default(bool)))
			{
				genBeamCommandArgs.Add(("--show-archived=" + this.showArchived));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual DeploymentStatusWrapper DeploymentStatus(DeploymentStatusArgs statusArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("deployment");
			genBeamCommandArgs.Add("status");
			genBeamCommandArgs.Add(statusArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			DeploymentStatusWrapper genBeamCommandWrapper = new DeploymentStatusWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class DeploymentStatusWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual DeploymentStatusWrapper OnStreamShowCurrentBeamoStatusCommandOutput(System.Action<ReportDataPoint<BeamShowCurrentBeamoStatusCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
