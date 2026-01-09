
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ContentPsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>When true, the command will run forever and watch the state of the program</summary>
        public bool watch;
        /// <summary>Inform a subset of ','-separated manifest ids for which to return data. By default, will return just the global manifest</summary>
        public string[] manifestIds;
        /// <summary>Listens to the given process id. Terminates this long-running command when the it no longer is running</summary>
        public int requireProcessId;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the watch value was not default, then add it to the list of args.
            if ((this.watch != default(bool)))
            {
                genBeamCommandArgs.Add(("--watch=" + this.watch));
            }
            // If the manifestIds value was not default, then add it to the list of args.
            if ((this.manifestIds != default(string[])))
            {
                for (int i = 0; (i < this.manifestIds.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--manifest-ids=" + this.manifestIds[i]));
                }
            }
            // If the requireProcessId value was not default, then add it to the list of args.
            if ((this.requireProcessId != default(int)))
            {
                genBeamCommandArgs.Add(("--require-process-id=" + this.requireProcessId));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ContentPsWrapper ContentPs(ContentPsArgs psArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("content");
            genBeamCommandArgs.Add("ps");
            genBeamCommandArgs.Add(psArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ContentPsWrapper genBeamCommandWrapper = new ContentPsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ContentPsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ContentPsWrapper OnStreamContentPsCommandEvent(System.Action<ReportDataPoint<BeamContentPsCommandEvent>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
        public virtual ContentPsWrapper OnProgressStreamContentPsProgressMessage(System.Action<ReportDataPoint<BeamContentPsProgressMessage>> cb)
        {
            this.Command.On("progressStream", cb);
            return this;
        }
    }
}
