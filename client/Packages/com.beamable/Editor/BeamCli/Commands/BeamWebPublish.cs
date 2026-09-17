
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class WebPublishArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Absolute path to the BeamableProduct checkout holding web/ and beam-portal-toolkit/; defaults to searching upwards from the working directory</summary>
        public string productDir;
        /// <summary>The local npm registry to publish to</summary>
        public string registry;
        /// <summary>The local unpkg-style CDN whose file cache is flushed after publishing</summary>
        public string cdn;
        /// <summary>Rebuild just one package, either 'sdk' or 'toolkit'; both are still published, since their versions must match</summary>
        public string only;
        /// <summary>Publish as this version instead of the standard local-dev version (0.0.123)</summary>
        public string version;
        /// <summary>Publish whatever is already built instead of rebuilding first</summary>
        public bool skipBuild;
        /// <summary>Run 'pnpm install' before building even when node_modules already exists; use after the packages' dependencies changed</summary>
        public bool forceInstall;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the productDir value was not default, then add it to the list of args.
            if ((this.productDir != default(string)))
            {
                genBeamCommandArgs.Add(("--product-dir=" + this.productDir));
            }
            // If the registry value was not default, then add it to the list of args.
            if ((this.registry != default(string)))
            {
                genBeamCommandArgs.Add(("--registry=" + this.registry));
            }
            // If the cdn value was not default, then add it to the list of args.
            if ((this.cdn != default(string)))
            {
                genBeamCommandArgs.Add(("--cdn=" + this.cdn));
            }
            // If the only value was not default, then add it to the list of args.
            if ((this.only != default(string)))
            {
                genBeamCommandArgs.Add(("--only=" + this.only));
            }
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(string)))
            {
                genBeamCommandArgs.Add(("--version=" + this.version));
            }
            // If the skipBuild value was not default, then add it to the list of args.
            if ((this.skipBuild != default(bool)))
            {
                genBeamCommandArgs.Add(("--skip-build=" + this.skipBuild));
            }
            // If the forceInstall value was not default, then add it to the list of args.
            if ((this.forceInstall != default(bool)))
            {
                genBeamCommandArgs.Add(("--force-install=" + this.forceInstall));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual WebPublishWrapper WebPublish(WebPublishArgs publishArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("web");
            genBeamCommandArgs.Add("publish");
            genBeamCommandArgs.Add(publishArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            WebPublishWrapper genBeamCommandWrapper = new WebPublishWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class WebPublishWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual WebPublishWrapper OnStreamWebPublishCommandResults(System.Action<ReportDataPoint<BeamWebPublishCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
