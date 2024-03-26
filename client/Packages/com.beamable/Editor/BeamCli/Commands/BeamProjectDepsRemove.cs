
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectDepsRemoveArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The microservice name that the dependency will be removed from</summary>
        public string microservice;
        /// <summary>The dependency that will be removed from that service</summary>
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
        public virtual ProjectDepsRemoveWrapper ProjectDepsRemove(ProjectDepsRemoveArgs removeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("deps");
            genBeamCommandArgs.Add("remove");
            genBeamCommandArgs.Add(removeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectDepsRemoveWrapper genBeamCommandWrapper = new ProjectDepsRemoveWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProjectDepsRemoveWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
