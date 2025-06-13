
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeploymentGetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Include archived (removed) services</summary>
        public bool showArchived;
        /// <summary>A file path to save the plan</summary>
        public string toFile;
        /// <summary>Find only the single manifest that matches the given id</summary>
        public string id;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the showArchived value was not default, then add it to the list of args.
            if ((this.showArchived != default(bool)))
            {
                genBeamCommandArgs.Add(("--show-archived=" + this.showArchived));
            }
            // If the toFile value was not default, then add it to the list of args.
            if ((this.toFile != default(string)))
            {
                genBeamCommandArgs.Add(("--to-file=" + this.toFile));
            }
            // If the id value was not default, then add it to the list of args.
            if ((this.id != default(string)))
            {
                genBeamCommandArgs.Add(("--id=" + this.id));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual DeploymentGetWrapper DeploymentGet(DeploymentGetArgs getArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("deployment");
            genBeamCommandArgs.Add("get");
            genBeamCommandArgs.Add(getArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeploymentGetWrapper genBeamCommandWrapper = new DeploymentGetWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeploymentGetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeploymentGetWrapper OnStreamGetDeploymentCommandOutput(System.Action<ReportDataPoint<BeamGetDeploymentCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
