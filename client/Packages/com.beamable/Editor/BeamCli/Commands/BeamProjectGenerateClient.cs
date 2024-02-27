
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectGenerateClientArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The .dll filepath for the built microservice</summary>
        public string source;
        /// <summary>Directory to write the output client at</summary>
        public string outputDir;
        /// <summary>When true, generate the source client files to all associated projects</summary>
        public bool outputLinks;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the source value to the list of args.
            genBeamCommandArgs.Add(this.source);
            // If the outputDir value was not default, then add it to the list of args.
            if ((this.outputDir != default(string)))
            {
                genBeamCommandArgs.Add((("--output-dir=\"" + this.outputDir) 
                                + "\""));
            }
            // If the outputLinks value was not default, then add it to the list of args.
            if ((this.outputLinks != default(bool)))
            {
                genBeamCommandArgs.Add(("--output-links=" + this.outputLinks));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectGenerateClientWrapper ProjectGenerateClient(ProjectGenerateClientArgs generateClientArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("generate-client");
            genBeamCommandArgs.Add(generateClientArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectGenerateClientWrapper genBeamCommandWrapper = new ProjectGenerateClientWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProjectGenerateClientWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
