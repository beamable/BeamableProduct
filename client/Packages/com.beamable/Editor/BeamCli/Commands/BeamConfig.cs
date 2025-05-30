
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ConfigArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Whether this command should ignore the local config overrides</summary>
        public bool noOverrides;
        /// <summary>When true, whatever '--host', '--cid', '--pid' values you provide will be set. If '--no-overrides' is true, this will set the version controlled configuration file. If not, this will set the local overrides file inside the .beamable/temp directory</summary>
        public bool set;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the noOverrides value was not default, then add it to the list of args.
            if ((this.noOverrides != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-overrides=" + this.noOverrides));
            }
            // If the set value was not default, then add it to the list of args.
            if ((this.set != default(bool)))
            {
                genBeamCommandArgs.Add(("--set=" + this.set));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ConfigWrapper Config(ConfigArgs configArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("config");
            genBeamCommandArgs.Add(configArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ConfigWrapper genBeamCommandWrapper = new ConfigWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ConfigWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ConfigWrapper OnStreamConfigCommandResult(System.Action<ReportDataPoint<BeamConfigCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
