
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ListenServerArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>When true, do not send any approved list of messages, such that all server messages will be sent</summary>
        public bool noFilter;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the noFilter value was not default, then add it to the list of args.
            if ((this.noFilter != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-filter=" + this.noFilter));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ListenServerWrapper ListenServer(ListenServerArgs serverArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("listen");
            genBeamCommandArgs.Add("server");
            genBeamCommandArgs.Add(serverArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ListenServerWrapper genBeamCommandWrapper = new ListenServerWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ListenServerWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ListenServerWrapper OnStreamNotificationServerOutput(System.Action<ReportDataPoint<BeamNotificationServerOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
