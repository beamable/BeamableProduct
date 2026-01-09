
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectRunArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>When true, the command will run forever and watch the state of the program</summary>
        public bool watch;
        /// <summary>The list of services to include, defaults to all local services (separated by whitespace). To use NO services, use the --exact-ids flag</summary>
        public string[] ids;
        /// <summary>By default, a blank --ids option maps to ALL available ids. When the --exact-ids flag is given, a blank --ids option maps to NO ids</summary>
        public bool exactIds;
        /// <summary>A set of BeamServiceGroup tags that will exclude the associated services. Exclusion takes precedence over inclusion</summary>
        public string[] withoutGroup;
        /// <summary>A set of BeamServiceGroup tags that will include the associated services</summary>
        public string[] withGroup;
        /// <summary>With this flag, we restart any running services. Without it, we skip running services</summary>
        public bool force;
        /// <summary>With this flag, the service will run the background after it has reached basic startup</summary>
        public bool detach;
        /// <summary>We compile services that need compiling before running. This will disable the client-code generation part of the compilation</summary>
        public bool noClientGen;
        /// <summary>Forwards the given process-id to the BEAM_REQUIRE_PROCESS_ID environment variable of the running Microservice. The Microservice will self-destruct if the given process exits</summary>
        public int requireProcessId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the watch value was not default, then add it to the list of args.
            if ((this.watch != default(bool)))
            {
                genBeamCommandArgs.Add(("--watch=" + this.watch));
            }
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
            // If the withoutGroup value was not default, then add it to the list of args.
            if ((this.withoutGroup != default(string[])))
            {
                for (int i = 0; (i < this.withoutGroup.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--without-group=" + this.withoutGroup[i]));
                }
            }
            // If the withGroup value was not default, then add it to the list of args.
            if ((this.withGroup != default(string[])))
            {
                for (int i = 0; (i < this.withGroup.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--with-group=" + this.withGroup[i]));
                }
            }
            // If the force value was not default, then add it to the list of args.
            if ((this.force != default(bool)))
            {
                genBeamCommandArgs.Add(("--force=" + this.force));
            }
            // If the detach value was not default, then add it to the list of args.
            if ((this.detach != default(bool)))
            {
                genBeamCommandArgs.Add(("--detach=" + this.detach));
            }
            // If the noClientGen value was not default, then add it to the list of args.
            if ((this.noClientGen != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-client-gen=" + this.noClientGen));
            }
            // If the requireProcessId value was not default, then add it to the list of args.
            if ((this.requireProcessId != default(int)))
            {
                genBeamCommandArgs.Add(("--require-process-id=" + this.requireProcessId));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectRunWrapper ProjectRun(ProjectRunArgs runArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("run");
            genBeamCommandArgs.Add(runArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectRunWrapper genBeamCommandWrapper = new ProjectRunWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectRunWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectRunWrapper OnStreamRunProjectResultStream(System.Action<ReportDataPoint<BeamRunProjectResultStream>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ProjectRunWrapper OnBuildErrorsRunProjectBuildErrorStream(System.Action<ReportDataPoint<BeamRunProjectBuildErrorStream>> cb)
        {
            this.Command.On("buildErrors", cb);
            return this;
        }
        public virtual ProjectRunWrapper OnErrorRunFailErrorOutput(System.Action<ReportDataPoint<BeamRunFailErrorOutput>> cb)
        {
            this.Command.On("errorRunFailErrorOutput", cb);
            return this;
        }
    }
}
