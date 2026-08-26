
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class InitGetRealmsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Specify user email address</summary>
        public string email;
        /// <summary>User password</summary>
        public string password;
        /// <summary>A Refresh Token to use for the requests. It overwrites the logged in user stored in auth.beam.json for THIS INVOCATION ONLY</summary>
        public string refreshToken;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the email value was not default, then add it to the list of args.
            if ((this.email != default(string)))
            {
                genBeamCommandArgs.Add(("--email=" + this.email));
            }
            // If the password value was not default, then add it to the list of args.
            if ((this.password != default(string)))
            {
                genBeamCommandArgs.Add(("--password=" + this.password));
            }
            // If the refreshToken value was not default, then add it to the list of args.
            if ((this.refreshToken != default(string)))
            {
                genBeamCommandArgs.Add(("--refresh-token=" + this.refreshToken));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual InitGetRealmsWrapper InitGetRealms(InitArgs initArgs, InitGetRealmsArgs getRealmsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("init");
            genBeamCommandArgs.Add(initArgs.Serialize());
            genBeamCommandArgs.Add("get-realms");
            genBeamCommandArgs.Add(getRealmsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            InitGetRealmsWrapper genBeamCommandWrapper = new InitGetRealmsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class InitGetRealmsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual InitGetRealmsWrapper OnStreamInitGetRealmsResult(System.Action<ReportDataPoint<BeamInitGetRealmsResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
