
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeveloperUserManagerCreateUserArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The max amount of captured users that you can have before starting to delete the oldest (0 means infinity)</summary>
        public int rollingBufferSize;
        /// <summary>The alias is a chosen name for this player which is not the same as the player name in the backend</summary>
        public string alias;
        /// <summary>A gamer tag to a template that will be used to copy the stats and inventory to the created player</summary>
        public string template;
        /// <summary>A shortly description of this new player</summary>
        public string description;
        /// <summary>The user type of this player 0 - Captured 1 - Local 2 - Shared</summary>
        public int userType;
        /// <summary>A list of tags to set in this new player (only locally)</summary>
        public string[] tags;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the rollingBufferSize value was not default, then add it to the list of args.
            if ((this.rollingBufferSize != default(int)))
            {
                genBeamCommandArgs.Add(("--rolling-buffer-size=" + this.rollingBufferSize));
            }
            // If the alias value was not default, then add it to the list of args.
            if ((this.alias != default(string)))
            {
                genBeamCommandArgs.Add(("--alias=" + this.alias));
            }
            // If the template value was not default, then add it to the list of args.
            if ((this.template != default(string)))
            {
                genBeamCommandArgs.Add(("--template=" + this.template));
            }
            // If the description value was not default, then add it to the list of args.
            if ((this.description != default(string)))
            {
                genBeamCommandArgs.Add(("--description=" + this.description));
            }
            // If the userType value was not default, then add it to the list of args.
            if ((this.userType != default(int)))
            {
                genBeamCommandArgs.Add(("--user-type=" + this.userType));
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
        public virtual DeveloperUserManagerCreateUserWrapper DeveloperUserManagerCreateUser(DeveloperUserManagerCreateUserArgs createUserArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("developer-user-manager");
            genBeamCommandArgs.Add("create-user");
            genBeamCommandArgs.Add(createUserArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeveloperUserManagerCreateUserWrapper genBeamCommandWrapper = new DeveloperUserManagerCreateUserWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeveloperUserManagerCreateUserWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeveloperUserManagerCreateUserWrapper OnStreamDeveloperUserResult(System.Action<ReportDataPoint<BeamDeveloperUserResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
