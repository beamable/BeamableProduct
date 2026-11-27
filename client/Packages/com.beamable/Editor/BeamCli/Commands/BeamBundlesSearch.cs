
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class BundlesSearchArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Text to match in bundle names</summary>
        public string query;
        /// <summary>Only bundles in this category</summary>
        public string category;
        /// <summary>The cursor a previous search returned, for its next page</summary>
        public string cursor;
        /// <summary>How many bundles per page</summary>
        public int limit;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the query value was not default, then add it to the list of args.
            if ((this.query != default(string)))
            {
                genBeamCommandArgs.Add(this.query.ToString());
            }
            // If the category value was not default, then add it to the list of args.
            if ((this.category != default(string)))
            {
                genBeamCommandArgs.Add(("--category=" + this.category));
            }
            // If the cursor value was not default, then add it to the list of args.
            if ((this.cursor != default(string)))
            {
                genBeamCommandArgs.Add(("--cursor=" + this.cursor));
            }
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
        public virtual BundlesSearchWrapper BundlesSearch(BundlesSearchArgs searchArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("bundles");
            genBeamCommandArgs.Add("search");
            genBeamCommandArgs.Add(searchArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            BundlesSearchWrapper genBeamCommandWrapper = new BundlesSearchWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class BundlesSearchWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual BundlesSearchWrapper OnStreamSearchMarketplaceCommandOutput(System.Action<ReportDataPoint<BeamSearchMarketplaceCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
