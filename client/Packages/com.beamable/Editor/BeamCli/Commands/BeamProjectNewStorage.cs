
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProjectNewStorageArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The name of the new Microstorage.</summary>
		public Beamable.Common.Semantics.ServiceName name;
		/// <summary>The path to the solution that the Microstorage will be added to</summary>
		public string sln;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the name value to the list of args.
			genBeamCommandArgs.Add(this.name);
			// If the sln value was not default, then add it to the list of args.
			if ((this.sln != default(string)))
			{
				genBeamCommandArgs.Add(("--sln=" + this.sln));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectNewStorageWrapper ProjectNewStorage(ProjectNewStorageArgs newStorageArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("new-storage");
			genBeamCommandArgs.Add(newStorageArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectNewStorageWrapper genBeamCommandWrapper = new ProjectNewStorageWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProjectNewStorageWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
