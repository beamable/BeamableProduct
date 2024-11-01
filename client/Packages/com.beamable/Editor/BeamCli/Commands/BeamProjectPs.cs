
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ProjectPsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>When true, the command will run forever and watch the state of the program</summary>
		public bool watch;
		/// <summary>The list of services to include, defaults to all local services (separated by whitespace)</summary>
		public string[] ids;
		/// <summary>A set of BeamServiceGroup tags that will exclude the associated services. Exclusion takes precedence over inclusion</summary>
		public string[] withoutGroup;
		/// <summary>A set of BeamServiceGroup tags that will include the associated services</summary>
		public string[] withGroup;
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
			// If the withoutGroup value was not default, then add it to the list of args.
			if ((this.withoutGroup != default(string[])))
			{
				for (int i = 0; (i < this.withoutGroup.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--without-group=" + this.withoutGroup[i]));
				}
			}
			// If the withGroup value was not default, then add it to the list of args.
			if ((this.withGroup != default(string[])))
			{
				for (int i = 0; (i < this.withGroup.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--with-group=" + this.withGroup[i]));
				}
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProjectPsWrapper ProjectPs(ProjectPsArgs psArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("project");
			genBeamCommandArgs.Add("ps");
			genBeamCommandArgs.Add(psArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProjectPsWrapper genBeamCommandWrapper = new ProjectPsWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ProjectPsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ProjectPsWrapper OnStreamCheckStatusServiceResult(System.Action<ReportDataPoint<BeamCheckStatusServiceResult>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
