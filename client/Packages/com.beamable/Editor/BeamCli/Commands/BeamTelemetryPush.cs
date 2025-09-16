
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TelemetryPushArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The endpoint to which the telemetry data should be exported</summary>
        public string endpoint;
        /// <summary>Defines the process Id that called this method. If is not passed a new process ID will be generated</summary>
        public string processId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the endpoint value was not default, then add it to the list of args.
            if ((this.endpoint != default(string)))
            {
                genBeamCommandArgs.Add(("--endpoint=" + this.endpoint));
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
        public virtual TelemetryPushWrapper TelemetryPush(TelemetryPushArgs pushArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("telemetry");
            genBeamCommandArgs.Add("push");
            genBeamCommandArgs.Add(pushArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TelemetryPushWrapper genBeamCommandWrapper = new TelemetryPushWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TelemetryPushWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
