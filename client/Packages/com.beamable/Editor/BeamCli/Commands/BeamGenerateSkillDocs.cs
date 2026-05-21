
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class GenerateSkillDocsArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>Path to the Docs directory containing SkillTemplates/ and Skills/ subdirectories</summary>
        public string templateDir;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the templateDir value was not default, then add it to the list of args.
            if ((this.templateDir != default(string)))
            {
                genBeamCommandArgs.Add(("--template-dir=" + this.templateDir));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual GenerateSkillDocsWrapper GenerateSkillDocs(GenerateSkillDocsArgs generateSkillDocsArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("generate-skill-docs");
            genBeamCommandArgs.Add(generateSkillDocsArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            GenerateSkillDocsWrapper genBeamCommandWrapper = new GenerateSkillDocsWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class GenerateSkillDocsWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual GenerateSkillDocsWrapper OnStreamGenerateSkillDocsCommandResult(System.Action<ReportDataPoint<BeamGenerateSkillDocsCommandResult>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
