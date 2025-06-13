
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TempClearLogsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Only clear logs older than a given value. This string should be in a duration format.
        ///
        /// The duration format should be a number, followed by a time unit. Valid time units include seconds (s), minutes (m), hours (h), days (d), and months(mo). Please note that the month unit is short-hand for 30 days. Here are a few examples, 
        ///	--older-than 30m (30 minutes)
        ///  --older-than 18mo (18 months)
        ///  --older-than 12d (12 days)
        ///</summary>
        public string olderThan;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the olderThan value was not default, then add it to the list of args.
            if ((this.olderThan != default(string)))
            {
                genBeamCommandArgs.Add(("--older-than=" + this.olderThan));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual TempClearLogsWrapper TempClearLogs(TempClearLogsArgs logsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("temp");
            genBeamCommandArgs.Add("clear");
            genBeamCommandArgs.Add("logs");
            genBeamCommandArgs.Add(logsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TempClearLogsWrapper genBeamCommandWrapper = new TempClearLogsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TempClearLogsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TempClearLogsWrapper OnStreamClearTempLogFilesCommandOutput(System.Action<ReportDataPoint<BeamClearTempLogFilesCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
