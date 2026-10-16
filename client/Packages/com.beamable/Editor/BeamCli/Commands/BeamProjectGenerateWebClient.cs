
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectGenerateWebClientArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The directory where the generated code will be written</summary>
        public string outputDir;
        /// <summary>The language of the generated code. Valid values are: `typescript` (default), `ts`, `javascript`, `js`</summary>
        public string lang;
        /// <summary>How C# `long` (OpenAPI int64) fields are typed in the generated TypeScript. Valid values are: `bigint-union` (default, `bigint | string`), `number`, `string`</summary>
        public string int64As;
        /// <summary>Build the microservices first so the generated clients match the current code, instead of using the OpenAPI documents from their last build</summary>
        public bool build;
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
            // If the lang value was not default, then add it to the list of args.
            if ((this.lang != default(string)))
            {
                genBeamCommandArgs.Add(("--lang=" + this.lang));
            }
            // If the int64As value was not default, then add it to the list of args.
            if ((this.int64As != default(string)))
            {
                genBeamCommandArgs.Add(("--int64-as=" + this.int64As));
            }
            // If the build value was not default, then add it to the list of args.
            if ((this.build != default(bool)))
            {
                genBeamCommandArgs.Add(("--build=" + this.build));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectGenerateWebClientWrapper ProjectGenerateWebClient(ProjectGenerateWebClientArgs webClientArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("generate");
            genBeamCommandArgs.Add("web-client");
            genBeamCommandArgs.Add(webClientArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectGenerateWebClientWrapper genBeamCommandWrapper = new ProjectGenerateWebClientWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectGenerateWebClientWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectGenerateWebClientWrapper OnStreamGenerateWebClientCommandArgsResult(System.Action<ReportDataPoint<BeamGenerateWebClientCommandArgsResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
