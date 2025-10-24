
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class MkdocsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>A folder where the output docs will be written</summary>
        public string output;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the output value was not default, then add it to the list of args.
            if ((this.output != default(string)))
            {
                genBeamCommandArgs.Add(("--output=" + this.output));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual MkdocsWrapper Mkdocs(MkdocsArgs mkdocsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("mkdocs");
            genBeamCommandArgs.Add(mkdocsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            MkdocsWrapper genBeamCommandWrapper = new MkdocsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class MkdocsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual MkdocsWrapper OnStreamGenerateMkDocsCommandResult(System.Action<ReportDataPoint<BeamGenerateMkDocsCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
