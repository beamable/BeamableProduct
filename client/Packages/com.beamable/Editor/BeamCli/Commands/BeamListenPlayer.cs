
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ListenPlayerArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>A regex to filter for notification channels</summary>
        public string context;
        /// <summary>Authenticate as a newly created guest player instead of the logged-in identity. --probe also uses a guest when no one is logged in</summary>
        public bool guest;
        /// <summary>Instead of listening, open one realtime session the way the Web SDK does, report the handshake, session-start and frames on the probe channel, then exit</summary>
        public bool probe;
        /// <summary>How many seconds --probe listens for frames after the socket opens</summary>
        public int probeSeconds;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the context value was not default, then add it to the list of args.
            if ((this.context != default(string)))
            {
                genBeamCommandArgs.Add(("--context=" + this.context));
            }
            // If the guest value was not default, then add it to the list of args.
            if ((this.guest != default(bool)))
            {
                genBeamCommandArgs.Add(("--guest=" + this.guest));
            }
            // If the probe value was not default, then add it to the list of args.
            if ((this.probe != default(bool)))
            {
                genBeamCommandArgs.Add(("--probe=" + this.probe));
            }
            // If the probeSeconds value was not default, then add it to the list of args.
            if ((this.probeSeconds != default(int)))
            {
                genBeamCommandArgs.Add(("--probe-seconds=" + this.probeSeconds));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ListenPlayerWrapper ListenPlayer(ListenPlayerArgs playerArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("listen");
            genBeamCommandArgs.Add("player");
            genBeamCommandArgs.Add(playerArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ListenPlayerWrapper genBeamCommandWrapper = new ListenPlayerWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ListenPlayerWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ListenPlayerWrapper OnStreamNotificationPlayerOutput(System.Action<ReportDataPoint<BeamNotificationPlayerOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ListenPlayerWrapper OnProbeListenPlayerProbeResult(System.Action<ReportDataPoint<BeamListenPlayerProbeResult>> cb)
        {
            this.Command.On("probe", cb);
            return this;
        }
    }
}
