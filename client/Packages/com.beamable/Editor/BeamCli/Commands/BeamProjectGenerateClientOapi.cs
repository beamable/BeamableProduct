
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectGenerateClientOapiArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Directory to write the output client at</summary>
        public string outputDir;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the outputDir value was not default, then add it to the list of args.
            if ((this.outputDir != default(string)))
            {
                genBeamCommandArgs.Add(("--output-dir=" + this.outputDir));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectGenerateClientOapiWrapper ProjectGenerateClientOapi(ProjectGenerateClientOapiArgs generateClientOapiArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("generate-client-oapi");
            genBeamCommandArgs.Add(generateClientOapiArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectGenerateClientOapiWrapper genBeamCommandWrapper = new ProjectGenerateClientOapiWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectGenerateClientOapiWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectGenerateClientOapiWrapper OnStreamGenerateClientOapiCommandArgsResult(System.Action<ReportDataPoint<BeamGenerateClientOapiCommandArgsResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
