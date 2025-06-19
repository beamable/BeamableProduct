
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentTagRmArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>List of tags for us to affect</summary>
        public string tag;
        /// <summary>Defines the semantics for the `filter` argument. When no filters are given, affects all existing content.
        ///ExactIds => Will only add the given tags to the ','-separated list of filters
        ///Regexes => Will add the given tags to any content whose Id is matched by any of the ','-separated list of filters (C# regex string)
        ///TypeHierarchy => Will add the given tags to any content of the ','-separated list of filters (content type strings with full hierarchy --- StartsWith comparison)
        ///Tags => Will add the given tags to any content that currently has any of the ','-separated list of filters (tags)</summary>
        public Beamable.Common.Content.ContentFilterType filterType;
        /// <summary>Accepts different strings to filter which content files will be affected. See the `filter-type` option</summary>
        public string filter;
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the tag value to the list of args.
            genBeamCommandArgs.Add(this.tag.ToString());
            // If the filterType value was not default, then add it to the list of args.
            if ((this.filterType != default(Beamable.Common.Content.ContentFilterType)))
            {
                genBeamCommandArgs.Add(("--filter-type=" + this.filterType));
            }
            // If the filter value was not default, then add it to the list of args.
            if ((this.filter != default(string)))
            {
                genBeamCommandArgs.Add(("--filter=" + this.filter));
            }
            // If the manifestIds value was not default, then add it to the list of args.
            if ((this.manifestIds != default(string[])))
            {
                for (int i = 0; (i < this.manifestIds.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--manifest-ids=" + this.manifestIds[i]));
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
        public virtual ContentTagRmWrapper ContentTagRm(ContentTagRmArgs rmArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("tag");
            genBeamCommandArgs.Add("rm");
            genBeamCommandArgs.Add(rmArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentTagRmWrapper genBeamCommandWrapper = new ContentTagRmWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentTagRmWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
