
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ConfigRealmUseArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The realm's name (as listed by `beam org realms`) or pid</summary>
        public string realm;
        /// <summary>The game the realm belongs to, by name or pid; needed only when several games have a realm with this name</summary>
        public string game;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the realm value to the list of args.
            genBeamCommandArgs.Add(this.realm.ToString());
            // If the game value was not default, then add it to the list of args.
            if ((this.game != default(string)))
            {
                genBeamCommandArgs.Add(("--game=" + this.game));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ConfigRealmUseWrapper ConfigRealmUse(ConfigArgs configArgs, ConfigRealmArgs realmArgs, ConfigRealmUseArgs useArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("config");
            genBeamCommandArgs.Add(configArgs.Serialize());
            genBeamCommandArgs.Add("realm");
            genBeamCommandArgs.Add(realmArgs.Serialize());
            genBeamCommandArgs.Add("use");
            genBeamCommandArgs.Add(useArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ConfigRealmUseWrapper genBeamCommandWrapper = new ConfigRealmUseWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ConfigRealmUseWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ConfigRealmUseWrapper OnStreamRealmConfigUseCommandResult(System.Action<ReportDataPoint<BeamRealmConfigUseCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
