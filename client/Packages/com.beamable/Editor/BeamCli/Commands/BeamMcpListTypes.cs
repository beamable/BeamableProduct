
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class McpListTypesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Which section to return: 'content', 'federation', or 'utility'. Leave empty to return all sections.</summary>
        public string section;
        /// <summary>For section='utility': narrow results to types whose namespace or name contains this string (case-insensitive).</summary>
        public string filter;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the section value was not default, then add it to the list of args.
            if ((this.section != default(string)))
            {
                genBeamCommandArgs.Add(("--section=" + this.section));
            }
            // If the filter value was not default, then add it to the list of args.
            if ((this.filter != default(string)))
            {
                genBeamCommandArgs.Add(("--filter=" + this.filter));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual McpListTypesWrapper McpListTypes(McpListTypesArgs listTypesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("mcp");
            genBeamCommandArgs.Add("list-types");
            genBeamCommandArgs.Add(listTypesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            McpListTypesWrapper genBeamCommandWrapper = new McpListTypesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class McpListTypesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual McpListTypesWrapper OnStreamBeamableTypesSchema(System.Action<ReportDataPoint<BeamBeamableTypesSchema>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
