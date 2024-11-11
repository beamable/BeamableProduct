
namespace Beamable.Editor.BeamCli.Commands
{
	using Beamable.Common;
	using Beamable.Common.BeamCli;

	public partial class ConfigRealmSetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
	{
		/// <summary>A list of realm config key/value pairs in a 'namespace|key::value' format</summary>
		public string[] keyValues;
		/// <summary>Serializes the arguments for command line usage.</summary>
		public virtual string Serialize()
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			// If the keyValues value was not default, then add it to the list of args.
			if ((this.keyValues != default(string[])))
			{
				for (int i = 0; (i < this.keyValues.Length); i = (i + 1))
				{
					// The parameter allows multiple values
					genBeamCommandArgs.Add(("--key-values=" + this.keyValues[i]));
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
		public virtual ConfigRealmSetWrapper ConfigRealmSet(ConfigRealmArgs realmArgs, ConfigRealmSetArgs setArgs)
		{
			// Create a list of arguments for the command
			System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
			genBeamCommandArgs.Add("beam");
			genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
			genBeamCommandArgs.Add("config");
			genBeamCommandArgs.Add("realm");
			genBeamCommandArgs.Add(realmArgs.Serialize());
			genBeamCommandArgs.Add("set");
			genBeamCommandArgs.Add(setArgs.Serialize());
			// Create an instance of an IBeamCommand
			Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
			// Join all the command paths and args into one string
			string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
			// Configure the command with the command string
			command.SetCommand(genBeamCommandStr);
			ConfigRealmSetWrapper genBeamCommandWrapper = new ConfigRealmSetWrapper();
			genBeamCommandWrapper.Command = command;
			// Return the command!
			return genBeamCommandWrapper;
		}
	}
	public partial class ConfigRealmSetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
	{
		public virtual ConfigRealmSetWrapper OnStreamRealmConfigOutput(System.Action<ReportDataPoint<BeamRealmConfigOutput>> cb)
		{
			this.Command.On("stream", cb);
			return this;
		}
	}
}
