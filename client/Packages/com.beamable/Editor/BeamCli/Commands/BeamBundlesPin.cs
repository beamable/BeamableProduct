
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesPinArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name</summary>
        public string bundleName;
        /// <summary>Resolve the checksum from this tag</summary>
        public string tag;
        /// <summary>Pin this exact checksum (sha256:<checksum>) instead of a tag</summary>
        public string checksum;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // If the tag value was not default, then add it to the list of args.
            if ((this.tag != default(string)))
            {
                genBeamCommandArgs.Add(("--tag=" + this.tag));
            }
            // If the checksum value was not default, then add it to the list of args.
            if ((this.checksum != default(string)))
            {
                genBeamCommandArgs.Add(("--checksum=" + this.checksum));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesPinWrapper BundlesPin(BundlesPinArgs pinArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("pin");
            genBeamCommandArgs.Add(pinArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesPinWrapper genBeamCommandWrapper = new BundlesPinWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesPinWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesPinWrapper OnStreamPinBundleCommandOutput(System.Action<ReportDataPoint<BeamPinBundleCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
