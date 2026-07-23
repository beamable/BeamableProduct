
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesYankArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle checksum reference, e.g. <bundle-name>@sha256:<checksum></summary>
        public string bundleRef;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleRef value to the list of args.
            genBeamCommandArgs.Add(this.bundleRef.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesYankWrapper BundlesYank(BundlesYankArgs yankArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("yank");
            genBeamCommandArgs.Add(yankArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesYankWrapper genBeamCommandWrapper = new BundlesYankWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesYankWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesYankWrapper OnStreamYankBundleCommandOutput(System.Action<ReportDataPoint<BeamYankBundleCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
