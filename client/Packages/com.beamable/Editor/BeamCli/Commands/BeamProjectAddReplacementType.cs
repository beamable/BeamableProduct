
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectAddReplacementTypeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The reference Id (C# class/struct name) for the replacement</summary>
        public string referenceId;
        /// <summary>The name of the Type to replaced with in Unreal auto-gen</summary>
        public string replacementType;
        /// <summary>The name of the Optional Type to replaced with in Unreal auto-gen</summary>
        public string optionalReplacementType;
        /// <summary>The full import for the replacement type to be used in Unreal auto-gen</summary>
        public string engineImport;
        /// <summary>The Unreal project name</summary>
        public string projectName;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // If the referenceId value was not default, then add it to the list of args.
            if ((this.referenceId != default(string)))
            {
                genBeamCommandArgs.Add(("--reference-id=" + this.referenceId));
            }
            // If the replacementType value was not default, then add it to the list of args.
            if ((this.replacementType != default(string)))
            {
                genBeamCommandArgs.Add(("--replacement-type=" + this.replacementType));
            }
            // If the optionalReplacementType value was not default, then add it to the list of args.
            if ((this.optionalReplacementType != default(string)))
            {
                genBeamCommandArgs.Add(("--optional-replacement-type=" + this.optionalReplacementType));
            }
            // If the engineImport value was not default, then add it to the list of args.
            if ((this.engineImport != default(string)))
            {
                genBeamCommandArgs.Add(("--engine-import=" + this.engineImport));
            }
            // If the projectName value was not default, then add it to the list of args.
            if ((this.projectName != default(string)))
            {
                genBeamCommandArgs.Add(("--project-name=" + this.projectName));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual ProjectAddReplacementTypeWrapper ProjectAddReplacementType(ProjectAddReplacementTypeArgs addReplacementTypeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("add-replacement-type");
            genBeamCommandArgs.Add(addReplacementTypeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectAddReplacementTypeWrapper genBeamCommandWrapper = new ProjectAddReplacementTypeWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectAddReplacementTypeWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
