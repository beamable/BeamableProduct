
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TelemetryConfigArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual TelemetryConfigWrapper TelemetryConfig()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("telemetry");
            genBeamCommandArgs.Add("config");
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TelemetryConfigWrapper genBeamCommandWrapper = new TelemetryConfigWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TelemetryConfigWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TelemetryConfigWrapper OnStreamGetBeamOtelConfigCommandResult(System.Action<ReportDataPoint<BeamGetBeamOtelConfigCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
