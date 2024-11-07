
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ServicesResetImageArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The list of services to include, defaults to all local services (separated by whitespace)</summary>
		public string[] ids;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the ids value was not default, then add it to the list of args.
			if ((this.ids != default(string[])))
			{
				for (int i = 0; (i < this.ids.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--ids=" + this.ids[i]));
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
		public virtual ServicesResetImageWrapper ServicesResetImage(ServicesResetImageArgs imageArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("reset");
			genBeamCommandArgs.Add("image");
			genBeamCommandArgs.Add(imageArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesResetImageWrapper genBeamCommandWrapper = new ServicesResetImageWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ServicesResetImageWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ServicesResetImageWrapper OnStreamServicesResetImageCommandOutput(System.Action<ReportDataPoint<BeamServicesResetImageCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
