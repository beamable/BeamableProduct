
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class BeamArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>Should any networking happen?</summary>
		public bool dryrun;
		/// <summary>Cid to use; will default to whatever is in the file system</summary>
		public string cid;
		/// <summary>Pid to use; will default to whatever is in the file system</summary>
		public string pid;
		/// <summary>When true, skip input waiting and use defaults</summary>
		public bool quiet;
		/// <summary>The host endpoint for beamable</summary>
		public string host;
		/// <summary>Refresh token to use for the requests</summary>
		public string refreshToken;
		/// <summary>Extra logs gets printed out</summary>
		public string log;
		/// <summary>Show help for all commands</summary>
		public bool helpAll;
		/// <summary>By default, logs will automatically mask tokens. However, when this option is enabled, tokens will be visible in their full text. This is a security risk.</summary>
		public bool unmaskLogs;
		/// <summary>By default, logs are automatically written to a temp file so that they can be used in an error case. However, when this option is enabled, logs are not written. Also, if the BEAM_CLI_NO_FILE_LOG environment variable is set, no log file will be written. </summary>
		public bool noLogFile;
		/// <summary>Directory to use for configuration</summary>
		public string dir;
		/// <summary>Output raw JSON to standard out. This happens by default when the command is being piped</summary>
		public bool raw;
		/// <summary>Output syntax highlighted box text. This happens by default when the command is not piped</summary>
		public bool pretty;
		/// <summary>skips the check for commands that require beam config directories.</summary>
		public bool skipStandaloneValidation;
		/// <summary>a custom location for dotnet</summary>
		public string dotnetPath;
		/// <summary>Show version information</summary>
		public bool version;
		/// <summary>Show help and usage information</summary>
		public bool help;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the dryrun value was not default, then add it to the list of args.
			if ((this.dryrun != default(bool)))
			{
				genBeamCommandArgs.Add(("--dryrun=" + this.dryrun));
			}
			// If the cid value was not default, then add it to the list of args.
			if ((this.cid != default(string)))
			{
				genBeamCommandArgs.Add((("--cid=\"" + this.cid)
								+ "\""));
			}
			// If the pid value was not default, then add it to the list of args.
			if ((this.pid != default(string)))
			{
				genBeamCommandArgs.Add((("--pid=\"" + this.pid)
								+ "\""));
			}
			// If the quiet value was not default, then add it to the list of args.
			if ((this.quiet != default(bool)))
			{
				genBeamCommandArgs.Add(("--quiet=" + this.quiet));
			}
			// If the host value was not default, then add it to the list of args.
			if ((this.host != default(string)))
			{
				genBeamCommandArgs.Add((("--host=\"" + this.host)
								+ "\""));
			}
			// If the refreshToken value was not default, then add it to the list of args.
			if ((this.refreshToken != default(string)))
			{
				genBeamCommandArgs.Add((("--refresh-token=\"" + this.refreshToken)
								+ "\""));
			}
			// If the log value was not default, then add it to the list of args.
			if ((this.log != default(string)))
			{
				genBeamCommandArgs.Add((("--log=\"" + this.log)
								+ "\""));
			}
			// If the helpAll value was not default, then add it to the list of args.
			if ((this.helpAll != default(bool)))
			{
				genBeamCommandArgs.Add(("--help-all=" + this.helpAll));
			}
			// If the unmaskLogs value was not default, then add it to the list of args.
			if ((this.unmaskLogs != default(bool)))
			{
				genBeamCommandArgs.Add(("--unmask-logs=" + this.unmaskLogs));
			}
			// If the noLogFile value was not default, then add it to the list of args.
			if ((this.noLogFile != default(bool)))
			{
				genBeamCommandArgs.Add(("--no-log-file=" + this.noLogFile));
			}
			// If the dir value was not default, then add it to the list of args.
			if ((this.dir != default(string)))
			{
				genBeamCommandArgs.Add((("--dir=\"" + this.dir)
								+ "\""));
			}
			// If the raw value was not default, then add it to the list of args.
			if ((this.raw != default(bool)))
			{
				genBeamCommandArgs.Add(("--raw=" + this.raw));
			}
			// If the pretty value was not default, then add it to the list of args.
			if ((this.pretty != default(bool)))
			{
				genBeamCommandArgs.Add(("--pretty=" + this.pretty));
			}
			// If the skipStandaloneValidation value was not default, then add it to the list of args.
			if ((this.skipStandaloneValidation != default(bool)))
			{
				genBeamCommandArgs.Add(("--skip-standalone-validation=" + this.skipStandaloneValidation));
			}
			// If the dotnetPath value was not default, then add it to the list of args.
			if ((this.dotnetPath != default(string)))
			{
				genBeamCommandArgs.Add((("--dotnet-path=\"" + this.dotnetPath)
								+ "\""));
			}
			// If the version value was not default, then add it to the list of args.
			if ((this.version != default(bool)))
			{
				genBeamCommandArgs.Add(("--version=" + this.version));
			}
			// If the help value was not default, then add it to the list of args.
			if ((this.help != default(bool)))
			{
				genBeamCommandArgs.Add(("--help=" + this.help));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual BeamWrapper Beam()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			BeamWrapper genBeamCommandWrapper = new BeamWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class BeamWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
