
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public class OapiDownloadArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path</summary>
		public string output;
		/// <summary>Filter which open apis to generate. An empty string matches everything</summary>
		public string filter;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the output value was not default, then add it to the list of args.
			if ((this.output != default(string)))
			{
				genBeamCommandArgs.Add(("--output=" + this.output));
			}
			// If the filter value was not default, then add it to the list of args.
			if ((this.filter != default(string)))
			{
				genBeamCommandArgs.Add(("--filter=" + this.filter));
			}
			string genBeamCommandStr = "";
			// Join all the args with spaces
			genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			return genBeamCommandStr;
		}
	}
	public partial class BeamCommands
	{
		public virtual OapiDownloadWrapper OapiDownload(OapiDownloadArgs downloadArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("oapi");
			genBeamCommandArgs.Add("download");
			genBeamCommandArgs.Add(downloadArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			OapiDownloadWrapper genBeamCommandWrapper = new OapiDownloadWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public class OapiDownloadWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
	}
}
