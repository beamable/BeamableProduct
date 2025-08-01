
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectOpenArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used</summary>
        public string sln;
        /// <summary>Only generate the sln but do not open it</summary>
        public bool onlyGenerate;
        /// <summary>Use a solution filter that hides projects that aren't writable in a Unity project</summary>
        public bool fromUnity;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the sln value was not default, then add it to the list of args.
            if ((this.sln != default(string)))
            {
                genBeamCommandArgs.Add(("--sln=" + this.sln));
            }
            // If the onlyGenerate value was not default, then add it to the list of args.
            if ((this.onlyGenerate != default(bool)))
            {
                genBeamCommandArgs.Add(("--only-generate=" + this.onlyGenerate));
            }
            // If the fromUnity value was not default, then add it to the list of args.
            if ((this.fromUnity != default(bool)))
            {
                genBeamCommandArgs.Add(("--from-unity=" + this.fromUnity));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectOpenWrapper ProjectOpen(ProjectOpenArgs openArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("open");
            genBeamCommandArgs.Add(openArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectOpenWrapper genBeamCommandWrapper = new ProjectOpenWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectOpenWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
