
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProjectVersionArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Request specific version of Beamable packages.</summary>
		public string requestedVersion;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the requestedVersion value was not default, then add it to the list of args.
			if ((this.requestedVersion != default(string)))
			{
				genBeamCommandArgs.Add(("--requested-version=" + this.requestedVersion));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectVersionWrapper ProjectVersion(ProjectVersionArgs versionArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("version");
			genBeamCommandArgs.Add(versionArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectVersionWrapper genBeamCommandWrapper = new ProjectVersionWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProjectVersionWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual Beamable.Common.BeamCli.BeamCommandWrapper OnStreamProjectVersionCommandResult(System.Action<ReportDataPoint<BeamProjectVersionCommandResult>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
