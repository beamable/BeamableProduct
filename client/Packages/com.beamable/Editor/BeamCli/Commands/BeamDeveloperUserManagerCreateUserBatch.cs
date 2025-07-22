
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class DeveloperUserManagerCreateUserBatchArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The max amount of temporary users that you can have before starting to delete the oldest</summary>
        public int rollingBufferSize;
        /// <summary>The gamer tag list for all templates that will be copied into a new player</summary>
        public string[] templatesList;
        /// <summary>A parallel list of the template-list arg that contains the amount of users for each template</summary>
        public string[] amountList;
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
            // If the templatesList value was not default, then add it to the list of args.
            if ((this.templatesList != default(string[])))
            {
                for (int i = 0; (i < this.templatesList.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--templates-list=" + this.templatesList[i]));
                }
            }
            // If the amountList value was not default, then add it to the list of args.
            if ((this.amountList != default(string[])))
            {
                for (int i = 0; (i < this.amountList.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--amount-list=" + this.amountList[i]));
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
        public virtual DeveloperUserManagerCreateUserBatchWrapper DeveloperUserManagerCreateUserBatch(DeveloperUserManagerCreateUserBatchArgs createUserBatchArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("developer-user-manager");
            genBeamCommandArgs.Add("create-user-batch");
            genBeamCommandArgs.Add(createUserBatchArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            DeveloperUserManagerCreateUserBatchWrapper genBeamCommandWrapper = new DeveloperUserManagerCreateUserBatchWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class DeveloperUserManagerCreateUserBatchWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual DeveloperUserManagerCreateUserBatchWrapper OnStreamDeveloperUserResult(System.Action<ReportDataPoint<BeamDeveloperUserResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
