
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TelemetryPruneArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>If set, will delete all telemetry files</summary>
        public bool deleteAll;
        /// <summary>Can be passed to define a custom amount of days in which data should be preserved</summary>
        public int retainingDays;
        /// <summary>Defines the process Id that called this method. If is not passed a new process ID will be generated</summary>
        public string processId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the deleteAll value was not default, then add it to the list of args.
            if ((this.deleteAll != default(bool)))
            {
                genBeamCommandArgs.Add(("--delete-all=" + this.deleteAll));
            }
            // If the retainingDays value was not default, then add it to the list of args.
            if ((this.retainingDays != default(int)))
            {
                genBeamCommandArgs.Add(("--retaining-days=" + this.retainingDays));
            }
            // If the processId value was not default, then add it to the list of args.
            if ((this.processId != default(string)))
            {
                genBeamCommandArgs.Add(("--process-id=" + this.processId));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual TelemetryPruneWrapper TelemetryPrune(TelemetryPruneArgs pruneArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("telemetry");
            genBeamCommandArgs.Add("prune");
            genBeamCommandArgs.Add(pruneArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TelemetryPruneWrapper genBeamCommandWrapper = new TelemetryPruneWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TelemetryPruneWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
