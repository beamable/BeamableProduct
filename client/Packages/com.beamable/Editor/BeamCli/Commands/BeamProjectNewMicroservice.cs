
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectNewMicroserviceArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Name of the new project</summary>
        public Beamable.Common.Semantics.ServiceName name;
        /// <summary>Relative path to current directory where new solution should be created.</summary>
        public string newSolutionDirectory;
        /// <summary>Relative path to current solution file to which standalone microservice should be added.</summary>
        public string existingSolutionFile;
        /// <summary>If you should create a common library</summary>
        public bool skipCommon;
        /// <summary>The name of the solution of the new project. Use it if you want to create a new solution.</summary>
        public Beamable.Common.Semantics.ServiceName newSolutionName;
        /// <summary>Relative path to directory where microservice should be created. Defaults to "SOLUTION_DIR/services"</summary>
        public string serviceDirectory;
        /// <summary>Specifies version of Beamable project dependencies</summary>
        public string version;
        /// <summary>Created service by default would not be published</summary>
        public bool disable;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the name value to the list of args.
            genBeamCommandArgs.Add(this.name.ToString());
            // If the newSolutionDirectory value was not default, then add it to the list of args.
            if ((this.newSolutionDirectory != default(string)))
            {
                genBeamCommandArgs.Add((("--new-solution-directory=\"" + this.newSolutionDirectory) 
                                + "\""));
            }
            // If the existingSolutionFile value was not default, then add it to the list of args.
            if ((this.existingSolutionFile != default(string)))
            {
                genBeamCommandArgs.Add((("--existing-solution-file=\"" + this.existingSolutionFile) 
                                + "\""));
            }
            // If the skipCommon value was not default, then add it to the list of args.
            if ((this.skipCommon != default(bool)))
            {
                genBeamCommandArgs.Add(("--skip-common=" + this.skipCommon));
            }
            // If the newSolutionName value was not default, then add it to the list of args.
            if ((this.newSolutionName != default(Beamable.Common.Semantics.ServiceName)))
            {
                genBeamCommandArgs.Add(("--new-solution-name=" + this.newSolutionName));
            }
            // If the serviceDirectory value was not default, then add it to the list of args.
            if ((this.serviceDirectory != default(string)))
            {
                genBeamCommandArgs.Add((("--service-directory=\"" + this.serviceDirectory) 
                                + "\""));
            }
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(string)))
            {
                genBeamCommandArgs.Add((("--version=\"" + this.version) 
                                + "\""));
            }
            // If the disable value was not default, then add it to the list of args.
            if ((this.disable != default(bool)))
            {
                genBeamCommandArgs.Add(("--disable=" + this.disable));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectNewMicroserviceWrapper ProjectNewMicroservice(ProjectNewMicroserviceArgs microserviceArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("new");
            genBeamCommandArgs.Add("microservice");
            genBeamCommandArgs.Add(microserviceArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectNewMicroserviceWrapper genBeamCommandWrapper = new ProjectNewMicroserviceWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProjectNewMicroserviceWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
