
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProfileRunNbomberArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The name of the microservice to stress test</summary>
        public string service;
        /// <summary>The method name in the service to stress test</summary>
        public string method;
        /// <summary>The json body for each request</summary>
        public string body;
        /// <summary>If true, the generated .env file will include the local machine name as prefix</summary>
        public bool includePrefix;
        /// <summary>The requested requests per second for the test</summary>
        public int rps;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the service value to the list of args.
            genBeamCommandArgs.Add(this.service);
            // Add the method value to the list of args.
            genBeamCommandArgs.Add(this.method);
            // Add the body value to the list of args.
            genBeamCommandArgs.Add(this.body);
            // If the includePrefix value was not default, then add it to the list of args.
            if ((this.includePrefix != default(bool)))
            {
                genBeamCommandArgs.Add(("--include-prefix=" + this.includePrefix));
            }
            // If the rps value was not default, then add it to the list of args.
            if ((this.rps != default(int)))
            {
                genBeamCommandArgs.Add(("--rps=" + this.rps));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual Beamable.Common.BeamCli.IBeamCommand ProfileRunNbomber(ProfileRunNbomberArgs runNbomberArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("profile");
            genBeamCommandArgs.Add("run-nbomber");
            genBeamCommandArgs.Add(runNbomberArgs.Serialize());
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
