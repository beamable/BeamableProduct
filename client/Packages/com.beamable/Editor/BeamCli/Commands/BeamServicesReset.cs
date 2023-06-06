
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ServicesResetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Either image|container|protocols.'image' will cleanup all your locally built images for the selected Beamo Services.
		///'container' will stop all your locally running containers for the selected Beamo Services.
		///'protocols' will reset all the protocol data for the selected Beamo Services back to default parameters</summary>
		public string target;
		/// <summary>The ids for the services you wish to reset</summary>
		public string[] ids;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the target value to the list of args.
			genBeamCommandArgs.Add(this.target);
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
		public virtual ServicesResetWrapper ServicesReset(ServicesResetArgs resetArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("services");
			genBeamCommandArgs.Add("reset");
			genBeamCommandArgs.Add(resetArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ServicesResetWrapper genBeamCommandWrapper = new ServicesResetWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ServicesResetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual Beamable.Common.BeamCli.BeamCommandWrapper OnStreamServicesResetResult(System.Action<ReportDataPoint<BeamServicesResetResult>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
