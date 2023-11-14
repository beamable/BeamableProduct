
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProjectRegenerateArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Name of the new project</summary>
		public Beamable.Common.Semantics.ServiceName name;
		/// <summary>Where the temp project will be created</summary>
		public string output;
		/// <summary>The path to where the files will be copied to.</summary>
		public string copyPath;
		/// <summary>If you should create a common library</summary>
		public bool skipCommon;
		/// <summary>The name of the solution of the new project</summary>
		public Beamable.Common.Semantics.ServiceName solutionName;
		/// <summary>Specifies version of Beamable project dependencies</summary>
		public string version;
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
			// If the copyPath value was not default, then add it to the list of args.
			if ((this.copyPath != default(string)))
			{
				genBeamCommandArgs.Add(this.copyPath);
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
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectRegenerateWrapper ProjectRegenerate(ProjectRegenerateArgs regenerateArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("regenerate");
			genBeamCommandArgs.Add(regenerateArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectRegenerateWrapper genBeamCommandWrapper = new ProjectRegenerateWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProjectRegenerateWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
