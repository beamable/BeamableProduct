
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TelemetryGetCredsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Which scope to target: 'realm' (default) for the selected realm (cid.pid); 'zone' for the zone bound to it (cid.zid)</summary>
        public string scope;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the scope value was not default, then add it to the list of args.
            if ((this.scope != default(string)))
            {
                genBeamCommandArgs.Add(("--scope=" + this.scope));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual TelemetryGetCredsWrapper TelemetryGetCreds(TelemetryGetCredsArgs getCredsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("telemetry");
            genBeamCommandArgs.Add("get-creds");
            genBeamCommandArgs.Add(getCredsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TelemetryGetCredsWrapper genBeamCommandWrapper = new TelemetryGetCredsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TelemetryGetCredsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TelemetryGetCredsWrapper OnStreamGetClickhouseCredentialsResult(System.Action<ReportDataPoint<BeamGetClickhouseCredentialsResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
