
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TelemetryLogsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Value to be matched with the log message body</summary>
        public string filter;
        /// <summary>Filter logs by doing a full match with the service name</summary>
        public string id;
        /// <summary>Filter logs by doing a full match with the Log Level. Available values are: ["Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"]</summary>
        public string logLevel;
        /// <summary>Sets a max number of rows to be retrieved by this command</summary>
        public int limit;
        /// <summary>If set, this will make the body message match be a full exact match</summary>
        public bool fullMatch;
        /// <summary>The amount of time to go back and retrieve logs. Examples: 12d (12 days), 5m (5 minutes), 48h (48 hours)</summary>
        public string from;
        /// <summary>If set, this will force the order to be ascending instead of the default descending order</summary>
        public bool ascending;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the filter value was not default, then add it to the list of args.
            if ((this.filter != default(string)))
            {
                genBeamCommandArgs.Add(this.filter.ToString());
            }
            // If the id value was not default, then add it to the list of args.
            if ((this.id != default(string)))
            {
                genBeamCommandArgs.Add(("--id=" + this.id));
            }
            // If the logLevel value was not default, then add it to the list of args.
            if ((this.logLevel != default(string)))
            {
                genBeamCommandArgs.Add(("--log-level=" + this.logLevel));
            }
            // If the limit value was not default, then add it to the list of args.
            if ((this.limit != default(int)))
            {
                genBeamCommandArgs.Add(("--limit=" + this.limit));
            }
            // If the fullMatch value was not default, then add it to the list of args.
            if ((this.fullMatch != default(bool)))
            {
                genBeamCommandArgs.Add(("--full-match=" + this.fullMatch));
            }
            // If the from value was not default, then add it to the list of args.
            if ((this.from != default(string)))
            {
                genBeamCommandArgs.Add(("--from=" + this.from));
            }
            // If the ascending value was not default, then add it to the list of args.
            if ((this.ascending != default(bool)))
            {
                genBeamCommandArgs.Add(("--ascending=" + this.ascending));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual TelemetryLogsWrapper TelemetryLogs(TelemetryLogsArgs logsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("telemetry");
            genBeamCommandArgs.Add("logs");
            genBeamCommandArgs.Add(logsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TelemetryLogsWrapper genBeamCommandWrapper = new TelemetryLogsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TelemetryLogsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TelemetryLogsWrapper OnStreamFetchTelemetryLogsResult(System.Action<ReportDataPoint<BeamFetchTelemetryLogsResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
