
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesReleasesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally namespaced as @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>Stop after this many releases, newest first. 0 (the default) walks the whole log</summary>
        public int limit;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // If the limit value was not default, then add it to the list of args.
            if ((this.limit != default(int)))
            {
                genBeamCommandArgs.Add(("--limit=" + this.limit));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesReleasesWrapper BundlesReleases(BundlesReleasesArgs releasesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("releases");
            genBeamCommandArgs.Add(releasesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesReleasesWrapper genBeamCommandWrapper = new BundlesReleasesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesReleasesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesReleasesWrapper OnStreamBundleReleasesCommandOutput(System.Action<ReportDataPoint<BeamBundleReleasesCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
