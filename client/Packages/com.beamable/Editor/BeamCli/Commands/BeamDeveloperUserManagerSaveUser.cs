
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeveloperUserManagerSaveUserArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The access token to be saved</summary>
        public string[] accessToken;
        /// <summary>The refresh token to be saved</summary>
        public string[] refreshToken;
        /// <summary>The PID of the user</summary>
        public string[] pid;
        /// <summary>The CID of the user</summary>
        public string[] cid;
        /// <summary>The Gamer Tag of the user</summary>
        public string[] gamerTag;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the accessToken value was not default, then add it to the list of args.
            if ((this.accessToken != default(string[])))
            {
                for (int i = 0; (i < this.accessToken.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--access-token=" + this.accessToken[i]));
                }
            }
            // If the refreshToken value was not default, then add it to the list of args.
            if ((this.refreshToken != default(string[])))
            {
                for (int i = 0; (i < this.refreshToken.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--refresh-token=" + this.refreshToken[i]));
                }
            }
            // If the pid value was not default, then add it to the list of args.
            if ((this.pid != default(string[])))
            {
                for (int i = 0; (i < this.pid.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--pid=" + this.pid[i]));
                }
            }
            // If the cid value was not default, then add it to the list of args.
            if ((this.cid != default(string[])))
            {
                for (int i = 0; (i < this.cid.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--cid=" + this.cid[i]));
                }
            }
            // If the gamerTag value was not default, then add it to the list of args.
            if ((this.gamerTag != default(string[])))
            {
                for (int i = 0; (i < this.gamerTag.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--gamer-tag=" + this.gamerTag[i]));
                }
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual DeveloperUserManagerSaveUserWrapper DeveloperUserManagerSaveUser(DeveloperUserManagerSaveUserArgs saveUserArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("developer-user-manager");
            genBeamCommandArgs.Add("save-user");
            genBeamCommandArgs.Add(saveUserArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeveloperUserManagerSaveUserWrapper genBeamCommandWrapper = new DeveloperUserManagerSaveUserWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeveloperUserManagerSaveUserWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeveloperUserManagerSaveUserWrapper OnStreamDeveloperUserResult(System.Action<ReportDataPoint<BeamDeveloperUserResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
