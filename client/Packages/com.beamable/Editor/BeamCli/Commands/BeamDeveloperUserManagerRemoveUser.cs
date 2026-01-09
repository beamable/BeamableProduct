
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeveloperUserManagerRemoveUserArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The gamer tag of the player that you would like to remove, it will not remove from the portal</summary>
        public string[] gamerTag;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the gamerTag value was not default, then add it to the list of args.
            if ((this.gamerTag != default(string[])))
            {
                for (int i = 0; (i < this.gamerTag.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--gamer-tag=" + this.gamerTag[i]));
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
        public virtual DeveloperUserManagerRemoveUserWrapper DeveloperUserManagerRemoveUser(DeveloperUserManagerRemoveUserArgs removeUserArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("developer-user-manager");
            genBeamCommandArgs.Add("remove-user");
            genBeamCommandArgs.Add(removeUserArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeveloperUserManagerRemoveUserWrapper genBeamCommandWrapper = new DeveloperUserManagerRemoveUserWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeveloperUserManagerRemoveUserWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeveloperUserManagerRemoveUserWrapper OnStreamDeveloperUserResult(System.Action<ReportDataPoint<BeamDeveloperUserResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
