
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class OrgGamesArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Includes archived realms in the list of realms returned</summary>
        public bool includeArchived;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the includeArchived value was not default, then add it to the list of args.
            if ((this.includeArchived != default(bool)))
            {
                genBeamCommandArgs.Add(("--include-archived=" + this.includeArchived));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual OrgGamesWrapper OrgGames(OrgGamesArgs gamesArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("org");
            genBeamCommandArgs.Add("games");
            genBeamCommandArgs.Add(gamesArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            OrgGamesWrapper genBeamCommandWrapper = new OrgGamesWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class OrgGamesWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual OrgGamesWrapper OnStreamGameListCommandResults(System.Action<ReportDataPoint<BeamGameListCommandResults>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
