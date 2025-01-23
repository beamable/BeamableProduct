
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectOpenSwaggerArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Name of the service to open swagger to</summary>
        public Beamable.Common.Semantics.ServiceName serviceName;
        /// <summary>The routing key for the service instance we want. If not passed, defaults to the local service</summary>
        public string routingKey;
        /// <summary>When set, enforces the routing key to be the one for the service deployed to the realm. Cannot be specified when --routing-key is also set</summary>
        public bool remote;
        /// <summary>A hint to the Portal page which tool is being used</summary>
        public string srcTool;
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
            // If the routingKey value was not default, then add it to the list of args.
            if ((this.routingKey != default(string)))
            {
                genBeamCommandArgs.Add((("--routing-key=\"" + this.routingKey) 
                                + "\""));
            }
            // If the remote value was not default, then add it to the list of args.
            if ((this.remote != default(bool)))
            {
                genBeamCommandArgs.Add(("--remote=" + this.remote));
            }
            // If the srcTool value was not default, then add it to the list of args.
            if ((this.srcTool != default(string)))
            {
                genBeamCommandArgs.Add((("--src-tool=\"" + this.srcTool) 
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
    public partial class ProjectOpenSwaggerWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
