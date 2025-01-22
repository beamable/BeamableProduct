
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class TokenNewGuestArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The keys for initProperties. The count must match the count of the --value options</summary>
        public string[] key;
        /// <summary>The values for the initProperties. The count must match the count of the --key options</summary>
        public string[] value;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the key value was not default, then add it to the list of args.
            if ((this.key != default(string[])))
            {
                for (int i = 0; (i < this.key.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--key=" + this.key[i]));
                }
            }
            // If the value value was not default, then add it to the list of args.
            if ((this.value != default(string[])))
            {
                for (int i = 0; (i < this.value.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--value=" + this.value[i]));
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
        public virtual TokenNewGuestWrapper TokenNewGuest(TokenNewGuestArgs newGuestArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("token");
            genBeamCommandArgs.Add("new-guest");
            genBeamCommandArgs.Add(newGuestArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TokenNewGuestWrapper genBeamCommandWrapper = new TokenNewGuestWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class TokenNewGuestWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TokenNewGuestWrapper OnStreamGetTokenForGuestCommandOutput(System.Action<ReportDataPoint<BeamGetTokenForGuestCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
