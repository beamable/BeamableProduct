
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ServicesDockerStartArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Only return the links to download docker, but do not start</summary>
		public bool linksOnly;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the linksOnly value was not default, then add it to the list of args.
			if ((this.linksOnly != default(bool)))
			{
				genBeamCommandArgs.Add(("--links-only=" + this.linksOnly));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ServicesDockerStartWrapper ServicesDockerStart(ServicesDockerStartArgs startArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("docker");
			genBeamCommandArgs.Add("start");
			genBeamCommandArgs.Add(startArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesDockerStartWrapper genBeamCommandWrapper = new ServicesDockerStartWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ServicesDockerStartWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ServicesDockerStartWrapper OnStreamStartDockerCommandOutput(System.Action<ReportDataPoint<BeamStartDockerCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
