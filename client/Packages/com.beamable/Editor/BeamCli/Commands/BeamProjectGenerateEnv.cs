
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectGenerateEnvArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Which service to generate the .env file for</summary>
        public Beamable.Common.Semantics.ServiceName service;
        /// <summary>Where to output the .env file</summary>
        public string output;
        /// <summary>If true, the generated .env file will include the local machine name as prefix</summary>
        public bool includePrefix;
        /// <summary>If true, do not ask for otel auth credentials to be put in the env</summary>
        public bool excludeOtelCreds;
        /// <summary>How many virtual websocket connections the server will open</summary>
        public int instanceCount;
        /// <summary>When enabled, automatically deploy dependencies that aren't running</summary>
        public bool autoDeploy;
        /// <summary>When enabled, includes the legacy SECRET realm secret environment variable</summary>
        public bool includeSecret;
        /// <summary>When enabled, prevents any connection strings from being added to the environment</summary>
        public bool useRemoteDeps;
        /// <summary>When enabled, automatically stop all other local instances of this service</summary>
        public int removeAllExceptPid;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the service value to the list of args.
            genBeamCommandArgs.Add(this.service.ToString());
            // Add the output value to the list of args.
            genBeamCommandArgs.Add(this.output.ToString());
            // If the includePrefix value was not default, then add it to the list of args.
            if ((this.includePrefix != default(bool)))
            {
                genBeamCommandArgs.Add(("--include-prefix=" + this.includePrefix));
            }
            // If the excludeOtelCreds value was not default, then add it to the list of args.
            if ((this.excludeOtelCreds != default(bool)))
            {
                genBeamCommandArgs.Add(("--exclude-otel-creds=" + this.excludeOtelCreds));
            }
            // If the instanceCount value was not default, then add it to the list of args.
            if ((this.instanceCount != default(int)))
            {
                genBeamCommandArgs.Add(("--instance-count=" + this.instanceCount));
            }
            // If the autoDeploy value was not default, then add it to the list of args.
            if ((this.autoDeploy != default(bool)))
            {
                genBeamCommandArgs.Add(("--auto-deploy=" + this.autoDeploy));
            }
            // If the includeSecret value was not default, then add it to the list of args.
            if ((this.includeSecret != default(bool)))
            {
                genBeamCommandArgs.Add(("--include-secret=" + this.includeSecret));
            }
            // If the useRemoteDeps value was not default, then add it to the list of args.
            if ((this.useRemoteDeps != default(bool)))
            {
                genBeamCommandArgs.Add(("--use-remote-deps=" + this.useRemoteDeps));
            }
            // If the removeAllExceptPid value was not default, then add it to the list of args.
            if ((this.removeAllExceptPid != default(int)))
            {
                genBeamCommandArgs.Add(("--remove-all-except-pid=" + this.removeAllExceptPid));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectGenerateEnvWrapper ProjectGenerateEnv(ProjectGenerateEnvArgs generateEnvArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("generate-env");
            genBeamCommandArgs.Add(generateEnvArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectGenerateEnvWrapper genBeamCommandWrapper = new ProjectGenerateEnvWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectGenerateEnvWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectGenerateEnvWrapper OnStreamGenerateEnvFileOutput(System.Action<ReportDataPoint<Beamable.Common.BeamCli.Contracts.GenerateEnvFileOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
