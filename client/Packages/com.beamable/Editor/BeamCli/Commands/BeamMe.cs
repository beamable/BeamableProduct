
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class MeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Make command returns plain text without custom colors and formatting</summary>
		public bool plainOutput;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the plainOutput value was not default, then add it to the list of args.
			if ((this.plainOutput != default(bool)))
			{
				genBeamCommandArgs.Add(("--plain-output=" + this.plainOutput));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual MeWrapper Me(MeArgs meArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("me");
			genBeamCommandArgs.Add(meArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			MeWrapper genBeamCommandWrapper = new MeWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class MeWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual Beamable.Common.BeamCli.BeamCommandWrapper OnStreamUser(System.Action<ReportDataPoint<Beamable.Common.Api.Auth.User>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}