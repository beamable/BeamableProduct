
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class InitArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>the folder that will be initialized as a beamable project. </summary>
        public string path;
        /// <summary>Specify user email address</summary>
        public string email;
        /// <summary>User password</summary>
        public string password;
        /// <summary>The host endpoint for beamable</summary>
        public string host;
        /// <summary>Cid to use; will default to whatever is in the file system</summary>
        public string cid;
        /// <summary>Pid to use; will default to whatever is in the file system</summary>
        public string pid;
        /// <summary>Refresh token to use for the requests</summary>
        public string refreshToken;
        /// <summary>Overwrite the stored extra paths for where to find projects</summary>
        public string[] saveExtraPaths;
        /// <summary>Paths to ignore when searching for services</summary>
        public string[] pathsToIgnore;
        /// <summary>Save login refresh token to environment variable</summary>
        public bool saveToEnvironment;
        /// <summary>Save login refresh token to file</summary>
        public bool saveToFile;
        /// <summary>Prevent auth tokens from being saved to disk. This replaces the legacy --save-to-file option</summary>
        public bool noTokenSave;
        /// <summary>Make request customer scoped instead of product only</summary>
        public bool customerScoped;
        /// <summary>Prints out login request response to console</summary>
        public bool printToConsole;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the path value was not default, then add it to the list of args.
            if ((this.path != default(string)))
            {
                genBeamCommandArgs.Add(this.path.ToString());
            }
            // If the email value was not default, then add it to the list of args.
            if ((this.email != default(string)))
            {
                genBeamCommandArgs.Add((("--email=\"" + this.email) 
                                + "\""));
            }
            // If the password value was not default, then add it to the list of args.
            if ((this.password != default(string)))
            {
                genBeamCommandArgs.Add((("--password=\"" + this.password) 
                                + "\""));
            }
            // If the host value was not default, then add it to the list of args.
            if ((this.host != default(string)))
            {
                genBeamCommandArgs.Add((("--host=\"" + this.host) 
                                + "\""));
            }
            // If the cid value was not default, then add it to the list of args.
            if ((this.cid != default(string)))
            {
                genBeamCommandArgs.Add((("--cid=\"" + this.cid) 
                                + "\""));
            }
            // If the pid value was not default, then add it to the list of args.
            if ((this.pid != default(string)))
            {
                genBeamCommandArgs.Add((("--pid=\"" + this.pid) 
                                + "\""));
            }
            // If the refreshToken value was not default, then add it to the list of args.
            if ((this.refreshToken != default(string)))
            {
                genBeamCommandArgs.Add((("--refresh-token=\"" + this.refreshToken) 
                                + "\""));
            }
            // If the saveExtraPaths value was not default, then add it to the list of args.
            if ((this.saveExtraPaths != default(string[])))
            {
                for (int i = 0; (i < this.saveExtraPaths.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--save-extra-paths=" + this.saveExtraPaths[i]));
                }
            }
            // If the pathsToIgnore value was not default, then add it to the list of args.
            if ((this.pathsToIgnore != default(string[])))
            {
                for (int i = 0; (i < this.pathsToIgnore.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--paths-to-ignore=" + this.pathsToIgnore[i]));
                }
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
            // If the customerScoped value was not default, then add it to the list of args.
            if ((this.customerScoped != default(bool)))
            {
                genBeamCommandArgs.Add(("--customer-scoped=" + this.customerScoped));
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
        public virtual InitWrapper Init(InitArgs initArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("init");
            genBeamCommandArgs.Add(initArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            InitWrapper genBeamCommandWrapper = new InitWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class InitWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual InitWrapper OnStreamInitCommandResult(System.Action<ReportDataPoint<BeamInitCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
