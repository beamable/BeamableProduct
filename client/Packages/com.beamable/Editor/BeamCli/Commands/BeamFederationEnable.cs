
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class FederationEnableArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The service to disable federation</summary>
		public string service;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the service value to the list of args.
			genBeamCommandArgs.Add(this.service.ToString());
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual FederationEnableWrapper FederationEnable(FederationEnableArgs enableArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("federation");
			genBeamCommandArgs.Add("enable");
			genBeamCommandArgs.Add(enableArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			FederationEnableWrapper genBeamCommandWrapper = new FederationEnableWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class FederationEnableWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual FederationEnableWrapper OnStreamDisableFederationCommandOutput(System.Action<ReportDataPoint<BeamDisableFederationCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}