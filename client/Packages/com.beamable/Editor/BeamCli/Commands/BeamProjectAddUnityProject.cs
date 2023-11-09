
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProjectAddUnityProjectArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Relative path to the Unity project</summary>
		public string path;
		/// <summary>When true, automatically accept path suggestions</summary>
		public bool quiet;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the path value to the list of args.
			genBeamCommandArgs.Add(this.path);
			// If the quiet value was not default, then add it to the list of args.
			if ((this.quiet != default(bool)))
			{
				genBeamCommandArgs.Add(("--quiet=" + this.quiet));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectAddUnityProjectWrapper ProjectAddUnityProject(ProjectAddUnityProjectArgs addUnityProjectArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("add-unity-project");
			genBeamCommandArgs.Add(addUnityProjectArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectAddUnityProjectWrapper genBeamCommandWrapper = new ProjectAddUnityProjectWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProjectAddUnityProjectWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
