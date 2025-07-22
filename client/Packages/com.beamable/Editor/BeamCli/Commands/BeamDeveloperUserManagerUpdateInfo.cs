
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeveloperUserManagerUpdateInfoArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>A alias (Name) of the user, which is not the same name as in the portal</summary>
        public string alias;
        /// <summary>The gamer tag of the user to be updated</summary>
        public string gamerTag;
        /// <summary>A new description for this user</summary>
        public string description;
        /// <summary>The tags to set in the local user data</summary>
        public string[] tags;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the alias value was not default, then add it to the list of args.
            if ((this.alias != default(string)))
            {
                genBeamCommandArgs.Add(("--alias=" + this.alias));
            }
            // If the gamerTag value was not default, then add it to the list of args.
            if ((this.gamerTag != default(string)))
            {
                genBeamCommandArgs.Add(("--gamer-tag=" + this.gamerTag));
            }
            // If the description value was not default, then add it to the list of args.
            if ((this.description != default(string)))
            {
                genBeamCommandArgs.Add(("--description=" + this.description));
            }
            // If the tags value was not default, then add it to the list of args.
            if ((this.tags != default(string[])))
            {
                for (int i = 0; (i < this.tags.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--tags=" + this.tags[i]));
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
        public virtual DeveloperUserManagerUpdateInfoWrapper DeveloperUserManagerUpdateInfo(DeveloperUserManagerUpdateInfoArgs updateInfoArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("developer-user-manager");
            genBeamCommandArgs.Add("update-info");
            genBeamCommandArgs.Add(updateInfoArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeveloperUserManagerUpdateInfoWrapper genBeamCommandWrapper = new DeveloperUserManagerUpdateInfoWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeveloperUserManagerUpdateInfoWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeveloperUserManagerUpdateInfoWrapper OnStreamDeveloperUserResult(System.Action<ReportDataPoint<BeamDeveloperUserResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
