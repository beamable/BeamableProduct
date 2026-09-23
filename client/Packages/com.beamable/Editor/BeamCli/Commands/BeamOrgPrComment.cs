
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OrgPrCommentArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The id of the pull request to comment on</summary>
        public string id;
        /// <summary>The comment to append to the discussion thread</summary>
        public string message;
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
            // If the message value was not default, then add it to the list of args.
            if ((this.message != default(string)))
            {
                genBeamCommandArgs.Add(("--message=" + this.message));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OrgPrCommentWrapper OrgPrComment(OrgPrCommentArgs commentArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("org");
            genBeamCommandArgs.Add("pr");
            genBeamCommandArgs.Add("comment");
            genBeamCommandArgs.Add(commentArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OrgPrCommentWrapper genBeamCommandWrapper = new OrgPrCommentWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OrgPrCommentWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual OrgPrCommentWrapper OnStreamPrCommentCommandOutput(System.Action<ReportDataPoint<BeamPrCommentCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
