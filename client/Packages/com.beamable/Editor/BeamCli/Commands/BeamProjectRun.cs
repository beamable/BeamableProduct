
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProjectRunArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>When true, the command will run forever and watch the state of the program</summary>
		public bool watch;
		/// <summary>The list of services to include, defaults to all local services (separated by whitespace)</summary>
		public string[] ids;
		/// <summary>With this flag, we restart any running services. Without it, we skip running services</summary>
		public bool force;
		/// <summary>With this flag, we don't remain attached to the running service process</summary>
		public bool detach;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the watch value was not default, then add it to the list of args.
			if ((this.watch != default(bool)))
			{
				genBeamCommandArgs.Add(("--watch=" + this.watch));
			}
			// If the ids value was not default, then add it to the list of args.
			if ((this.ids != default(string[])))
			{
				for (int i = 0; (i < this.ids.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--ids=" + this.ids[i]));
				}
			}
			// If the force value was not default, then add it to the list of args.
			if ((this.force != default(bool)))
			{
				genBeamCommandArgs.Add(("--force=" + this.force));
			}
			// If the detach value was not default, then add it to the list of args.
			if ((this.detach != default(bool)))
			{
				genBeamCommandArgs.Add(("--detach=" + this.detach));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectRunWrapper ProjectRun(ProjectRunArgs runArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("run");
			genBeamCommandArgs.Add(runArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectRunWrapper genBeamCommandWrapper = new ProjectRunWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProjectRunWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
