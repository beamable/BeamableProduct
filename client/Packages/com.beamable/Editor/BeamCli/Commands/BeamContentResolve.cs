
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentResolveArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>Defines the semantics for the `filter` argument. When no filters are given, affects all existing content.
        ///ExactIds => Will only add the given tags to the ','-separated list of filters
        ///Regexes => Will add the given tags to any content whose Id is matched by any of the ','-separated list of filters (C# regex string)
        ///TypeHierarchy => Will add the given tags to any content of the ','-separated list of filters (content type strings with full hierarchy --- StartsWith comparison)
        ///Tags => Will add the given tags to any content that currently has any of the ','-separated list of filters (tags)</summary>
        public Beamable.Common.Content.ContentFilterType filterType;
        /// <summary>Accepts different strings to filter which content files will be affected. See the `filter-type` option</summary>
        public string filter;
        /// <summary>Whether to use the 'local' or 'realm' version of the conflicted content.
        ///This applies to ALL matching elements of the filter that are conflicted.
        ///Value must be "local" or "realm"</summary>
        public string use;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the manifestIds value was not default, then add it to the list of args.
            if ((this.manifestIds != default(string[])))
            {
                genBeamCommandArgs.Add((("--manifest-ids=\"" + this.manifestIds) 
                                + "\""));
            }
            // If the filterType value was not default, then add it to the list of args.
            if ((this.filterType != default(Beamable.Common.Content.ContentFilterType)))
            {
                genBeamCommandArgs.Add(("--filter-type=" + this.filterType));
            }
            // If the filter value was not default, then add it to the list of args.
            if ((this.filter != default(string)))
            {
                genBeamCommandArgs.Add((("--filter=\"" + this.filter) 
                                + "\""));
            }
            // If the use value was not default, then add it to the list of args.
            if ((this.use != default(string)))
            {
                genBeamCommandArgs.Add((("--use=\"" + this.use) 
                                + "\""));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentResolveWrapper ContentResolve(ContentResolveArgs resolveArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("resolve");
            genBeamCommandArgs.Add(resolveArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentResolveWrapper genBeamCommandWrapper = new ContentResolveWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentResolveWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentResolveWrapper OnStreamContentResolveConflictResult(System.Action<ReportDataPoint<BeamContentResolveConflictResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
