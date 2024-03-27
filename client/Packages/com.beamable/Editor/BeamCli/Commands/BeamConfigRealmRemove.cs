
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ConfigRealmRemoveArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>A list of realm config keys in a namespace|key format</summary>
        public string[] keys;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the keys value was not default, then add it to the list of args.
            if ((this.keys != default(string[])))
            {
                for (int i = 0; (i < this.keys.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--keys=" + this.keys[i]));
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
        public virtual ConfigRealmRemoveWrapper ConfigRealmRemove(ConfigRealmArgs realmArgs, ConfigRealmRemoveArgs removeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("config");
            genBeamCommandArgs.Add("realm");
            genBeamCommandArgs.Add(realmArgs.Serialize());
            genBeamCommandArgs.Add("remove");
            genBeamCommandArgs.Add(removeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ConfigRealmRemoveWrapper genBeamCommandWrapper = new ConfigRealmRemoveWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ConfigRealmRemoveWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ConfigRealmRemoveWrapper OnStreamRealmConfigOutput(System.Action<ReportDataPoint<BeamRealmConfigOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
