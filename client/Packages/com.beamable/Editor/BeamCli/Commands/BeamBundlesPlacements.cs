
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesPlacementsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>A portal page, e.g. players/list (repeatable)</summary>
        public string[] page;
        /// <summary>A site id that portal extensions fill, e.g. campaign-offer (repeatable)</summary>
        public string[] site;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the page value was not default, then add it to the list of args.
            if ((this.page != default(string[])))
            {
                for (int i = 0; (i < this.page.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--page=" + this.page[i]));
                }
            }
            // If the site value was not default, then add it to the list of args.
            if ((this.site != default(string[])))
            {
                for (int i = 0; (i < this.site.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--site=" + this.site[i]));
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
        public virtual BundlesPlacementsWrapper BundlesPlacements(BundlesPlacementsArgs placementsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("placements");
            genBeamCommandArgs.Add(placementsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesPlacementsWrapper genBeamCommandWrapper = new BundlesPlacementsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesPlacementsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesPlacementsWrapper OnStreamPlacementsCommandOutput(System.Action<ReportDataPoint<BeamPlacementsCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
