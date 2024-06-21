
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ServicesPsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Outputs as json instead of summary table</summary>
		public bool json;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the json value was not default, then add it to the list of args.
			if ((this.json != default(bool)))
			{
				genBeamCommandArgs.Add(("--json=" + this.json));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ServicesPsWrapper ServicesPs(ServicesPsArgs psArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("ps");
			genBeamCommandArgs.Add(psArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesPsWrapper genBeamCommandWrapper = new ServicesPsWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ServicesPsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ServicesPsWrapper OnStreamServiceListResult(System.Action<ReportDataPoint<BeamServiceListResult>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
