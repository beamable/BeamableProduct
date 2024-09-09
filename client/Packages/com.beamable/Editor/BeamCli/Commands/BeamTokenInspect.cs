
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class TokenInspectArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>This command normally only works for an access token. However, if this option is enabled and a refresh token is given, then it will be automatically converted to the access token and this command is rerun</summary>
        public bool resolve;
        /// <summary>The token that you want to get information for. This must be an access token. By default, the current access token of the .beamable context is used</summary>
        public string token;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the resolve value was not default, then add it to the list of args.
            if ((this.resolve != default(bool)))
            {
                genBeamCommandArgs.Add(("--resolve=" + this.resolve));
            }
            // If the token value was not default, then add it to the list of args.
            if ((this.token != default(string)))
            {
                genBeamCommandArgs.Add((("--token=\"" + this.token) 
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
        public virtual TokenInspectWrapper TokenInspect(TokenInspectArgs inspectArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("token");
            genBeamCommandArgs.Add("inspect");
            genBeamCommandArgs.Add(inspectArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            TokenInspectWrapper genBeamCommandWrapper = new TokenInspectWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class TokenInspectWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual TokenInspectWrapper OnStreamGetTokenDetailsCommandOutput(System.Action<ReportDataPoint<BeamGetTokenDetailsCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual TokenInspectWrapper OnErrorInvalidTokenErrorOutput(System.Action<ReportDataPoint<BeamInvalidTokenErrorOutput>> cb)
        {
            this.Command.On("errorInvalidTokenErrorOutput", cb);
            return this;
        }
    }
}
