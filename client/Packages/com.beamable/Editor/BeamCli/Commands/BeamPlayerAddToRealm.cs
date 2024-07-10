
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class PlayerAddToRealmArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>the playerId (gamerTag)</summary>
        public long playerId;
        /// <summary>the token for a player. Cannot be specified when --player-id is set.</summary>
        public string token;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the playerId value was not default, then add it to the list of args.
            if ((this.playerId != default(long)))
            {
                genBeamCommandArgs.Add(("--player-id=" + this.playerId));
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
        public virtual PlayerAddToRealmWrapper PlayerAddToRealm(PlayerArgs playerArgs, PlayerAddToRealmArgs addToRealmArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("player");
            genBeamCommandArgs.Add(playerArgs.Serialize());
            genBeamCommandArgs.Add("add-to-realm");
            genBeamCommandArgs.Add(addToRealmArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            PlayerAddToRealmWrapper genBeamCommandWrapper = new PlayerAddToRealmWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public class PlayerAddToRealmWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual PlayerAddToRealmWrapper OnStreamAddPlayerToRealmCommandOutput(System.Action<ReportDataPoint<BeamAddPlayerToRealmCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
