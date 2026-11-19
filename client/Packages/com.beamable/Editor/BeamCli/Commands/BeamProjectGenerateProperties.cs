
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectGeneratePropertiesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Where the file will be created</summary>
        public string output;
        /// <summary>Beam path to be used. Use BEAM_SOLUTION_DIR to template in $(SolutionDir)</summary>
        public string beamPath;
        /// <summary>The solution path to be used. 
        ///The following values have special meaning and are not treated as paths... 
        ///- "DIR.PROPS" = $([System.IO.Path]::GetDirectoryName(`$(DirectoryBuildPropsPath)`)) </summary>
        public string solutionDir;
        /// <summary>A path relative to the given solution directory, that will be used to store the projects /bin and /obj directories. Note: the given path will have the project's assembly name and the bin or obj folder appended</summary>
        public string buildDir;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the output value to the list of args.
            genBeamCommandArgs.Add(this.output.ToString());
            // Add the beamPath value to the list of args.
            genBeamCommandArgs.Add(this.beamPath.ToString());
            // Add the solutionDir value to the list of args.
            genBeamCommandArgs.Add(this.solutionDir.ToString());
            // If the buildDir value was not default, then add it to the list of args.
            if ((this.buildDir != default(string)))
            {
                genBeamCommandArgs.Add(("--build-dir=" + this.buildDir));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectGeneratePropertiesWrapper ProjectGenerateProperties(ProjectGeneratePropertiesArgs generatePropertiesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("generate-properties");
            genBeamCommandArgs.Add(generatePropertiesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectGeneratePropertiesWrapper genBeamCommandWrapper = new ProjectGeneratePropertiesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectGeneratePropertiesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
