
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ServicesGetConnectionStringArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The name of the Microstorage</summary>
		public string storageName;
		/// <summary>The Microstorage remote connection string</summary>
		public bool remote;
		/// <summary>Ignores confirmation step</summary>
		public bool quiet;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the storageName value to the list of args.
			genBeamCommandArgs.Add(this.storageName.ToString());
			// If the remote value was not default, then add it to the list of args.
			if ((this.remote != default(bool)))
			{
				genBeamCommandArgs.Add(("--remote=" + this.remote));
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
		public virtual ServicesGetConnectionStringWrapper ServicesGetConnectionString(ServicesGetConnectionStringArgs getConnectionStringArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("get-connection-string");
			genBeamCommandArgs.Add(getConnectionStringArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesGetConnectionStringWrapper genBeamCommandWrapper = new ServicesGetConnectionStringWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ServicesGetConnectionStringWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ServicesGetConnectionStringWrapper OnStreamServicesGetConnectionStringCommandOutput(System.Action<ReportDataPoint<BeamServicesGetConnectionStringCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
