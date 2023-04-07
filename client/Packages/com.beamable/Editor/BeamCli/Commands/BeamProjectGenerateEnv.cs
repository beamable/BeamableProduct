
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectGenerateEnvArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Where to output the .env file</summary>
        public string output;
        /// <summary>If true, the generated .env file will include the local machine name as prefix</summary>
        public bool includePrefix;
        /// <summary>How many virtual websocket connections the server will open</summary>
        public int instanceCount;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the output value to the list of args.
            genBeamCommandArgs.Add(this.output);
            // If the includePrefix value was not default, then add it to the list of args.
            if ((this.includePrefix != default(bool)))
            {
                genBeamCommandArgs.Add(("--include-prefix=" + this.includePrefix));
            }
            // If the instanceCount value was not default, then add it to the list of args.
            if ((this.instanceCount != default(int)))
            {
                genBeamCommandArgs.Add(("--instance-count=" + this.instanceCount));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual Beamable.Common.BeamCli.IBeamCommand ProjectGenerateEnv(ProjectGenerateEnvArgs generateEnvArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("generate-env");
            genBeamCommandArgs.Add(generateEnvArgs.Serialize());
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
