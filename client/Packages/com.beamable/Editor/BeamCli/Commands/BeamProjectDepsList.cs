
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ProjectDepsListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The name of the service to list the dependencies of</summary>
		public string service;
		/// <summary>If this is passed and set to True, then all references of the service will be listed</summary>
		public bool all;
		/// <summary>If this is passed and set to True, then all references that are not storages or microservices will be listed</summary>
		public bool nonBeamo;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the service value was not default, then add it to the list of args.
			if ((this.service != default(string)))
			{
				genBeamCommandArgs.Add((("--service=\"" + this.service)
								+ "\""));
			}
			// If the all value was not default, then add it to the list of args.
			if ((this.all != default(bool)))
			{
				genBeamCommandArgs.Add(("--all=" + this.all));
			}
			// If the nonBeamo value was not default, then add it to the list of args.
			if ((this.nonBeamo != default(bool)))
			{
				genBeamCommandArgs.Add(("--non-beamo=" + this.nonBeamo));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectDepsListWrapper ProjectDepsList(ProjectDepsListArgs listArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("deps");
			genBeamCommandArgs.Add("list");
			genBeamCommandArgs.Add(listArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectDepsListWrapper genBeamCommandWrapper = new ProjectDepsListWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ProjectDepsListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ProjectDepsListWrapper OnStreamListDepsCommandResults(System.Action<ReportDataPoint<BeamListDepsCommandResults>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
