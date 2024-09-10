
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectNewServiceArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Name of the new project</summary>
        public Beamable.Common.Semantics.ServiceName name;
        /// <summary>Automatically create a .beamable folder context if no context exists</summary>
        public bool init;
        /// <summary>Relative path to the .sln file to use for the new project. If the .sln file does not exist, it will be created. When no option is configured, if this command is executing inside a .beamable folder, then the first .sln found in .beamable/.. will be used. If no .sln is found, the .sln path will be <name>.sln. If no .beamable folder exists, then the <project>/<project>.sln will be used</summary>
        public string sln;
        /// <summary>Relative path to directory where project should be created. Defaults to "SOLUTION_DIR/services"</summary>
        public string serviceDirectory;
        /// <summary>If passed, will create a common library for this project</summary>
        public bool generateCommon;
        /// <summary>INTERNAL This enables a sane workflow for beamable developers to be happy and productive</summary>
        public bool beamableDev;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the name value to the list of args.
            genBeamCommandArgs.Add(this.name.ToString());
            // If the init value was not default, then add it to the list of args.
            if ((this.init != default(bool)))
            {
                genBeamCommandArgs.Add(("--init=" + this.init));
            }
            // If the sln value was not default, then add it to the list of args.
            if ((this.sln != default(string)))
            {
                genBeamCommandArgs.Add((("--sln=\"" + this.sln) 
                                + "\""));
            }
            // If the serviceDirectory value was not default, then add it to the list of args.
            if ((this.serviceDirectory != default(string)))
            {
                genBeamCommandArgs.Add((("--service-directory=\"" + this.serviceDirectory) 
                                + "\""));
            }
            // If the generateCommon value was not default, then add it to the list of args.
            if ((this.generateCommon != default(bool)))
            {
                genBeamCommandArgs.Add(("--generate-common=" + this.generateCommon));
            }
            // If the beamableDev value was not default, then add it to the list of args.
            if ((this.beamableDev != default(bool)))
            {
                genBeamCommandArgs.Add(("--beamable-dev=" + this.beamableDev));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectNewServiceWrapper ProjectNewService(ProjectNewServiceArgs serviceArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("new");
            genBeamCommandArgs.Add("service");
            genBeamCommandArgs.Add(serviceArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectNewServiceWrapper genBeamCommandWrapper = new ProjectNewServiceWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProjectNewServiceWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
