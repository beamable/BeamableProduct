
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesUninstallArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally namespaced as @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>An overridden component to keep as your own instead of archiving it</summary>
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
        public virtual BundlesUninstallWrapper BundlesUninstall(BundlesUninstallArgs uninstallArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("uninstall");
            genBeamCommandArgs.Add(uninstallArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesUninstallWrapper genBeamCommandWrapper = new BundlesUninstallWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesUninstallWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesUninstallWrapper OnStreamInstallBundleCommandOutput(System.Action<ReportDataPoint<BeamInstallBundleCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
