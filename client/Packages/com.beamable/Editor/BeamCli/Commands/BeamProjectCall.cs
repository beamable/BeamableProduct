
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectCallArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The beamo id of the microservice to call</summary>
        public string service;
        /// <summary>The endpoint to call, usually the name of a [ClientCallable] method</summary>
        public string method;
        /// <summary>A JSON object whose keys are the endpoint's parameter names. Defaults to {}</summary>
        public string payload;
        /// <summary>Who makes the call: 'guest' creates a new guest player, anything else is used as a player refresh token (e.g. the refreshToken of an earlier call)</summary>
        public string @as;
        /// <summary>Where to route the call: 'local' uses this machine's routing key, 'remote' calls the deployed service, 'auto' calls a locally running instance when one is discovered and the deployed service otherwise</summary>
        public string target;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the service value to the list of args.
            genBeamCommandArgs.Add(this.service.ToString());
            // Add the method value to the list of args.
            genBeamCommandArgs.Add(this.method.ToString());
            // If the payload value was not default, then add it to the list of args.
            if ((this.payload != default(string)))
            {
                genBeamCommandArgs.Add(("--payload=" + this.payload));
            }
            // If the as value was not default, then add it to the list of args.
            if ((this.@as != default(string)))
            {
                genBeamCommandArgs.Add(("--as=" + this.@as));
            }
            // If the target value was not default, then add it to the list of args.
            if ((this.target != default(string)))
            {
                genBeamCommandArgs.Add(("--target=" + this.target));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectCallWrapper ProjectCall(ProjectCallArgs callArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("call");
            genBeamCommandArgs.Add(callArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectCallWrapper genBeamCommandWrapper = new ProjectCallWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectCallWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectCallWrapper OnStreamProjectCallResult(System.Action<ReportDataPoint<BeamProjectCallResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
