
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BeamArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>[DEPRECATED] Run as much of the command as possible without making any network calls</summary>
        public bool dryrun;
        /// <summary>CID (CustomerId) to use (found in Portal->Account); defaults to whatever is in '.beamable/config.beam.json'</summary>
        public string cid;
        /// <summary>If passed, sets the engine integration that is calling for the command</summary>
        public string engine;
        /// <summary>The version of the Beamable's SDK running in that Engine</summary>
        public string engineSdkVersion;
        /// <summary>The version of the engine that is calling the CLI</summary>
        public string engineVersion;
        /// <summary>PID (Realm ID) to use (found in Portal -> Games -> Any Realm's details); defaults to whatever is in '.beamable/config.beam.json'</summary>
        public string pid;
        /// <summary>When true, skip input waiting and use default arguments (or error if no defaults are possible)</summary>
        public bool quiet;
        /// <summary>This option defines the target Beamable environment. Needed for private cloud customers to target their exclusive Beamable environment. Ignorable by everyone else. Stored in '.beamable/config.beam.json'</summary>
        public string host;
        /// <summary>The access token to use for the requests. It overwrites the logged in user stored in auth.beam.json for THIS INVOCATION ONLY</summary>
        public string accessToken;
        /// <summary>A Refresh Token to use for the requests. It overwrites the logged in user stored in auth.beam.json for THIS INVOCATION ONLY</summary>
        public string refreshToken;
        /// <summary>Extra logs gets printed out</summary>
        public string log;
        /// <summary>If there is a local dotnet tool installation (with a ./config/dotnet-tools.json file) for the beam tool, then any global invocation of the beam tool will automatically redirect and call the local version. However, there will be a performance penalty due to the extra process invocation. This option flag will cause an error to occur instead of automatically redirecting the execution to a new process invocation. </summary>
        public bool noRedirect;
        /// <summary>A set of beam ids that should be excluded from the local source code scans. When a beam id is ignored, it cannot be deployed or understood by the CLI. The final set of ignored beam ids is the summation of this option AND any .beamignore files found in the .beamable folder</summary>
        public string[] ignoreBeamIds;
        /// <summary>By default, any local CLI invocation that should trigger a Federation of any type will prefer locally running Microservices. However, if you need the CLI to use the remotely running Microservices, use this option to ignore locally running services. </summary>
        public bool preferRemoteFederation;
        /// <summary>Show help for all commands</summary>
        public bool helpAll;
        /// <summary>By default, logs will automatically mask tokens. However, when this option is enabled, tokens will be visible in their full text. This is a security risk.</summary>
        public bool unmaskLogs;
        /// <summary>By default, logs are automatically written to a temp file so that they can be used in an error case. However, when this option is enabled, logs are not written. Also, if the BEAM_CLI_NO_FILE_LOG environment variable is set, no log file will be written. </summary>
        public bool noLogFile;
        /// <summary>a custom location for docker. By default, the CLI will attempt to resolve docker through its usual install locations. You can also use the BEAM_DOCKER_EXE environment variable to specify. 
        ///Currently, a docker path has been automatically identified.</summary>
        public string dockerCliPath;
        /// <summary>Out all log messages as data payloads in addition to however they are logged</summary>
        public bool emitLogStreams;
        /// <summary>additional file paths to be included when building a local project manifest. </summary>
        public string[] addProjectPath;
        /// <summary>Output raw JSON to standard out. This happens by default when the command is being piped</summary>
        public bool raw;
        /// <summary>Output syntax highlighted box text. This happens by default when the command is not piped</summary>
        public bool pretty;
        /// <summary>skips the check for commands that require beam config directories.</summary>
        public bool skipStandaloneValidation;
        /// <summary>a custom location for dotnet</summary>
        public string dotnetPath;
        /// <summary>Show version information</summary>
        public bool version;
        /// <summary>Show help and usage information</summary>
        public bool help;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the dryrun value was not default, then add it to the list of args.
            if ((this.dryrun != default(bool)))
            {
                genBeamCommandArgs.Add(("--dryrun=" + this.dryrun));
            }
            // If the cid value was not default, then add it to the list of args.
            if ((this.cid != default(string)))
            {
                genBeamCommandArgs.Add(("--cid=" + this.cid));
            }
            // If the engine value was not default, then add it to the list of args.
            if ((this.engine != default(string)))
            {
                genBeamCommandArgs.Add(("--engine=" + this.engine));
            }
            // If the engineSdkVersion value was not default, then add it to the list of args.
            if ((this.engineSdkVersion != default(string)))
            {
                genBeamCommandArgs.Add(("--engine-sdk-version=" + this.engineSdkVersion));
            }
            // If the engineVersion value was not default, then add it to the list of args.
            if ((this.engineVersion != default(string)))
            {
                genBeamCommandArgs.Add(("--engine-version=" + this.engineVersion));
            }
            // If the pid value was not default, then add it to the list of args.
            if ((this.pid != default(string)))
            {
                genBeamCommandArgs.Add(("--pid=" + this.pid));
            }
            // If the quiet value was not default, then add it to the list of args.
            if ((this.quiet != default(bool)))
            {
                genBeamCommandArgs.Add(("--quiet=" + this.quiet));
            }
            // If the host value was not default, then add it to the list of args.
            if ((this.host != default(string)))
            {
                genBeamCommandArgs.Add(("--host=" + this.host));
            }
            // If the accessToken value was not default, then add it to the list of args.
            if ((this.accessToken != default(string)))
            {
                genBeamCommandArgs.Add(("--access-token=" + this.accessToken));
            }
            // If the refreshToken value was not default, then add it to the list of args.
            if ((this.refreshToken != default(string)))
            {
                genBeamCommandArgs.Add(("--refresh-token=" + this.refreshToken));
            }
            // If the log value was not default, then add it to the list of args.
            if ((this.log != default(string)))
            {
                genBeamCommandArgs.Add(("--log=" + this.log));
            }
            // If the noRedirect value was not default, then add it to the list of args.
            if ((this.noRedirect != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-redirect=" + this.noRedirect));
            }
            // If the ignoreBeamIds value was not default, then add it to the list of args.
            if ((this.ignoreBeamIds != default(string[])))
            {
                for (int i = 0; (i < this.ignoreBeamIds.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--ignore-beam-ids=" + this.ignoreBeamIds[i]));
                }
            }
            // If the preferRemoteFederation value was not default, then add it to the list of args.
            if ((this.preferRemoteFederation != default(bool)))
            {
                genBeamCommandArgs.Add(("--prefer-remote-federation=" + this.preferRemoteFederation));
            }
            // If the helpAll value was not default, then add it to the list of args.
            if ((this.helpAll != default(bool)))
            {
                genBeamCommandArgs.Add(("--help-all=" + this.helpAll));
            }
            // If the unmaskLogs value was not default, then add it to the list of args.
            if ((this.unmaskLogs != default(bool)))
            {
                genBeamCommandArgs.Add(("--unmask-logs=" + this.unmaskLogs));
            }
            // If the noLogFile value was not default, then add it to the list of args.
            if ((this.noLogFile != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-log-file=" + this.noLogFile));
            }
            // If the dockerCliPath value was not default, then add it to the list of args.
            if ((this.dockerCliPath != default(string)))
            {
                genBeamCommandArgs.Add(("--docker-cli-path=" + this.dockerCliPath));
            }
            // If the emitLogStreams value was not default, then add it to the list of args.
            if ((this.emitLogStreams != default(bool)))
            {
                genBeamCommandArgs.Add(("--emit-log-streams=" + this.emitLogStreams));
            }
            // If the addProjectPath value was not default, then add it to the list of args.
            if ((this.addProjectPath != default(string[])))
            {
                for (int i = 0; (i < this.addProjectPath.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--add-project-path=" + this.addProjectPath[i]));
                }
            }
            // If the raw value was not default, then add it to the list of args.
            if ((this.raw != default(bool)))
            {
                genBeamCommandArgs.Add(("--raw=" + this.raw));
            }
            // If the pretty value was not default, then add it to the list of args.
            if ((this.pretty != default(bool)))
            {
                genBeamCommandArgs.Add(("--pretty=" + this.pretty));
            }
            // If the skipStandaloneValidation value was not default, then add it to the list of args.
            if ((this.skipStandaloneValidation != default(bool)))
            {
                genBeamCommandArgs.Add(("--skip-standalone-validation=" + this.skipStandaloneValidation));
            }
            // If the dotnetPath value was not default, then add it to the list of args.
            if ((this.dotnetPath != default(string)))
            {
                genBeamCommandArgs.Add(("--dotnet-path=" + this.dotnetPath));
            }
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(bool)))
            {
                genBeamCommandArgs.Add(("--version=" + this.version));
            }
            // If the help value was not default, then add it to the list of args.
            if ((this.help != default(bool)))
            {
                genBeamCommandArgs.Add(("--help=" + this.help));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BeamWrapper Beam()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BeamWrapper genBeamCommandWrapper = new BeamWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BeamWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
