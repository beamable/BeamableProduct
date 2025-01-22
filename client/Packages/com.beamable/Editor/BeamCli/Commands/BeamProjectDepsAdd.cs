
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectDepsAddArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The microservice name that the dependency will be added to</summary>
        public string microservice;
        /// <summary>The storage that will be a dependency of the given microservice</summary>
        public string dependency;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the microservice value to the list of args.
            genBeamCommandArgs.Add(this.microservice.ToString());
            // Add the dependency value to the list of args.
            genBeamCommandArgs.Add(this.dependency.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectDepsAddWrapper ProjectDepsAdd(ProjectDepsAddArgs addArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("deps");
            genBeamCommandArgs.Add("add");
            genBeamCommandArgs.Add(addArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectDepsAddWrapper genBeamCommandWrapper = new ProjectDepsAddWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectDepsAddWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
