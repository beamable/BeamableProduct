
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class ProfileCheckNbomberArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>The path to the nbomber output csv file</summary>
		public string nbomberFilePath;
		/// <summary>The max number of failed requests</summary>
		public double failLimit;
		/// <summary>The max p95 in ms</summary>
		public double p95Limit;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// Add the nbomberFilePath value to the list of args.
			genBeamCommandArgs.Add(this.nbomberFilePath.ToString());
			// If the failLimit value was not default, then add it to the list of args.
			if ((this.failLimit != default(double)))
			{
				genBeamCommandArgs.Add(("--fail-limit=" + this.failLimit));
			}
			// If the p95Limit value was not default, then add it to the list of args.
			if ((this.p95Limit != default(double)))
			{
				genBeamCommandArgs.Add(("--p95-limit=" + this.p95Limit));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual ProfileCheckNbomberWrapper ProfileCheckNbomber(ProfileCheckNbomberArgs checkNbomberArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("profile");
			genBeamCommandArgs.Add("check-nbomber");
			genBeamCommandArgs.Add(checkNbomberArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ProfileCheckNbomberWrapper genBeamCommandWrapper = new ProfileCheckNbomberWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class ProfileCheckNbomberWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ProfileCheckNbomberWrapper OnStreamCheckPerfCommandOutput(System.Action<ReportDataPoint<BeamCheckPerfCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
