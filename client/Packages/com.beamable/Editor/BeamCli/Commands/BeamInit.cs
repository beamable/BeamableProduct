
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class InitArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Specify user name</summary>
        public string username;
        /// <summary>User password</summary>
        public string password;
        /// <summary>Save login refresh token to environment variable</summary>
        public bool saveToEnvironment;
        /// <summary>Save login refresh token to file</summary>
        public bool saveToFile;
        /// <summary>Make request customer scoped instead of product only</summary>
        public bool customerScoped;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the username value was not default, then add it to the list of args.
            if ((this.username != default(string)))
            {
                genBeamCommandArgs.Add(("--username=" + this.username));
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
            // If the customerScoped value was not default, then add it to the list of args.
            if ((this.customerScoped != default(bool)))
            {
                genBeamCommandArgs.Add(("--customer-scoped=" + this.customerScoped));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual Beamable.Common.BeamCli.IBeamCommand Init(InitArgs initArgs)
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
            // Return the command!
            return command;
        }
    }
}
