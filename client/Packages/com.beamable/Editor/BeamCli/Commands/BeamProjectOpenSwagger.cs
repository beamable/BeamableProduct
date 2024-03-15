
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class ProjectOpenSwaggerArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Name of the service to open swagger to</summary>
        public Beamable.Common.Semantics.ServiceName serviceName;
        /// <summary>If passed, swagger will open to the remote version of this service. Otherwise, it will try and use the local version</summary>
        public bool remote;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the serviceName value was not default, then add it to the list of args.
            if ((this.serviceName != default(Beamable.Common.Semantics.ServiceName)))
            {
                genBeamCommandArgs.Add(this.serviceName.ToString());
            }
            // If the remote value was not default, then add it to the list of args.
            if ((this.remote != default(bool)))
            {
                genBeamCommandArgs.Add(("--remote=" + this.remote));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectOpenSwaggerWrapper ProjectOpenSwagger(ProjectOpenSwaggerArgs openSwaggerArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("open-swagger");
            genBeamCommandArgs.Add(openSwaggerArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectOpenSwaggerWrapper genBeamCommandWrapper = new ProjectOpenSwaggerWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class ProjectOpenSwaggerWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
