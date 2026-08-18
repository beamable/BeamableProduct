
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class LocalLogsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The step name to tail (default: all recorded steps)</summary>
        public string step;
        /// <summary>Path to the manifest whose run-state to read (defaults to .beamable/local-stack.json)</summary>
        public string config;
        /// <summary>Keep streaming new log lines until cancelled</summary>
        public bool follow;
        /// <summary>How many trailing lines of history to show first</summary>
        public int lines;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the step value was not default, then add it to the list of args.
            if ((this.step != default(string)))
            {
                genBeamCommandArgs.Add(this.step.ToString());
            }
            // If the config value was not default, then add it to the list of args.
            if ((this.config != default(string)))
            {
                genBeamCommandArgs.Add(("--config=" + this.config));
            }
            // If the follow value was not default, then add it to the list of args.
            if ((this.follow != default(bool)))
            {
                genBeamCommandArgs.Add(("--follow=" + this.follow));
            }
            // If the lines value was not default, then add it to the list of args.
            if ((this.lines != default(int)))
            {
                genBeamCommandArgs.Add(("--lines=" + this.lines));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual LocalLogsWrapper LocalLogs(LocalLogsArgs logsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("local");
            genBeamCommandArgs.Add("logs");
            genBeamCommandArgs.Add(logsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            LocalLogsWrapper genBeamCommandWrapper = new LocalLogsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class LocalLogsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual LocalLogsWrapper OnStreamLocalStackLogsCommandResult(System.Action<ReportDataPoint<BeamLocalStackLogsCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
