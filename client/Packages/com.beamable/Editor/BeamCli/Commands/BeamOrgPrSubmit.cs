
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OrgPrSubmitArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Snapshot this realm's current manifest as the proposal</summary>
        public string sourceRealm;
        /// <summary>An optional comment to attach to the pull request</summary>
        public string comment;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the sourceRealm value was not default, then add it to the list of args.
            if ((this.sourceRealm != default(string)))
            {
                genBeamCommandArgs.Add(("--source-realm=" + this.sourceRealm));
            }
            // If the comment value was not default, then add it to the list of args.
            if ((this.comment != default(string)))
            {
                genBeamCommandArgs.Add(("--comment=" + this.comment));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OrgPrSubmitWrapper OrgPrSubmit(OrgPrSubmitArgs submitArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("org");
            genBeamCommandArgs.Add("pr");
            genBeamCommandArgs.Add("submit");
            genBeamCommandArgs.Add(submitArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OrgPrSubmitWrapper genBeamCommandWrapper = new OrgPrSubmitWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OrgPrSubmitWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual OrgPrSubmitWrapper OnStreamPrSubmitCommandOutput(System.Action<ReportDataPoint<BeamPrSubmitCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
