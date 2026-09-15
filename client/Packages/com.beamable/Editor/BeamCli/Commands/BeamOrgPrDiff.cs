
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OrgPrDiffArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The id of the pull request to inspect</summary>
        public string id;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the id value was not default, then add it to the list of args.
            if ((this.id != default(string)))
            {
                genBeamCommandArgs.Add(("--id=" + this.id));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OrgPrDiffWrapper OrgPrDiff(OrgPrDiffArgs diffArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("org");
            genBeamCommandArgs.Add("pr");
            genBeamCommandArgs.Add("diff");
            genBeamCommandArgs.Add(diffArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OrgPrDiffWrapper genBeamCommandWrapper = new OrgPrDiffWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OrgPrDiffWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual OrgPrDiffWrapper OnStreamPrDiffCommandOutput(System.Action<ReportDataPoint<BeamPrDiffCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
