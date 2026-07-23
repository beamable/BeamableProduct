
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesGetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, optionally @<tag> or @sha256:<checksum></summary>
        public string bundleRef;
        /// <summary>Also pin the fetched checksum into the local manifest.beam.json references</summary>
        public bool pin;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleRef value to the list of args.
            genBeamCommandArgs.Add(this.bundleRef.ToString());
            // If the pin value was not default, then add it to the list of args.
            if ((this.pin != default(bool)))
            {
                genBeamCommandArgs.Add(("--pin=" + this.pin));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesGetWrapper BundlesGet(BundlesGetArgs getArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("get");
            genBeamCommandArgs.Add(getArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesGetWrapper genBeamCommandWrapper = new BundlesGetWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesGetWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesGetWrapper OnStreamGetBundleCommandOutput(System.Action<ReportDataPoint<BeamGetBundleCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
