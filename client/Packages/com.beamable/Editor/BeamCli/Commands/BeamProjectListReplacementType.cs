
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public partial class ProjectListReplacementTypeArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary>The Unreal project name</summary>
        public string projectName;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
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
        public virtual ProjectListReplacementTypeWrapper ProjectListReplacementType(ProjectListReplacementTypeArgs listReplacementTypeArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("project");
            genBeamCommandArgs.Add("list-replacement-type");
            genBeamCommandArgs.Add(listReplacementTypeArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            ProjectListReplacementTypeWrapper genBeamCommandWrapper = new ProjectListReplacementTypeWrapper();
            genBeamCommandWrapper.Command = command;
            // Return the command!
            return genBeamCommandWrapper;
        }
    }
    public partial class ProjectListReplacementTypeWrapper : Beamable.Common.BeamCli.BeamCommandWrapper
    {
        public virtual ProjectListReplacementTypeWrapper OnStreamListReplacementTypeCommandOutput(System.Action<ReportDataPoint<BeamListReplacementTypeCommandOutput>> cb)
        {
            this.Command.On("stream", cb);
            return this;
        }
    }
}
