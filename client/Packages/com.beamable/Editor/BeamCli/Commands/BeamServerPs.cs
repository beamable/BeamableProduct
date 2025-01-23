
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ServerPsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Only match servers that match the given version</summary>
        public string version;
        /// <summary>Only match servers that match the given owner</summary>
        public string owner;
        /// <summary>Only match servers that match the given port</summary>
        public int port;
        /// <summary>Only match servers that match the given process id</summary>
        public int pid;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(string)))
            {
                genBeamCommandArgs.Add((("--version=\"" + this.version) 
                                + "\""));
            }
            // If the owner value was not default, then add it to the list of args.
            if ((this.owner != default(string)))
            {
                genBeamCommandArgs.Add((("--owner=\"" + this.owner) 
                                + "\""));
            }
            // If the port value was not default, then add it to the list of args.
            if ((this.port != default(int)))
            {
                genBeamCommandArgs.Add(("--port=" + this.port));
            }
            // If the pid value was not default, then add it to the list of args.
            if ((this.pid != default(int)))
            {
                genBeamCommandArgs.Add(("--pid=" + this.pid));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ServerPsWrapper ServerPs(ServerPsArgs psArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("server");
            genBeamCommandArgs.Add("ps");
            genBeamCommandArgs.Add(psArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServerPsWrapper genBeamCommandWrapper = new ServerPsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ServerPsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ServerPsWrapper OnStreamServerPsCommandResult(System.Action<ReportDataPoint<BeamServerPsCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
