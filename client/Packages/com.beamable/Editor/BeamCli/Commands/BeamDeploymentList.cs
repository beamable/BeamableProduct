
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class DeploymentListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The limit of resources. A value of -1 means no limit</summary>
		public int limit;
		/// <summary>Include archived (removed) services</summary>
		public bool showArchived;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the limit value was not default, then add it to the list of args.
			if ((this.limit != default(int)))
			{
				genBeamCommandArgs.Add(("--limit=" + this.limit));
			}
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
		public virtual DeploymentListWrapper DeploymentList(DeploymentListArgs listArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("deployment");
			genBeamCommandArgs.Add("list");
			genBeamCommandArgs.Add(listArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			DeploymentListWrapper genBeamCommandWrapper = new DeploymentListWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class DeploymentListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual DeploymentListWrapper OnStreamListDeploymentsCommandOutput(System.Action<ReportDataPoint<BeamListDeploymentsCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
