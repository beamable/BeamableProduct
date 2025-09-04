
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OtelSetConfigArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The minimum Open Telemetry LogLevel to be sent to Clickhouse, this needs to be a valid LogLevel converted to string value</summary>
        public string cliLogLevel;
        /// <summary>The maximum size in bytes for saved Otel data</summary>
        public string cliTelemetryMaxSize;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the cliLogLevel value to the list of args.
            genBeamCommandArgs.Add(this.cliLogLevel.ToString());
            // Add the cliTelemetryMaxSize value to the list of args.
            genBeamCommandArgs.Add(this.cliTelemetryMaxSize.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OtelSetConfigWrapper OtelSetConfig(OtelSetConfigArgs setConfigArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("otel");
            genBeamCommandArgs.Add("set-config");
            genBeamCommandArgs.Add(setConfigArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OtelSetConfigWrapper genBeamCommandWrapper = new OtelSetConfigWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OtelSetConfigWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual OtelSetConfigWrapper OnStreamSetBeamOtelConfigCommandResults(System.Action<ReportDataPoint<BeamSetBeamOtelConfigCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
