
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ServicesBuildArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The list of services to include, defaults to all local services (separated by whitespace). To use NO services, use the --exact-ids flag</summary>
        public string[] ids;
        /// <summary>By default, a blank --ids option maps to ALL available ids. When the --exact-ids flag is given, a blank --ids option maps to NO ids</summary>
        public bool exactIds;
        /// <summary>A set of BeamServiceGroup tags that will exclude the associated services. Exclusion takes precedence over inclusion</summary>
        public string[] withoutGroup;
        /// <summary>A set of BeamServiceGroup tags that will include the associated services</summary>
        public string[] withGroup;
        /// <summary>When true, build an image for the Beamable Cloud architecture, amd64</summary>
        public bool forceCpuArch;
        /// <summary>When true, force the docker build to pull all base images</summary>
        public bool pull;
        /// <summary>When true, force the docker build to ignore all caches</summary>
        public bool noCache;
        /// <summary>Provider custom tags for the resulting docker images</summary>
        public string[] tags;
        /// <summary>When true, all build images will run in parallel</summary>
        public bool simultaneous;
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
            // If the forceCpuArch value was not default, then add it to the list of args.
            if ((this.forceCpuArch != default(bool)))
            {
                genBeamCommandArgs.Add(("--force-cpu-arch=" + this.forceCpuArch));
            }
            // If the pull value was not default, then add it to the list of args.
            if ((this.pull != default(bool)))
            {
                genBeamCommandArgs.Add(("--pull=" + this.pull));
            }
            // If the noCache value was not default, then add it to the list of args.
            if ((this.noCache != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-cache=" + this.noCache));
            }
            // If the tags value was not default, then add it to the list of args.
            if ((this.tags != default(string[])))
            {
                for (int i = 0; (i < this.tags.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--tags=" + this.tags[i]));
                }
            }
            // If the simultaneous value was not default, then add it to the list of args.
            if ((this.simultaneous != default(bool)))
            {
                genBeamCommandArgs.Add(("--simultaneous=" + this.simultaneous));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ServicesBuildWrapper ServicesBuild(ServicesBuildArgs buildArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("services");
            genBeamCommandArgs.Add("build");
            genBeamCommandArgs.Add(buildArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ServicesBuildWrapper genBeamCommandWrapper = new ServicesBuildWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ServicesBuildWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ServicesBuildWrapper OnStreamServicesBuildCommandOutput(System.Action<ReportDataPoint<BeamServicesBuildCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ServicesBuildWrapper OnProgressServicesBuiltProgress(System.Action<ReportDataPoint<BeamServicesBuiltProgress>> cb)
        {
            this.Command.On("progress", cb);
            return this;
        }
    }
}
