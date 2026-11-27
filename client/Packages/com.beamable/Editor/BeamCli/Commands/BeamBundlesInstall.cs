
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesInstallArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally namespaced as @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>The release to run. Defaults to latest on a new install, and to the installed one on a change</summary>
        public long version;
        /// <summary>Run the release this tag points at, instead of --version</summary>
        public string tag;
        /// <summary>The component groups to turn on, comma separated or repeated. Defaults to the ones on by default</summary>
        public string[] componentGroups;
        /// <summary>Turn every component group off, instead of --component-groups</summary>
        public bool noComponentGroups;
        /// <summary>Which realms get the realm components: all (every realm of the zone, including later ones) or none (zone components only). Defaults to all</summary>
        public string realms;
        /// <summary>A realm (its id) to get the realm components; repeat for each. Instead of --realms</summary>
        public string[] realm;
        /// <summary>Apply compatible releases automatically. Defaults to your org's setting on a new install</summary>
        public System.Nullable<bool> autoUpdate;
        /// <summary>An overridden component to keep as your own when this change takes it off a site, instead of archiving it</summary>
        public string[] detach;
        /// <summary>The zone to act on (its zone id). Defaults to the workspace's zone</summary>
        public string targetZone;
        /// <summary>Show the plan and stop, applying nothing</summary>
        public bool planOnly;
        /// <summary>The hash of a plan you reviewed (from --plan-only): apply only if the plan is still that one</summary>
        public string planHash;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // If the version value was not default, then add it to the list of args.
            if ((this.version != default(long)))
            {
                genBeamCommandArgs.Add(("--version=" + this.version));
            }
            // If the tag value was not default, then add it to the list of args.
            if ((this.tag != default(string)))
            {
                genBeamCommandArgs.Add(("--tag=" + this.tag));
            }
            // If the componentGroups value was not default, then add it to the list of args.
            if ((this.componentGroups != default(string[])))
            {
                for (int i = 0; (i < this.componentGroups.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--component-groups=" + this.componentGroups[i]));
                }
            }
            // If the noComponentGroups value was not default, then add it to the list of args.
            if ((this.noComponentGroups != default(bool)))
            {
                genBeamCommandArgs.Add(("--no-component-groups=" + this.noComponentGroups));
            }
            // If the realms value was not default, then add it to the list of args.
            if ((this.realms != default(string)))
            {
                genBeamCommandArgs.Add(("--realms=" + this.realms));
            }
            // If the realm value was not default, then add it to the list of args.
            if ((this.realm != default(string[])))
            {
                for (int i = 0; (i < this.realm.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--realm=" + this.realm[i]));
                }
            }
            // If the autoUpdate value was not default, then add it to the list of args.
            if ((this.autoUpdate != default(System.Nullable<bool>)))
            {
                genBeamCommandArgs.Add(("--auto-update=" + this.autoUpdate));
            }
            // If the detach value was not default, then add it to the list of args.
            if ((this.detach != default(string[])))
            {
                for (int i = 0; (i < this.detach.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--detach=" + this.detach[i]));
                }
            }
            // If the targetZone value was not default, then add it to the list of args.
            if ((this.targetZone != default(string)))
            {
                genBeamCommandArgs.Add(("--target-zone=" + this.targetZone));
            }
            // If the planOnly value was not default, then add it to the list of args.
            if ((this.planOnly != default(bool)))
            {
                genBeamCommandArgs.Add(("--plan-only=" + this.planOnly));
            }
            // If the planHash value was not default, then add it to the list of args.
            if ((this.planHash != default(string)))
            {
                genBeamCommandArgs.Add(("--plan-hash=" + this.planHash));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesInstallWrapper BundlesInstall(BundlesInstallArgs installArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("install");
            genBeamCommandArgs.Add(installArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesInstallWrapper genBeamCommandWrapper = new BundlesInstallWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesInstallWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesInstallWrapper OnStreamInstallBundleCommandOutput(System.Action<ReportDataPoint<BeamInstallBundleCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
