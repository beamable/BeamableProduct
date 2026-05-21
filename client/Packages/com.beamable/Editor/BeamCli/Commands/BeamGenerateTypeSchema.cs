
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class GenerateTypeSchemaArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>File path to write the JSON schema to; omit to return as command output</summary>
        public string output;
        /// <summary>Directory to write split JSON schema files to; generates one file per section plus an index</summary>
        public string outputDir;
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
        public virtual GenerateTypeSchemaWrapper GenerateTypeSchema(GenerateTypeSchemaArgs generateTypeSchemaArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("generate-type-schema");
            genBeamCommandArgs.Add(generateTypeSchemaArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            GenerateTypeSchemaWrapper genBeamCommandWrapper = new GenerateTypeSchemaWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class GenerateTypeSchemaWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual GenerateTypeSchemaWrapper OnStreamBeamableTypesSchema(System.Action<ReportDataPoint<BeamBeamableTypesSchema>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
