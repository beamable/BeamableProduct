
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectNewCommonLibArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The name of the new library project</summary>
        public Beamable.Common.Semantics.ServiceName name;
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used</summary>
        public string sln;
        /// <summary>The path where the project is going to be created</summary>
        public string outputPath;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the name value to the list of args.
            genBeamCommandArgs.Add(this.name.ToString());
            // If the sln value was not default, then add it to the list of args.
            if ((this.sln != default(string)))
            {
                genBeamCommandArgs.Add((("--sln=\"" + this.sln) 
                                + "\""));
            }
            // If the outputPath value was not default, then add it to the list of args.
            if ((this.outputPath != default(string)))
            {
                genBeamCommandArgs.Add((("--output-path=\"" + this.outputPath) 
                                + "\""));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectNewCommonLibWrapper ProjectNewCommonLib(ProjectNewCommonLibArgs commonLibArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("new");
            genBeamCommandArgs.Add("common-lib");
            genBeamCommandArgs.Add(commonLibArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectNewCommonLibWrapper genBeamCommandWrapper = new ProjectNewCommonLibWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectNewCommonLibWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
