
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ServeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>the owner of the server is used to identify the server later with the /info endpoint</summary>
		public string owner;
		/// <summary>the port the local server will bind to</summary>
		public int port;
		/// <summary>the number of seconds the server will stay alive without receiving any traffic</summary>
		public int selfDestructSeconds;
		/// <summary>when true, if the given --port is not available, it will be incremented until an available port is discovered</summary>
		public bool autoIncPort;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the owner value was not default, then add it to the list of args.
			if ((this.owner != default(string)))
			{
				genBeamCommandArgs.Add(this.owner.ToString());
			}
			// If the port value was not default, then add it to the list of args.
			if ((this.port != default(int)))
			{
				genBeamCommandArgs.Add(("--port=" + this.port));
			}
			// If the selfDestructSeconds value was not default, then add it to the list of args.
			if ((this.selfDestructSeconds != default(int)))
			{
				genBeamCommandArgs.Add(("--self-destruct-seconds=" + this.selfDestructSeconds));
			}
			// If the autoIncPort value was not default, then add it to the list of args.
			if ((this.autoIncPort != default(bool)))
			{
				genBeamCommandArgs.Add(("--auto-inc-port=" + this.autoIncPort));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ServeWrapper Serve(ServeArgs serveArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("serve");
			genBeamCommandArgs.Add(serveArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServeWrapper genBeamCommandWrapper = new ServeWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ServeWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ServeWrapper OnStreamServeCliCommandOutput(System.Action<ReportDataPoint<BeamServeCliCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
