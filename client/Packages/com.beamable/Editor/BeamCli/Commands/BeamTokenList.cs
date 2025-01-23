
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TokenListArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The player or account id to fetch tokens for. When blank, the current player id will be used</summary>
        public long id;
        /// <summary>The offset</summary>
        public int offset;
        /// <summary>The max size of the response</summary>
        public int length;
        /// <summary>A cid</summary>
        public long cid;
        /// <summary>A pid</summary>
        public string pid;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the id value was not default, then add it to the list of args.
            if ((this.id != default(long)))
            {
                genBeamCommandArgs.Add(("--id=" + this.id));
            }
            // If the offset value was not default, then add it to the list of args.
            if ((this.offset != default(int)))
            {
                genBeamCommandArgs.Add(("--offset=" + this.offset));
            }
            // If the length value was not default, then add it to the list of args.
            if ((this.length != default(int)))
            {
                genBeamCommandArgs.Add(("--length=" + this.length));
            }
            // If the cid value was not default, then add it to the list of args.
            if ((this.cid != default(long)))
            {
                genBeamCommandArgs.Add(("--cid=" + this.cid));
            }
            // If the pid value was not default, then add it to the list of args.
            if ((this.pid != default(string)))
            {
                genBeamCommandArgs.Add((("--pid=\"" + this.pid) 
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
        public virtual TokenListWrapper TokenList(TokenListArgs listArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("token");
            genBeamCommandArgs.Add("list");
            genBeamCommandArgs.Add(listArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TokenListWrapper genBeamCommandWrapper = new TokenListWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TokenListWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TokenListWrapper OnStreamGetTokenListCommandOutput(System.Action<ReportDataPoint<BeamGetTokenListCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
