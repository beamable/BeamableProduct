
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ServerReqArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>the port where the CLI server is running</summary>
		public int port;
		/// <summary>the CLI command to execute</summary>
		public string cli;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the port value was not default, then add it to the list of args.
			if ((this.port != default(int)))
			{
				genBeamCommandArgs.Add(("--port=" + this.port));
			}
			// If the cli value was not default, then add it to the list of args.
			if ((this.cli != default(string)))
			{
				genBeamCommandArgs.Add((("--cli=\"" + this.cli)
								+ "\""));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ServerReqWrapper ServerReq(ServerReqArgs reqArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("server");
			genBeamCommandArgs.Add("req");
			genBeamCommandArgs.Add(reqArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServerReqWrapper genBeamCommandWrapper = new ServerReqWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ServerReqWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ServerReqWrapper OnStreamRequestCliCommandOutput(System.Action<ReportDataPoint<BeamRequestCliCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
