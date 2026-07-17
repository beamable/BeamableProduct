
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectStopArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The list of services to include, defaults to all local services (separated by whitespace). To use NO services, use the --exact-ids flag</summary>
        public string[] ids;
        /// <summary>By default, a blank --ids option maps to ALL available ids. When the --exact-ids flag is given, a blank --ids option maps to NO ids</summary>
        public bool exactIds;
        /// <summary>Kill the task instead of sending a graceful shutdown signal via the socket</summary>
        public bool killTask;
        /// <summary>The reason for stopping the service</summary>
        public string reason;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the ids value was not default, then add it to the list of args.
            if ((this.ids != default(string[])))
            {
                for (int i = 0; (i < this.ids.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--ids=" + this.ids[i]));
                }
            }
            // If the exactIds value was not default, then add it to the list of args.
            if ((this.exactIds != default(bool)))
            {
                genBeamCommandArgs.Add(("--exact-ids=" + this.exactIds));
            }
            // If the killTask value was not default, then add it to the list of args.
            if ((this.killTask != default(bool)))
            {
                genBeamCommandArgs.Add(("--kill-task=" + this.killTask));
            }
            // If the reason value was not default, then add it to the list of args.
            if ((this.reason != default(string)))
            {
                genBeamCommandArgs.Add(("--reason=" + this.reason));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectStopWrapper ProjectStop(ProjectStopArgs stopArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("stop");
            genBeamCommandArgs.Add(stopArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectStopWrapper genBeamCommandWrapper = new ProjectStopWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectStopWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectStopWrapper OnStreamStopProjectCommandOutput(System.Action<ReportDataPoint<BeamStopProjectCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
