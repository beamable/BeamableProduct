
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesInfoArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name, @<namespace>/<bundle-name></summary>
        public string bundleName;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesInfoWrapper BundlesInfo(BundlesInfoArgs infoArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("info");
            genBeamCommandArgs.Add(infoArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesInfoWrapper genBeamCommandWrapper = new BundlesInfoWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesInfoWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesInfoWrapper OnStreamBundleInfoCommandOutput(System.Action<ReportDataPoint<BeamBundleInfoCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
