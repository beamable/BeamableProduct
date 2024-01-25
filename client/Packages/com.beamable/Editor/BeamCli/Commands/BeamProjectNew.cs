
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProjectNewArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Name of the new project</summary>
		public Beamable.Common.Semantics.ServiceName name;
		/// <summary>Where the project be created</summary>
		public string output;
		/// <summary>If you should create a common library</summary>
		public bool skipCommon;
		/// <summary>The name of the solution of the new project</summary>
		public Beamable.Common.Semantics.ServiceName solutionName;
		/// <summary>Specifies version of Beamable project dependencies</summary>
		public string version;
		/// <summary>Create service that is disabled on publish</summary>
		public bool disable;
		/// <summary>When true, automatically accept path suggestions</summary>
		public bool quiet;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the name value to the list of args.
			genBeamCommandArgs.Add(this.name);
			// If the output value was not default, then add it to the list of args.
			if ((this.output != default(string)))
			{
				genBeamCommandArgs.Add(this.output);
			}
			// If the skipCommon value was not default, then add it to the list of args.
			if ((this.skipCommon != default(bool)))
			{
				genBeamCommandArgs.Add(("--skip-common=" + this.skipCommon));
			}
			// If the solutionName value was not default, then add it to the list of args.
			if ((this.solutionName != default(Beamable.Common.Semantics.ServiceName)))
			{
				genBeamCommandArgs.Add(("--solution-name=" + this.solutionName));
			}
			// If the version value was not default, then add it to the list of args.
			if ((this.version != default(string)))
			{
				genBeamCommandArgs.Add(("--version=" + this.version));
			}
			// If the disable value was not default, then add it to the list of args.
			if ((this.disable != default(bool)))
			{
				genBeamCommandArgs.Add(("--disable=" + this.disable));
			}
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
		public virtual ProjectNewWrapper ProjectNew(ProjectNewArgs newArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("new");
			genBeamCommandArgs.Add(newArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectNewWrapper genBeamCommandWrapper = new ProjectNewWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProjectNewWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
