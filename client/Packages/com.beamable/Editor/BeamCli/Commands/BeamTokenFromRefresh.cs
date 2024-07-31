
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class TokenFromRefreshArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>the token that you want to get information for. This must be a refresh token. By default, the current refresh token of the .beamable context is used.</summary>
		public string token;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the token value was not default, then add it to the list of args.
			if ((this.token != default(string)))
			{
				genBeamCommandArgs.Add((("--token=\"" + this.token)
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
		public virtual TokenFromRefreshWrapper TokenFromRefresh(TokenFromRefreshArgs fromRefreshArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("token");
			genBeamCommandArgs.Add("from-refresh");
			genBeamCommandArgs.Add(fromRefreshArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			TokenFromRefreshWrapper genBeamCommandWrapper = new TokenFromRefreshWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class TokenFromRefreshWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual TokenFromRefreshWrapper OnStreamGetTokenViaRefreshCommandOutput(System.Action<ReportDataPoint<BeamGetTokenViaRefreshCommandOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
