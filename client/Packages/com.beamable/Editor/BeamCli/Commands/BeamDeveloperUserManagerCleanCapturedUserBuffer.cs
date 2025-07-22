
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeveloperUserManagerCleanCapturedUserBufferArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The max amount of captured users that you can have before starting to delete the oldest (0 means infinity)</summary>
        public int rollingBufferSize;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the rollingBufferSize value was not default, then add it to the list of args.
            if ((this.rollingBufferSize != default(int)))
            {
                genBeamCommandArgs.Add(("--rolling-buffer-size=" + this.rollingBufferSize));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual DeveloperUserManagerCleanCapturedUserBufferWrapper DeveloperUserManagerCleanCapturedUserBuffer(DeveloperUserManagerCleanCapturedUserBufferArgs cleanCapturedUserBufferArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("developer-user-manager");
            genBeamCommandArgs.Add("clean-captured-user-buffer");
            genBeamCommandArgs.Add(cleanCapturedUserBufferArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeveloperUserManagerCleanCapturedUserBufferWrapper genBeamCommandWrapper = new DeveloperUserManagerCleanCapturedUserBufferWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeveloperUserManagerCleanCapturedUserBufferWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeveloperUserManagerCleanCapturedUserBufferWrapper OnStreamDeveloperUserResult(System.Action<ReportDataPoint<BeamDeveloperUserResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
