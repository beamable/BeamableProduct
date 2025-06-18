
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class LoginArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Specify user email address</summary>
        public string email;
        /// <summary>User password</summary>
        public string password;
        /// <summary>Save login refresh token to environment variable</summary>
        public bool saveToEnvironment;
        /// <summary>Save login refresh token to file</summary>
        public bool saveToFile;
        /// <summary>Prevent auth tokens from being saved to disk. This replaces the legacy --save-to-file option</summary>
        public bool noTokenSave;
        /// <summary>Makes the resulting access/refresh token pair be realm scoped instead of the default customer scoped one</summary>
        public bool realmScoped;
        /// <summary>A Refresh Token to use for the requests. It overwrites the logged in user stored in connection-auth.json for THIS INVOCATION ONLY</summary>
        public string refreshToken;
        /// <summary>Prints out login request response to console</summary>
        public bool printToConsole;
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
            // If the saveToEnvironment value was not default, then add it to the list of args.
            if ((this.saveToEnvironment != default(bool)))
            {
                genBeamCommandArgs.Add(("--save-to-environment=" + this.saveToEnvironment));
            }
            // If the saveToFile value was not default, then add it to the list of args.
            if ((this.saveToFile != default(bool)))
            {
                genBeamCommandArgs.Add(("--save-to-file=" + this.saveToFile));
            }
            // If the noTokenSave value was not default, then add it to the list of args.
            if ((this.noTokenSave != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-token-save=" + this.noTokenSave));
            }
            // If the realmScoped value was not default, then add it to the list of args.
            if ((this.realmScoped != default(bool)))
            {
                genBeamCommandArgs.Add(("--realm-scoped=" + this.realmScoped));
            }
            // If the refreshToken value was not default, then add it to the list of args.
            if ((this.refreshToken != default(string)))
            {
                genBeamCommandArgs.Add(("--refresh-token=" + this.refreshToken));
            }
            // If the printToConsole value was not default, then add it to the list of args.
            if ((this.printToConsole != default(bool)))
            {
                genBeamCommandArgs.Add(("--print-to-console=" + this.printToConsole));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual LoginWrapper Login(LoginArgs loginArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("login");
            genBeamCommandArgs.Add(loginArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            LoginWrapper genBeamCommandWrapper = new LoginWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class LoginWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual LoginWrapper OnErrorLoginFailedError(System.Action<ReportDataPoint<BeamLoginFailedError>> cb)
        {
            this.Command.On("errorLoginFailedError", cb);
            return this;
        }
        public virtual LoginWrapper OnStreamLoginResults(System.Action<ReportDataPoint<BeamLoginResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
