
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectRegenerateArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Name of the new project</summary>
        public Beamable.Common.Semantics.ServiceName name;
        /// <summary>Where the temp project will be created</summary>
        public string output;
        /// <summary>The path to where the files will be copied to</summary>
        public string copyPath;
        /// <summary>Specifies version of Beamable project dependencies. Defaults to the current version of the CLI</summary>
        public Beamable.Common.PackageVersion version;
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. By default, when no value is provided, the .sln path will be <name>/<name>.sln</summary>
        public string sln;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the name value to the list of args.
            genBeamCommandArgs.Add(this.name.ToString());
            // If the output value was not default, then add it to the list of args.
            if ((this.output != default(string)))
            {
                genBeamCommandArgs.Add(this.output.ToString());
            }
            // If the copyPath value was not default, then add it to the list of args.
            if ((this.copyPath != default(string)))
            {
                genBeamCommandArgs.Add(this.copyPath.ToString());
            }
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(Beamable.Common.PackageVersion)))
            {
                genBeamCommandArgs.Add(("--version=" + this.version));
            }
            // If the sln value was not default, then add it to the list of args.
            if ((this.sln != default(string)))
            {
                genBeamCommandArgs.Add((("--sln=\"" + this.sln) 
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
        public virtual ProjectRegenerateWrapper ProjectRegenerate(ProjectRegenerateArgs regenerateArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("regenerate");
            genBeamCommandArgs.Add(regenerateArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectRegenerateWrapper genBeamCommandWrapper = new ProjectRegenerateWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProjectRegenerateWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
