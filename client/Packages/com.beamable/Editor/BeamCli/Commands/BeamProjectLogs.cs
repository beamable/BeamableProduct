
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectLogsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The name of the service to view logs for</summary>
        public Beamable.Common.Semantics.ServiceName service;
        /// <summary>If the service stops, and reconnect is enabled, then the logs command will wait for the service to restart and then reattach to logs</summary>
        public bool reconnect;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the service value to the list of args.
            genBeamCommandArgs.Add(this.service);
            // If the reconnect value was not default, then add it to the list of args.
            if ((this.reconnect != default(bool)))
            {
                genBeamCommandArgs.Add(("--reconnect=" + this.reconnect));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectLogsWrapper ProjectLogs(ProjectLogsArgs logsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("logs");
            genBeamCommandArgs.Add(logsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectLogsWrapper genBeamCommandWrapper = new ProjectLogsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProjectLogsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectLogsWrapper OnStreamTailLogMessageForClient(System.Action<ReportDataPoint<BeamTailLogMessageForClient>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
