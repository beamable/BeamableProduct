
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OtelPushArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The endpoint to which the telemetry data should be exported</summary>
        public string endpoint;
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
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OtelPushWrapper OtelPush(OtelPushArgs pushArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("otel");
            genBeamCommandArgs.Add("push");
            genBeamCommandArgs.Add(pushArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OtelPushWrapper genBeamCommandWrapper = new OtelPushWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OtelPushWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
