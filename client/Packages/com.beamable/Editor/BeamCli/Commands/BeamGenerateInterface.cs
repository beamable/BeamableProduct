
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class GenerateInterfaceArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>When true, all the generated code will be in one file. When false, there will be multiple files</summary>
        public bool concat;
        /// <summary>When null or empty, the generated code will be sent to standard-out. When there is a output value, the file or files will be written to the path</summary>
        public string output;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the concat value was not default, then add it to the list of args.
            if ((this.concat != default(bool)))
            {
                genBeamCommandArgs.Add(("--concat=" + this.concat));
            }
            // If the output value was not default, then add it to the list of args.
            if ((this.output != default(string)))
            {
                genBeamCommandArgs.Add(("--output=" + this.output));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual Beamable.Common.BeamCli.IBeamCommand GenerateInterface(GenerateInterfaceArgs generateInterfaceArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("generate-interface");
            genBeamCommandArgs.Add(generateInterfaceArgs.Serialize());
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
