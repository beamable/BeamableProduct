
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class LocalPsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Path to the manifest whose run-state to read (defaults to .beamable/local-stack.json)</summary>
        public string config;
        /// <summary>Continuously re-render the status until cancelled</summary>
        public bool watch;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the config value was not default, then add it to the list of args.
            if ((this.config != default(string)))
            {
                genBeamCommandArgs.Add(("--config=" + this.config));
            }
            // If the watch value was not default, then add it to the list of args.
            if ((this.watch != default(bool)))
            {
                genBeamCommandArgs.Add(("--watch=" + this.watch));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual LocalPsWrapper LocalPs(LocalPsArgs psArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("local");
            genBeamCommandArgs.Add("ps");
            genBeamCommandArgs.Add(psArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            LocalPsWrapper genBeamCommandWrapper = new LocalPsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class LocalPsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual LocalPsWrapper OnStreamLocalStackPsCommandResult(System.Action<ReportDataPoint<BeamLocalStackPsCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
