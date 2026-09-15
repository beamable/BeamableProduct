
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesPruneYankedArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Remove the yanked references from manifest.beam.json (otherwise just report them)</summary>
        public bool remove;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the remove value was not default, then add it to the list of args.
            if ((this.remove != default(bool)))
            {
                genBeamCommandArgs.Add(("--remove=" + this.remove));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual BundlesPruneYankedWrapper BundlesPruneYanked(BundlesPruneYankedArgs pruneYankedArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("prune-yanked");
            genBeamCommandArgs.Add(pruneYankedArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesPruneYankedWrapper genBeamCommandWrapper = new BundlesPruneYankedWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesPruneYankedWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesPruneYankedWrapper OnStreamPruneYankedCommandOutput(System.Action<ReportDataPoint<BeamPruneYankedCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
