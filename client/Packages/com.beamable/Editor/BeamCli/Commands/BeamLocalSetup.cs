
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class LocalSetupArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Directory the pinned dependencies are installed into and reused from (default: ~/.beamable-toolchain, or $BEAM_TOOLCHAIN_DIR). Point several workspaces at one directory to share a single install</summary>
        public string toolchainDir;
        /// <summary>Run only these steps (comma/space separated): jdk8, maven, dotnet, node, pnpm, scala-config, portal-config, aws</summary>
        public string only;
        /// <summary>Skip these steps (same ids as --only)</summary>
        public string skip;
        /// <summary>Re-download and re-install even when a dependency is already present, and overwrite generated config files</summary>
        public bool force;
        /// <summary>Adopt an already-installed dependency when its version matches the pin, instead of downloading a private copy</summary>
        public bool preferSystem;
        /// <summary>Never hit the network: install only from archives already in the toolchain's download cache</summary>
        public bool offline;
        /// <summary>Resolve and report what would be installed, downloading and writing nothing</summary>
        public bool dryRun;
        /// <summary>Token used to read the BeamableBackend `local` environment variables that the generated config files are rendered from (default: $GITHUB_TOKEN, else `gh auth token`)</summary>
        public string githubToken;
        /// <summary>Repository whose `local` environment holds the config values</summary>
        public string githubRepo;
        /// <summary>Region the AWS preflight checks the secret, buckets and queue in</summary>
        public string awsRegion;
        /// <summary>Path to the local-stack manifest to update (defaults to .beamable/local-stack.json)</summary>
        public string config;
        /// <summary>Absolute path to the BeamableBackend (Scala) repo; only needed when the manifest does not record it yet</summary>
        public string scalaDir;
        /// <summary>Absolute path to the BeamableAPI (C# gateway) repo; only needed when the manifest does not record it yet</summary>
        public string apiDir;
        /// <summary>Absolute path to the portal repo; only needed when the manifest does not record it yet</summary>
        public string portalDir;
        /// <summary>Print the AWS setup guide at the end of the run (it is printed automatically whenever an AWS check fails)</summary>
        public bool awsGuide;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the toolchainDir value was not default, then add it to the list of args.
            if ((this.toolchainDir != default(string)))
            {
                genBeamCommandArgs.Add(("--toolchain-dir=" + this.toolchainDir));
            }
            // If the only value was not default, then add it to the list of args.
            if ((this.only != default(string)))
            {
                genBeamCommandArgs.Add(("--only=" + this.only));
            }
            // If the skip value was not default, then add it to the list of args.
            if ((this.skip != default(string)))
            {
                genBeamCommandArgs.Add(("--skip=" + this.skip));
            }
            // If the force value was not default, then add it to the list of args.
            if ((this.force != default(bool)))
            {
                genBeamCommandArgs.Add(("--force=" + this.force));
            }
            // If the preferSystem value was not default, then add it to the list of args.
            if ((this.preferSystem != default(bool)))
            {
                genBeamCommandArgs.Add(("--prefer-system=" + this.preferSystem));
            }
            // If the offline value was not default, then add it to the list of args.
            if ((this.offline != default(bool)))
            {
                genBeamCommandArgs.Add(("--offline=" + this.offline));
            }
            // If the dryRun value was not default, then add it to the list of args.
            if ((this.dryRun != default(bool)))
            {
                genBeamCommandArgs.Add(("--dry-run=" + this.dryRun));
            }
            // If the githubToken value was not default, then add it to the list of args.
            if ((this.githubToken != default(string)))
            {
                genBeamCommandArgs.Add(("--github-token=" + this.githubToken));
            }
            // If the githubRepo value was not default, then add it to the list of args.
            if ((this.githubRepo != default(string)))
            {
                genBeamCommandArgs.Add(("--github-repo=" + this.githubRepo));
            }
            // If the awsRegion value was not default, then add it to the list of args.
            if ((this.awsRegion != default(string)))
            {
                genBeamCommandArgs.Add(("--aws-region=" + this.awsRegion));
            }
            // If the config value was not default, then add it to the list of args.
            if ((this.config != default(string)))
            {
                genBeamCommandArgs.Add(("--config=" + this.config));
            }
            // If the scalaDir value was not default, then add it to the list of args.
            if ((this.scalaDir != default(string)))
            {
                genBeamCommandArgs.Add(("--scala-dir=" + this.scalaDir));
            }
            // If the apiDir value was not default, then add it to the list of args.
            if ((this.apiDir != default(string)))
            {
                genBeamCommandArgs.Add(("--api-dir=" + this.apiDir));
            }
            // If the portalDir value was not default, then add it to the list of args.
            if ((this.portalDir != default(string)))
            {
                genBeamCommandArgs.Add(("--portal-dir=" + this.portalDir));
            }
            // If the awsGuide value was not default, then add it to the list of args.
            if ((this.awsGuide != default(bool)))
            {
                genBeamCommandArgs.Add(("--aws-guide=" + this.awsGuide));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual LocalSetupWrapper LocalSetup(LocalSetupArgs setupArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("local");
            genBeamCommandArgs.Add("setup");
            genBeamCommandArgs.Add(setupArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            LocalSetupWrapper genBeamCommandWrapper = new LocalSetupWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class LocalSetupWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual LocalSetupWrapper OnStreamLocalStackSetupCommandResult(System.Action<ReportDataPoint<BeamLocalStackSetupCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
