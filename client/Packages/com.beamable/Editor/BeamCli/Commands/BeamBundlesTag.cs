
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesTagArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle checksum reference, e.g. <bundle-name>@sha256:<checksum></summary>
        public string bundleRef;
        /// <summary>The tag to advance, e.g. stable</summary>
        public string tag;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleRef value to the list of args.
            genBeamCommandArgs.Add(this.bundleRef.ToString());
            // Add the tag value to the list of args.
            genBeamCommandArgs.Add(this.tag.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesTagWrapper BundlesTag(BundlesTagArgs tagArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("tag");
            genBeamCommandArgs.Add(tagArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesTagWrapper genBeamCommandWrapper = new BundlesTagWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesTagWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesTagWrapper OnStreamPromoteBundleTagCommandOutput(System.Action<ReportDataPoint<BeamPromoteBundleTagCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
