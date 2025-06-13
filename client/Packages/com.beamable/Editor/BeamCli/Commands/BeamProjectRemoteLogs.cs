
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectRemoteLogsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The beamo id for the service to get logs for</summary>
        public string serviceId;
        /// <summary>A text filter for log searching</summary>
        public string filter;
        /// <summary>A log level filter for searching</summary>
        public string serverLogLevel;
        /// <summary>A timestamp filter, where logs must be newer than this time. Must be an exact date time string, or a relative time string. Relative time strings are in the format <number><unit>. The unit is either s (seconds), m (minutes), h (hours), or d (days). To represent 5 minutes in the past, use the term '5m' </summary>
        public string from;
        /// <summary>A timestamp filter, where logs must be older than this time. Must be an exact date time string, or a relative time string. Relative time strings are in the format <number><unit>. The unit is either s (seconds), m (minutes), h (hours), or d (days). To represent 5 minutes in the past, use the term '5m' </summary>
        public string to;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the serviceId value to the list of args.
            genBeamCommandArgs.Add(this.serviceId.ToString());
            // If the filter value was not default, then add it to the list of args.
            if ((this.filter != default(string)))
            {
                genBeamCommandArgs.Add(("--filter=" + this.filter));
            }
            // If the serverLogLevel value was not default, then add it to the list of args.
            if ((this.serverLogLevel != default(string)))
            {
                genBeamCommandArgs.Add(("--server-log-level=" + this.serverLogLevel));
            }
            // If the from value was not default, then add it to the list of args.
            if ((this.from != default(string)))
            {
                genBeamCommandArgs.Add(("--from=" + this.from));
            }
            // If the to value was not default, then add it to the list of args.
            if ((this.to != default(string)))
            {
                genBeamCommandArgs.Add(("--to=" + this.to));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectRemoteLogsWrapper ProjectRemoteLogs(ProjectRemoteLogsArgs remoteLogsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("remote-logs");
            genBeamCommandArgs.Add(remoteLogsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectRemoteLogsWrapper genBeamCommandWrapper = new ProjectRemoteLogsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectRemoteLogsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectRemoteLogsWrapper OnStreamTailLogMessageForClient(System.Action<ReportDataPoint<BeamTailLogMessageForClient>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
