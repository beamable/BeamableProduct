
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesDependenciesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally namespaced as @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>The release to check. Defaults to the installed one, else latest</summary>
        public long version;
        /// <summary>The zone to act on (its zone id). Defaults to the workspace's zone</summary>
        public string targetZone;
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
            // If the targetZone value was not default, then add it to the list of args.
            if ((this.targetZone != default(string)))
            {
                genBeamCommandArgs.Add(("--target-zone=" + this.targetZone));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesDependenciesWrapper BundlesDependencies(BundlesDependenciesArgs dependenciesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("dependencies");
            genBeamCommandArgs.Add(dependenciesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesDependenciesWrapper genBeamCommandWrapper = new BundlesDependenciesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesDependenciesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesDependenciesWrapper OnStreamInstallationDependenciesCommandOutput(System.Action<ReportDataPoint<BeamInstallationDependenciesCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
