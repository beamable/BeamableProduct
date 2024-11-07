
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ServicesUpdateDockerfileArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The name of the microservice to udpate the Dockerfile</summary>
		public string ServiceName;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the ServiceName value to the list of args.
			genBeamCommandArgs.Add(this.ServiceName.ToString());
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ServicesUpdateDockerfileWrapper ServicesUpdateDockerfile(ServicesUpdateDockerfileArgs updateDockerfileArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("update-dockerfile");
			genBeamCommandArgs.Add(updateDockerfileArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesUpdateDockerfileWrapper genBeamCommandWrapper = new ServicesUpdateDockerfileWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ServicesUpdateDockerfileWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
