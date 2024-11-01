
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ContentResetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return all manifests</summary>
		public string[] manifestIds;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the manifestIds value was not default, then add it to the list of args.
			if ((this.manifestIds != default(string[])))
			{
				genBeamCommandArgs.Add((("--manifest-ids=\"" + this.manifestIds)
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
		public virtual ContentResetWrapper ContentReset(ContentResetArgs resetArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("content");
			genBeamCommandArgs.Add("reset");
			genBeamCommandArgs.Add(resetArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ContentResetWrapper genBeamCommandWrapper = new ContentResetWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ContentResetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ContentResetWrapper OnStreamLocalContentState(System.Action<ReportDataPoint<BeamLocalContentState>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
