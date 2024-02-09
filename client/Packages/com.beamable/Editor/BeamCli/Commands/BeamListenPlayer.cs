
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ListenPlayerArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>A regex to filter for notification channels</summary>
        public string context;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the context value was not default, then add it to the list of args.
            if ((this.context != default(string)))
            {
                genBeamCommandArgs.Add((("--context=\"" + this.context) 
                                + "\""));
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
    public class ListenPlayerWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ListenPlayerWrapper OnStreamNotificationPlayerOutput(System.Action<ReportDataPoint<BeamNotificationPlayerOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
