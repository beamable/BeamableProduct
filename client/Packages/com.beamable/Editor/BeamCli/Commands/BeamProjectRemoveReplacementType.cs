
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectRemoveReplacementTypeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The reference Id (C# class/struct name) for the replacement</summary>
        public string referenceId;
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
        public virtual ProjectRemoveReplacementTypeWrapper ProjectRemoveReplacementType(ProjectRemoveReplacementTypeArgs removeReplacementTypeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("remove-replacement-type");
            genBeamCommandArgs.Add(removeReplacementTypeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectRemoveReplacementTypeWrapper genBeamCommandWrapper = new ProjectRemoveReplacementTypeWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectRemoveReplacementTypeWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
    }
}
