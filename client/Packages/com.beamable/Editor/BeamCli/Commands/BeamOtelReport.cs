
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OtelReportArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>All paths to files that contain custom log data to be later exported to clickhouse</summary>
        public System.Collections.Generic.List<string> paths;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the paths value was not default, then add it to the list of args.
            if ((this.paths != default(System.Collections.Generic.List<string>)))
            {
                genBeamCommandArgs.Add(("--paths=" + this.paths));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OtelReportWrapper OtelReport(OtelReportArgs reportArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("otel");
            genBeamCommandArgs.Add("report");
            genBeamCommandArgs.Add(reportArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OtelReportWrapper genBeamCommandWrapper = new OtelReportWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OtelReportWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual OtelReportWrapper OnStreamReportTelemetryResult(System.Action<ReportDataPoint<BeamReportTelemetryResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
