
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TelemetryCollectorPsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>When true, the command will run forever and watch the state of the program</summary>
        public bool watch;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
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
        public virtual TelemetryCollectorPsWrapper TelemetryCollectorPs(TelemetryCollectorPsArgs psArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("telemetry");
            genBeamCommandArgs.Add("collector");
            genBeamCommandArgs.Add("ps");
            genBeamCommandArgs.Add(psArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TelemetryCollectorPsWrapper genBeamCommandWrapper = new TelemetryCollectorPsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TelemetryCollectorPsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TelemetryCollectorPsWrapper OnStreamCollectorStatusResult(System.Action<ReportDataPoint<BeamCollectorStatusResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual TelemetryCollectorPsWrapper OnExtraStreamOtelFileStatus(System.Action<ReportDataPoint<Beamable.Common.BeamCli.Contracts.OtelFileStatus>> cb)
        {
            this.Command.On("extraStream", cb);
            return this;
        }
    }
}
