
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesNewArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The bundle name; the config file will be named <bundle-name>.beam.bundle.json</summary>
        public string bundleName;
        /// <summary>A beamoId to include in the bundle (repeatable)</summary>
        public string[] component;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the bundleName value to the list of args.
            genBeamCommandArgs.Add(this.bundleName.ToString());
            // If the component value was not default, then add it to the list of args.
            if ((this.component != default(string[])))
            {
                for (int i = 0; (i < this.component.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--component=" + this.component[i]));
                }
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesNewWrapper BundlesNew(BundlesNewArgs newArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("new");
            genBeamCommandArgs.Add(newArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesNewWrapper genBeamCommandWrapper = new BundlesNewWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesNewWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesNewWrapper OnStreamNewBundleCommandOutput(System.Action<ReportDataPoint<BeamNewBundleCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
