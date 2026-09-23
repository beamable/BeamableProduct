
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ConfigRealmCheckArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The client to check the realm for. Valid values are: `web`</summary>
        public string @for;
        /// <summary>Fix what can be fixed: set the realtime publisher to beamable and publish the global content manifest</summary>
        public bool fix;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the for value was not default, then add it to the list of args.
            if ((this.@for != default(string)))
            {
                genBeamCommandArgs.Add(("--for=" + this.@for));
            }
            // If the fix value was not default, then add it to the list of args.
            if ((this.fix != default(bool)))
            {
                genBeamCommandArgs.Add(("--fix=" + this.fix));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ConfigRealmCheckWrapper ConfigRealmCheck(ConfigArgs configArgs, ConfigRealmArgs realmArgs, ConfigRealmCheckArgs checkArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("config");
            genBeamCommandArgs.Add(configArgs.Serialize());
            genBeamCommandArgs.Add("realm");
            genBeamCommandArgs.Add(realmArgs.Serialize());
            genBeamCommandArgs.Add("check");
            genBeamCommandArgs.Add(checkArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ConfigRealmCheckWrapper genBeamCommandWrapper = new ConfigRealmCheckWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ConfigRealmCheckWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ConfigRealmCheckWrapper OnStreamRealmCheckResult(System.Action<ReportDataPoint<BeamRealmCheckResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
